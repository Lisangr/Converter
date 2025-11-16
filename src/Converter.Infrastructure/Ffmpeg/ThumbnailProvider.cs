// ThumbnailProvider.cs
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Converter.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Converter.Infrastructure.Ffmpeg
{
    public sealed class ThumbnailProvider : IThumbnailProvider, IAsyncDisposable
    {
        private readonly IFFmpegExecutor _ffmpegExecutor;
        private readonly ILogger<ThumbnailProvider> _logger;
        private bool _disposed;

        public ThumbnailProvider(
            IFFmpegExecutor ffmpegExecutor,
            ILogger<ThumbnailProvider> logger)
        {
            _ffmpegExecutor = ffmpegExecutor ?? throw new ArgumentNullException(nameof(ffmpegExecutor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Stream> GetThumbnailAsync(string videoPath, int width, int height, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(videoPath))
                throw new ArgumentException("Video path cannot be empty", nameof(videoPath));
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));

            _logger.LogDebug("Generating thumbnail for {VideoPath} at {Width}x{Height}", videoPath, width, height);

            try
            {
                // Create a temporary file for the thumbnail
                var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.jpg");
                
                try
                {
                    // Generate thumbnail at 1 second
                    var arguments = $"-i \"{videoPath}\" -ss 00:00:01 -vframes 1 -vf \"scale={width}:{height}:force_original_aspect_ratio=decrease,pad={width}:{height}:(ow-iw)/2:(oh-ih)/2\" -q:v 2 -y \"{tempFile}\"";
                    
                    var exitCode = await _ffmpegExecutor.ExecuteAsync(arguments, new Progress<double>(), ct);
                    if (exitCode != 0)
                    {
                        throw new InvalidOperationException($"FFmpeg failed with exit code {exitCode}");
                    }

                    // Read the thumbnail into a memory stream
                    var memoryStream = new MemoryStream();
                    using (var fileStream = new FileStream(tempFile, FileMode.Open, FileAccess.Read))
                    {
                        await fileStream.CopyToAsync(memoryStream, ct);
                    }
                    
                    memoryStream.Position = 0;
                    return memoryStream;
                }
                finally
                {
                    try { if (File.Exists(tempFile)) File.Delete(tempFile); }
                    catch (Exception ex) { _logger.LogWarning(ex, "Failed to delete temporary thumbnail file"); }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating thumbnail for {VideoPath}", videoPath);
                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                _disposed = true;
                // Add any async cleanup if needed
                await Task.CompletedTask;
            }
        }
    }
}