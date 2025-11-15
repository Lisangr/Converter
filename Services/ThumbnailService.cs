using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xabe.FFmpeg;

namespace Converter.Services
{
    public class ThumbnailService
    {
        private readonly string _cacheDirectory;
        private readonly ConcurrentDictionary<string, Image> _memoryCache;
        private readonly SemaphoreSlim _semaphore;
        private readonly int _maxMemoryCacheSize = 50;
        
        public ThumbnailService()
        {
            _cacheDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Converter", "Thumbnails"
            );
            Directory.CreateDirectory(_cacheDirectory);
            _memoryCache = new ConcurrentDictionary<string, Image>();
            _semaphore = new SemaphoreSlim(3, 3); // Limit concurrent generations
            
            // Clean old cache on startup
            Task.Run(() => CleanOldCache());
        }
        
        public async Task<Image> GetThumbnailAsync(
            string videoPath, 
            int width = 120, 
            int height = 90,
            CancellationToken ct = default
        )
        {
            if (!File.Exists(videoPath))
                return CreatePlaceholderImage(width, height, "❌");
            
            var cacheKey = GetCacheKey(videoPath, width, height);
            
            // 1. Check memory cache
            if (_memoryCache.TryGetValue(cacheKey, out var cachedImage))
                return cachedImage;
            
            // 2. Check disk cache
            var cachePath = GetCachePath(cacheKey);
            if (IsCacheValid(cachePath, videoPath))
            {
                try
                {
                    var image = Image.FromFile(cachePath);
                    _memoryCache.TryAdd(cacheKey, image);
                    
                    // Limit memory cache size
                    if (_memoryCache.Count > _maxMemoryCacheSize)
                    {
                        ClearMemoryCache();
                    }
                    
                    return image;
                }
                catch
                {
                    // Cache file is corrupted, continue to generation
                }
            }
            
            // 3. Generate new thumbnail
            await _semaphore.WaitAsync(ct);
            try
            {
                // Double-check after acquiring semaphore
                if (_memoryCache.TryGetValue(cacheKey, out cachedImage))
                    return cachedImage;
                
                var tempPath = Path.Combine(_cacheDirectory, $"{Guid.NewGuid()}.jpg");
                
                try
                {
                    // FFmpeg command to extract first frame
                    var conversion = FFmpeg.Conversions.New()
                        .AddParameter($"-i \"{videoPath}\"")
                        .AddParameter($"-vf \"select=eq(n\\,0),scale={width}:{height}:force_original_aspect_ratio=decrease,pad={width}:{height}:(ow-iw)/2:(oh-ih)/2\"")
                        .AddParameter("-frames:v 1")
                        .AddParameter("-q:v 2") // JPEG quality
                        .SetOutput(tempPath);
                    
                    await conversion.Start(ct);
                    
                    // Move to cache path
                    File.Move(tempPath, cachePath, true);
                    
                    var newImage = Image.FromFile(cachePath);
                    _memoryCache.TryAdd(cacheKey, newImage);
                    
                    // Limit memory cache size
                    if (_memoryCache.Count > _maxMemoryCacheSize)
                    {
                        ClearMemoryCache();
                    }
                    
                    return newImage;
                }
                catch (Exception ex)
                {
                    // Clean up temp file if exists
                    if (File.Exists(tempPath))
                        File.Delete(tempPath);
                    
                    // Return placeholder on error
                    return CreatePlaceholderImage(width, height, "❌");
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }
        
        private async Task<string> GenerateThumbnailFile(
            string videoPath, 
            string outputPath,
            CancellationToken ct
        )
        {
            var conversion = FFmpeg.Conversions.New()
                .AddParameter($"-i \"{videoPath}\"")
                .AddParameter("-vf \"select=eq(n\\,0)\"")
                .AddParameter("-frames:v 1")
                .AddParameter("-q:v 2")
                .SetOutput(outputPath);
            
            await conversion.Start(ct);
            return outputPath;
        }
        
        private string GetCacheKey(string videoPath, int width, int height)
        {
            var fileInfo = new FileInfo(videoPath);
            var keyData = $"{videoPath}_{fileInfo.LastWriteTime.Ticks}_{width}x{height}";
            
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(keyData));
            var hash = Convert.ToHexString(hashBytes)[..16];
            
            return $"{hash}.jpg";
        }
        
        private string GetCachePath(string cacheKey)
        {
            return Path.Combine(_cacheDirectory, cacheKey);
        }
        
        private bool IsCacheValid(string cachePath, string videoPath)
        {
            if (!File.Exists(cachePath) || !File.Exists(videoPath))
                return false;
            
            var cacheFile = new FileInfo(cachePath);
            var videoFile = new FileInfo(videoPath);
            
            // Cache is valid if video file hasn't been modified since cache was created
            return cacheFile.LastWriteTime >= videoFile.LastWriteTime;
        }
        
        public Image CreatePlaceholderImage(int width, int height, string text = "?")
        {
            var bmp = new Bitmap(width, height);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.FromArgb(50, 50, 50));
                
                var font = new Font("Segoe UI", Math.Min(width, height) / 8);
                var textSize = g.MeasureString(text, font);
                var textX = (width - textSize.Width) / 2;
                var textY = (height - textSize.Height) / 2;
                
                g.DrawString(text, font, Brushes.White, textX, textY);
            }
            return bmp;
        }
        
        public void ClearCache()
        {
            try
            {
                ClearMemoryCache();
                
                if (Directory.Exists(_cacheDirectory))
                {
                    var files = Directory.GetFiles(_cacheDirectory, "*.jpg");
                    foreach (var file in files)
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch
                        {
                            // Ignore files that can't be deleted
                        }
                    }
                }
            }
            catch
            {
                // Ignore cache clearing errors
            }
        }
        
        public void ClearMemoryCache()
        {
            foreach (var kvp in _memoryCache)
            {
                kvp.Value?.Dispose();
            }
            _memoryCache.Clear();
        }
        
        private async Task CleanOldCache()
        {
            try
            {
                if (!Directory.Exists(_cacheDirectory))
                    return;
                
                var cutoffDate = DateTime.Now.AddDays(-30);
                var files = Directory.GetFiles(_cacheDirectory, "*.jpg");
                
                foreach (var file in files)
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        if (fileInfo.LastWriteTime < cutoffDate)
                        {
                            File.Delete(file);
                        }
                    }
                    catch
                    {
                        // Ignore files that can't be deleted
                    }
                }
            }
            catch
            {
                // Ignore cache cleaning errors
            }
        }
        
        public async Task<Image> GetThumbnailAtPositionAsync(
            string videoPath, 
            TimeSpan position,
            int width = 120, 
            int height = 90,
            CancellationToken ct = default
        )
        {
            if (!File.Exists(videoPath))
                return CreatePlaceholderImage(width, height, "❌");
            
            var cacheKey = GetCacheKey(videoPath, width, height) + $"_pos_{position.TotalSeconds:F1}";
            
            // Check memory cache
            if (_memoryCache.TryGetValue(cacheKey, out var cachedImage))
                return cachedImage;
            
            await _semaphore.WaitAsync(ct);
            try
            {
                var tempPath = Path.Combine(_cacheDirectory, $"{Guid.NewGuid()}.jpg");
                var cachePath = GetCachePath(cacheKey);
                
                try
                {
                    // FFmpeg command to extract frame at specific position
                    var conversion = FFmpeg.Conversions.New()
                        .AddParameter($"-ss {position.TotalSeconds:F3}")
                        .AddParameter($"-i \"{videoPath}\"")
                        .AddParameter($"-vf \"scale={width}:{height}:force_original_aspect_ratio=decrease,pad={width}:{height}:(ow-iw)/2:(oh-ih)/2\"")
                        .AddParameter("-frames:v 1")
                        .AddParameter("-q:v 2")
                        .SetOutput(tempPath);
                    
                    await conversion.Start(ct);
                    
                    File.Move(tempPath, cachePath, true);
                    
                    var newImage = Image.FromFile(cachePath);
                    _memoryCache.TryAdd(cacheKey, newImage);
                    
                    return newImage;
                }
                catch
                {
                    if (File.Exists(tempPath))
                        File.Delete(tempPath);
                    
                    return CreatePlaceholderImage(width, height, "❌");
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }
        
        public void Dispose()
        {
            ClearMemoryCache();
            _semaphore?.Dispose();
        }
    }
}
