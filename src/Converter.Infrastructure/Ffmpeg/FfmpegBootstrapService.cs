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
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _initializationTask;

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
                _logger.LogInformation("Запуск инициализации FFmpeg...");
                _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                
                _initializationTask = EnsureFfmpegAsync(_cancellationTokenSource.Token);
                await _initializationTask;
                
                var version = await _ffmpegExecutor.GetVersionAsync(cancellationToken);
                var firstLine = version?.Split('\n')[0] ?? version;
                _logger.LogInformation("FFmpeg успешно инициализирован. Версия: {Version}", firstLine);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при инициализации FFmpeg");
                throw;
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Остановка FFmpeg сервиса...");
                
                // Отменяем все текущие операции FFmpeg
                _cancellationTokenSource?.Cancel();
                
                // Ожидаем завершения текущих операций, но не дольше 5 секунд
                if (_initializationTask != null && !_initializationTask.IsCompleted)
                {
                    await Task.WhenAny(
                        _initializationTask,
                        Task.Delay(TimeSpan.FromSeconds(5), cancellationToken)
                    );
                }
                
                _logger.LogInformation("FFmpeg сервис успешно остановлен");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при остановке FFmpeg сервиса");
                throw;
            }
        }

        /// <summary>
        /// Ensures that FFmpeg binaries are available and configured for Xabe.FFmpeg.
        /// Downloads FFmpeg into a per-user application data folder if missing
        /// and validates availability via <see cref="IFFmpegExecutor"/>.
        /// </summary>
        public async Task EnsureFfmpegAsync(CancellationToken cancellationToken = default)
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
