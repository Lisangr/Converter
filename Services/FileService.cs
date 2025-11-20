using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xabe.FFmpeg;
using Converter.Application.Abstractions;

namespace Converter.Services
{
    public class FileService : IFileService, IDisposable
    {
        private readonly IThumbnailProvider _thumbnailProvider;
        private bool _disposed;

        public FileService(IThumbnailProvider thumbnailProvider)
        {
            _thumbnailProvider = thumbnailProvider ?? throw new ArgumentNullException(nameof(thumbnailProvider));
        }

        public async Task<Image> GetThumbnailAsync(string filePath, int width, int height, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            try
            {
                using var stream = await _thumbnailProvider.GetThumbnailAsync(filePath, width, height, cancellationToken);
                return Image.FromStream(stream);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return CreatePlaceholderThumbnail(width, height, "❌");
            }
        }

        public async Task<FileInfo> ProbeFileAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found", filePath);

            var fileInfo = new FileInfo(filePath);
            
            // Example of probing file with FFmpeg
            try
            {
                var mediaInfo = await FFmpeg.GetMediaInfo(filePath);
                // You can add more file information here if needed
                return fileInfo;
            }
            catch
            {
                // If FFmpeg probing fails, return basic file info
                return fileInfo;
            }
        }

        public string[] GetSupportedFileExtensions()
        {
            return new[]
            {
                ".mp4", ".avi", ".mov", ".mkv", ".wmv", ".flv", ".webm", // Video
                ".mp3", ".wav", ".ogg", ".aac", ".flac", ".wma"           // Audio
            };
        }

        public bool IsFileSupported(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            var extension = Path.GetExtension(filePath)?.ToLowerInvariant();
            return !string.IsNullOrEmpty(extension) && 
                   Array.Exists(GetSupportedFileExtensions(), ext => ext.Equals(extension, StringComparison.OrdinalIgnoreCase));
        }

        public Image CreatePlaceholderThumbnail(int width, int height, string text)
        {
            var bmp = new Bitmap(width, height);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.FromArgb(50, 50, 50));
                using var font = new Font("Segoe UI", Math.Min(width, height) / 8);
                var textSize = g.MeasureString(text, font);
                var textX = (width - textSize.Width) / 2;
                var textY = (height - textSize.Height) / 2;
                g.DrawString(text, font, Brushes.White, textX, textY);
            }
            return bmp;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _thumbnailProvider?.Dispose();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}