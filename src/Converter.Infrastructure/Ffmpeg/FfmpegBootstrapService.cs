using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Converter.Application.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;
using System.Collections.Generic;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Converter.Infrastructure.Ffmpeg
{
    /// <summary>
    /// Класс для отслеживания прогресса загрузки FFmpeg
    /// </summary>
    internal class ProgressInfo
    {
        public string Type { get; set; } = string.Empty;
        public long DownloadedBytes { get; set; }
        public long TotalBytes { get; set; }
        public double Progress => TotalBytes > 0 ? (double)DownloadedBytes / TotalBytes * 100 : 0;
    }

    /// <summary>
    /// Класс для загрузки FFmpeg с поддержкой прогресса
    /// </summary>
    internal class FFmpegDownloader
    {
        private readonly FFmpegVersion _version;
        private readonly string _path;
        private readonly ILogger? _logger;

        public FFmpegDownloader(FFmpegVersion version, string path, ILogger? logger = null)
        {
            _version = version;
            _path = path;
            _logger = logger;
        }

        /// <summary>
        /// Фактическая загрузка FFmpeg (и FFprobe) через Xabe.FFmpeg.Downloader с грубым прогрессом.
        /// </summary>
        public async Task DownloadAsync(IProgress<ProgressInfo> progress, CancellationToken cancellationToken = default)
        {
            try
            {
                // Старт прогресса для FFmpeg
                progress?.Report(new ProgressInfo
                {
                    Type = "FFmpeg",
                    DownloadedBytes = 0,
                    TotalBytes = 1
                });

                _logger?.LogInformation("Starting FFmpeg download to {Path}...", _path);

                // Реальный вызов загрузчика Xabe.FFmpeg.Downloader.
                // GetLatestVersion - синхронный метод, поэтому оборачиваем его в Task.Run,
                // чтобы не блокировать вызывающий поток.
                await Task.Run(() =>
                {
                    // Загружаем FFmpeg непосредственно в целевую директорию _path,
                    // как в старой реализации EnsureFfmpegAsync.
                    Xabe.FFmpeg.Downloader.FFmpegDownloader.GetLatestVersion(_version, _path);
                }, cancellationToken).ConfigureAwait(false);

                // Завершение прогресса FFmpeg
                progress?.Report(new ProgressInfo
                {
                    Type = "FFmpeg",
                    DownloadedBytes = 1,
                    TotalBytes = 1
                });

                // Для совместимости с текущим UI даём простой сигнал о завершении FFprobe,
                // хотя Xabe скачивает оба бинарника в одном вызове.
                progress?.Report(new ProgressInfo
                {
                    Type = "FFprobe",
                    DownloadedBytes = 1,
                    TotalBytes = 1
                });

                _logger?.LogInformation("FFmpeg (and FFprobe) downloaded successfully to {Path}.", _path);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error downloading FFmpeg/FFprobe");
                throw;
            }
        }
    }


    public sealed class FfmpegBootstrapService : IHostedService, IDisposable
    {
        private readonly IFFmpegExecutor _ffmpegExecutor;
        private readonly ILogger<FfmpegBootstrapService> _logger;
        private readonly IMainView _mainView;
        private readonly CancellationTokenSource _shutdownCts = new();
        private Task? _backgroundTask;

        public FfmpegBootstrapService(
            IFFmpegExecutor ffmpegExecutor,
            ILogger<FfmpegBootstrapService> logger,
            IMainView mainView)
        {
            _ffmpegExecutor = ffmpegExecutor ?? throw new ArgumentNullException(nameof(ffmpegExecutor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mainView = mainView ?? throw new ArgumentNullException(nameof(mainView));
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Scheduling FFmpeg initialization in background...");
            _mainView.RunOnUiThread(() => _mainView.IsBusy = true);
            _mainView.UpdateFfmpegStatus("Загрузка и инициализация FFmpeg...");

            // Запускаем фоновую задачу без ожидания
            _backgroundTask = Task.Run(() => InitializeFfmpegAsync(_shutdownCts.Token),
                CancellationToken.None);

            return Task.CompletedTask;
        }

        private async Task InitializeFfmpegAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Starting FFmpeg initialization in background...");
                _mainView.UpdateFfmpegStatus("Проверка наличия FFmpeg...");

                // Local per-user FFmpeg location (compatible with previous WinForms implementation)
                var baseDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Converter",
                    "ffmpeg");

                var exeName = OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg";
                var ffmpegExe = Path.Combine(baseDir, exeName);

                bool needsDownload = !File.Exists(ffmpegExe);

                // Version check (simplified for now, more robust check would involve running ffmpeg -version)
                if (!needsDownload)
                {
                    try
                    {
                        _mainView.UpdateFfmpegStatus("Проверка версии FFmpeg...");
                        FFmpeg.SetExecutablesPath(baseDir);
                        var currentVersion = await _ffmpegExecutor.GetVersionAsync(cancellationToken);
                        if (string.IsNullOrEmpty(currentVersion))
                        {
                            _logger.LogWarning("Could not get current FFmpeg version, forcing re-download.");
                            needsDownload = true;
                        }
                        else
                        {
                            _logger.LogInformation("Current FFmpeg version: {Version}", currentVersion.Split('\n')[0]);
                            // For a true version check, one would compare currentVersion with a known latest version.
                            // For now, if we have a version, we assume it's good unless explicitly told to update.
                        }
                    }
                    catch (Exception versionEx)
                    {
                        _logger.LogWarning(versionEx, "Failed to get FFmpeg version, forcing re-download.");
                        needsDownload = true;
                    }
                }

                // Если основной exe ещё не существует, пробуем найти его в подпапках (структура Xabe.FFmpeg.Downloader)
                if (!File.Exists(ffmpegExe) && !needsDownload) // Only if we didn't already decide to download
                {
                    Directory.CreateDirectory(baseDir);

                    try
                    {
                        var existing = Directory
                            .EnumerateFiles(baseDir, exeName, SearchOption.AllDirectories)
                            .FirstOrDefault();

                        if (!string.IsNullOrEmpty(existing) && !string.Equals(existing, ffmpegExe, StringComparison.OrdinalIgnoreCase))
                        {
                            File.Copy(existing, ffmpegExe, overwrite: true);
                            _logger.LogInformation("FFmpeg executable found at {ExistingPath} and copied to {TargetPath}", existing, ffmpegExe);
                            _mainView.UpdateFfmpegStatus("FFmpeg найден и скопирован.");
                        }
                        else
                        {
                            needsDownload = true; // Still need to download if not found or copied
                        }
                    }
                    catch (Exception scanEx)
                    {
                        _logger.LogWarning(scanEx, "Failed to scan existing FFmpeg binaries in {Path}", baseDir);
                        needsDownload = true; // Error scanning, so assume download is needed
                    }
                }

                // Если после попытки поиска бинаря всё ещё нет, скачиваем его через Xabe.FFmpeg.Downloader
                if (needsDownload)
                {
                    _logger.LogInformation("FFmpeg executable not found or outdated. Downloading to {Path}...", baseDir);
                    _mainView.UpdateFfmpegStatus("Загрузка FFmpeg и FFprobe...\r\nЭто может занять несколько минут.");

                    // Создаем прогресс-репортер для отображения прогресса загрузки
                    var progress = new Progress<ProgressInfo>(p =>
                    {
                        string status = $"Загрузка {p.Type}: {p.DownloadedBytes / 1024 / 1024:0.0} МБ из {p.TotalBytes / 1024 / 1024:0.0} МБ ({p.Progress}%)";
                        _mainView.RunOnUiThread(() => _mainView.UpdateFfmpegStatus(status));
                        _logger.LogInformation(status);
                    });

                    try
                    {
                        // Скачиваем FFmpeg и FFprobe с отображением прогресса
                        var downloader = new FFmpegDownloader(FFmpegVersion.Official, baseDir, _logger);
                        await downloader.DownloadAsync(progress, cancellationToken).ConfigureAwait(false);

                        _logger.LogInformation("FFmpeg and FFprobe downloaded successfully to {Path}", baseDir);
                        _mainView.UpdateFfmpegStatus("FFmpeg и FFprobe успешно загружены.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error downloading FFmpeg");
                        _mainView.ShowError($"Ошибка загрузки FFmpeg: {ex.Message}");
                        throw;
                    }

                    try
                    {
                        // Исходный каталог, куда Xabe.FFmpeg.Downloader скачал бинарники
                        var sourceDir = FFmpeg.ExecutablesPath;
                        if (string.IsNullOrWhiteSpace(sourceDir) || !Directory.Exists(sourceDir))
                        {
                            sourceDir = baseDir;
                        }

                        _logger.LogInformation("Scanning downloaded FFmpeg binaries in {SourceDir}", sourceDir);

                        // Находим и копируем оба исполняемых файла (ffmpeg и ffprobe)
                        var downloadedFiles = Directory
                            .EnumerateFiles(sourceDir, "*.exe", SearchOption.AllDirectories)
                            .Where(f => f.EndsWith("ffmpeg.exe", StringComparison.OrdinalIgnoreCase) ||
                                       f.EndsWith("ffprobe.exe", StringComparison.OrdinalIgnoreCase))
                            .ToList();

                        if (downloadedFiles.Any())
                        {
                            foreach (var downloaded in downloadedFiles)
                            {
                                string targetPath = Path.Combine(baseDir, Path.GetFileName(downloaded));
                                if (!string.Equals(downloaded, targetPath, StringComparison.OrdinalIgnoreCase))
                                {
                                    File.Copy(downloaded, targetPath, overwrite: true);
                                }
                                _logger.LogInformation("Executable prepared at {TargetPath}", targetPath);
                            }
                            _mainView.UpdateFfmpegStatus("FFmpeg и FFprobe подготовлены к работе.");
                        }
                        else
                        {
                            _logger.LogWarning("FFmpegDownloader completed but no ffmpeg.exe/ffprobe.exe was found under {Path}", sourceDir);
                            _mainView.ShowError("FFmpeg не найден после загрузки. Проверьте логи.");
                        }
                    }
                    catch (Exception prepareEx)
                    {
/*                        _logger.LogError(ex, "Error downloading FFmpeg");
                        _mainView.ShowError($"Ошибка загрузки FFmpeg: {ex.Message}");*/
                        throw;
                    }
                }

                // Configure Xabe.FFmpeg to use the downloaded binaries
                FFmpeg.SetExecutablesPath(baseDir);
                _logger.LogInformation("FFmpeg executable path being used: {FfmpegDirPath}, Full path: {FfmpegExePath}, Exists: {Exists}", baseDir, ffmpegExe, File.Exists(ffmpegExe));
                _mainView.UpdateFfmpegStatus("Настройка FFmpeg...");

                // Validate availability via the infrastructure executor, но не падаем, если FFmpeg недоступен.
                var available = await _ffmpegExecutor.IsFfmpegAvailableAsync().ConfigureAwait(false);
                if (!available)
                {
                    _logger.LogWarning("FFmpeg is not available even after bootstrap. Executable path: {ExePath}, Exists: {Exists}", ffmpegExe, File.Exists(ffmpegExe));
                    _mainView.ShowError("FFmpeg недоступен после инициализации. Функции конвертации могут быть ограничены.");
                    return;
                }

                _logger.LogInformation("FFmpeg is available and initialized successfully in background.");
                _mainView.UpdateFfmpegStatus("FFmpeg готов к работе.");
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("FFmpeg initialization was canceled");
                _mainView.UpdateFfmpegStatus("Загрузка FFmpeg отменена.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing FFmpeg in background");
                _mainView.ShowError($"Ошибка инициализации FFmpeg: {ex.Message}");
            }
            finally
            {
                _mainView.RunOnUiThread(() => _mainView.IsBusy = false);
                _mainView.UpdateFfmpegStatus(""); // Clear status
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping FFmpeg bootstrap service...");

            // Отменяем фоновую задачу
            _shutdownCts.Cancel();

            // Даем задаче время на корректное завершение
            if (_backgroundTask != null)
            {
                await Task.WhenAny(
                    _backgroundTask,
                    Task.Delay(Timeout.Infinite, cancellationToken)
                ).ConfigureAwait(false);
            }
            _logger.LogInformation("FFmpeg сервис успешно остановлен");
        }

        public void Dispose()
        {
            _shutdownCts.Cancel();
            _shutdownCts.Dispose();
        }
    }
}