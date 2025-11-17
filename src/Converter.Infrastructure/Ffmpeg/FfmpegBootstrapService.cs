// FfmpegBootstrapService.cs
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Converter.Application.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;

namespace Converter.Infrastructure.Ffmpeg
{
    public class FfmpegBootstrapService : IHostedService
    {
        private readonly IFFmpegExecutor _ffmpegExecutor;
        private readonly ILogger<FfmpegBootstrapService> _logger;

        public FfmpegBootstrapService(
            IFFmpegExecutor ffmpegExecutor,
            ILogger<FfmpegBootstrapService> logger)
        {
            _ffmpegExecutor = ffmpegExecutor ?? throw new ArgumentNullException(nameof(ffmpegExecutor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Starting FFmpeg bootstrap service...");

                // Ensure FFmpeg is available and configured on service start as a safety net.
                await EnsureFfmpegAsync();

                var version = await _ffmpegExecutor.GetVersionAsync(cancellationToken);
                var firstLine = version?.Split('\n')[0] ?? version;
                _logger.LogInformation("FFmpeg version: {Version}", firstLine);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize FFmpeg");
                throw;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        /// <summary>
        /// Ensures that FFmpeg binaries are available and configured for Xabe.FFmpeg.
        /// Downloads FFmpeg into a per-user application data folder if missing
        /// and validates availability via <see cref="IFFmpegExecutor"/>.
        /// </summary>
        public async Task EnsureFfmpegAsync()
        {
            try
            {
                // Local per-user FFmpeg location (compatible with previous WinForms implementation)
                var baseDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Converter",
                    "ffmpeg");

                var exeName = OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg";
                var ffmpegExe = Path.Combine(baseDir, exeName);

                if (!File.Exists(ffmpegExe))
                {
                    _logger.LogInformation("FFmpeg executable not found. Downloading to {Path}...", baseDir);
                    Directory.CreateDirectory(baseDir);

                    await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official, baseDir)
                        .ConfigureAwait(false);

                    _logger.LogInformation("FFmpeg downloaded successfully to {Path}", baseDir);
                }

                // Configure Xabe.FFmpeg to use the downloaded binaries
                FFmpeg.SetExecutablesPath(baseDir);
                _logger.LogInformation("FFmpeg executables path set to {Path}", baseDir);

                // Validate availability via the infrastructure executor
                var available = await _ffmpegExecutor.IsFfmpegAvailableAsync().ConfigureAwait(false);
                if (!available)
                {
                    throw new InvalidOperationException("FFmpeg is not available even after bootstrap.");
                }

                _logger.LogInformation("FFmpeg is available and initialized.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while ensuring FFmpeg availability");
                throw;
            }
        }
    }
}
