// FfmpegBootstrapService.cs
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Converter.Application.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Converter.Infrastructure.Ffmpeg
{
    public class FfmpegBootstrapService : IHostedService
    {
        private readonly IFFmpegExecutor _ffmpegExecutor;
        private readonly ILogger<FfmpegBootstrapService> _logger;
        private readonly string _ffmpegPath;

        public FfmpegBootstrapService(
            IFFmpegExecutor ffmpegExecutor,
            ILogger<FfmpegBootstrapService> logger)
        {
            _ffmpegExecutor = ffmpegExecutor ?? throw new ArgumentNullException(nameof(ffmpegExecutor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg");
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Checking FFmpeg installation...");
                
                var isInstalled = await _ffmpegExecutor.IsFfmpegAvailableAsync(cancellationToken);
                if (!isInstalled)
                {
                    _logger.LogWarning("FFmpeg is not installed in system PATH");
                    // The public EnsureFfmpegAsync will be called from Program.cs
                    // This method should ideally handle the download or throw an error if not found
                    // For now, we'll just log and let the public method handle the error.
                    _logger.LogError("FFmpeg is not available. Please ensure it's installed.");
                }
                
                var version = await _ffmpegExecutor.GetVersionAsync(cancellationToken);
                _logger.LogInformation("FFmpeg version: {Version}", version.Split('\n')[0]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize FFmpeg");
                throw;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public async Task EnsureFfmpegAsync()
        {
            // Check if FFmpeg exists in the expected location
            var ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg", "ffmpeg.exe");
            
            if (!File.Exists(ffmpegPath))
            {
                // Download FFmpeg or show an error message
                throw new FileNotFoundException("FFmpeg not found. Please install FFmpeg and ensure it's in the correct location.");
            }
            
            // Optionally, verify FFmpeg version
            // var version = await GetFfmpegVersionAsync();
            // _logger.LogInformation("Using FFmpeg version: {Version}", version);
            
            await Task.CompletedTask;
        }
    }
}
