// FFmpegExecutor.cs
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Converter.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Converter.Infrastructure.Ffmpeg
{
    public class FFmpegExecutor : IFFmpegExecutor, IDisposable
    {
        private readonly string? _ffmpegPath;
        private readonly ILogger<FFmpegExecutor>? _logger;
        private bool _disposed = false;

        public FFmpegExecutor(string? ffmpegPath = null, ILogger<FFmpegExecutor>? logger = null)
        {
            _ffmpegPath = ffmpegPath;
            _logger = logger;
        }

        /// <summary>
        /// Пытается извлечь путь к входному файлу из аргументов FFmpeg (ищет -i "..." ).
        /// </summary>
        private static string? TryExtractInputPath(string arguments)
        {
            if (string.IsNullOrWhiteSpace(arguments))
            {
                return null;
            }

            const string marker = "-i \"";
            var idx = arguments.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
            {
                return null;
            }

            idx += marker.Length;
            var endIdx = arguments.IndexOf('"', idx);
            if (endIdx <= idx)
            {
                return null;
            }

            return arguments.Substring(idx, endIdx - idx);
        }

        /// <summary>
        /// Получает длительность медиафайла в секундах с помощью ffprobe (через сам FFmpeg binary).
        /// </summary>
        private async Task<double?> GetMediaDurationSecondsAsync(string inputPath, CancellationToken ct)
        {
            try
            {
                var exe = ResolveFfmpegExecutable();
                if (exe == null)
                {
                    return null;
                }

                var args = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{inputPath}\"";

                var startInfo = new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using var process = new Process { StartInfo = startInfo };
                process.Start();
                var output = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
                await process.WaitForExitAsync(ct).ConfigureAwait(false);

                if (double.TryParse(output.Trim().Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var seconds))
                {
                    if (seconds > 0.1)
                    {
                        return seconds;
                    }
                }
            }
            catch
            {
                // Если что-то пошло не так, просто вернем null и не будем использовать реальный прогресс
            }

            return null;
        }

        /// <summary>
        /// Пытается извлечь значение time= из строки лога FFmpeg и репортит прогресс.
        /// </summary>
        private static bool TryReportProgressFromLine(string line, double totalDurationSeconds, IProgress<double> progress, ref double lastPercent)
        {
            if (totalDurationSeconds <= 0)
            {
                return false;
            }

            // Ищем фрагмент вида time=00:00:05.12
            var timeIndex = line.IndexOf("time=", StringComparison.OrdinalIgnoreCase);
            if (timeIndex < 0)
            {
                return false;
            }

            timeIndex += "time=".Length;

            // Вырезаем до первого пробела после time=
            var endIndex = line.IndexOf(' ', timeIndex);
            if (endIndex < 0)
            {
                endIndex = line.Length;
            }

            var timeToken = line.Substring(timeIndex, endIndex - timeIndex).Trim();
            if (!TryParseFfmpegTime(timeToken, out var seconds))
            {
                return false;
            }

            var percent = Math.Max(0, Math.Min(100, seconds / totalDurationSeconds * 100.0));

            // Не репортим слишком часто: минимум +0.5% разницы
            if (percent - lastPercent < 0.5)
            {
                return false;
            }

            lastPercent = percent;
            progress.Report(percent);
            return true;
        }

        /// <summary>
        /// Парсит время формата HH:MM:SS.xx в секунды.
        /// </summary>
        private static bool TryParseFfmpegTime(string token, out double seconds)
        {
            seconds = 0;
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            // Иногда FFmpeg выдаёт time=5.123 без часов/минут
            if (!token.Contains(':'))
            {
                return double.TryParse(token.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out seconds);
            }

            var parts = token.Split(':');
            if (parts.Length != 3)
            {
                return false;
            }

            if (!int.TryParse(parts[0], out var hours)) return false;
            if (!int.TryParse(parts[1], out var minutes)) return false;
            if (!double.TryParse(parts[2].Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var secs)) return false;

            seconds = hours * 3600 + minutes * 60 + secs;
            return true;
        }

        public async Task ProbeAsync(string inputPath, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(inputPath))
                throw new ArgumentException("Input path cannot be empty", nameof(inputPath));

            _logger?.LogDebug("Probing media file: {InputPath}", inputPath);
            
            var arguments = $@"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 ""{inputPath}""";
            var result = await ExecuteProcessAsync(arguments, ct);
            
            if (result.ExitCode != 0)
            {
                throw new InvalidOperationException($"Failed to probe media file: {result.Error}");
            }
        }

        public async Task<int> ExecuteAsync(string arguments, IProgress<double> progress, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(arguments))
                throw new ArgumentException("Arguments cannot be empty", nameof(arguments));

            _logger?.LogDebug("Executing FFmpeg with arguments: {Arguments}", arguments);
            
            var result = await ExecuteProcessAsync(arguments, ct, progress);
            return result.ExitCode;
        }

        public async Task<string> GetVersionAsync(CancellationToken ct = default)
        {
            _logger?.LogDebug("Getting FFmpeg version");
            var result = await ExecuteProcessAsync("-version", ct);
            return result.ExitCode == 0 ? result.Output : throw new InvalidOperationException("Failed to get FFmpeg version");
        }

        public async Task<bool> IsFfmpegAvailableAsync(CancellationToken ct = default)
        {
            try
            {
                await GetVersionAsync(ct);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> GetMediaInfoAsync(string inputPath, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(inputPath))
                throw new ArgumentException("Input path cannot be empty", nameof(inputPath));

            _logger?.LogDebug("Getting media info for: {InputPath}", inputPath);
            
            var arguments = $"-i \"{inputPath}\" -hide_banner";
            var result = await ExecuteProcessAsync(arguments, ct);
            
            if (result.ExitCode != 0 && !result.Output.Contains("At least one output file must be specified"))
            {
                throw new InvalidOperationException($"Failed to get media info: {result.Error}");
            }
            
            return result.Output;
        }

        private async Task<ProcessResult> ExecuteProcessAsync(
            string arguments,
            CancellationToken ct,
            IProgress<double>? progress = null)
        {
            var exe = ResolveFfmpegExecutable();
            if (exe == null)
            {
                throw new FileNotFoundException("FFmpeg executable not found. Please ensure FFmpeg is installed and added to your PATH.");
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = arguments,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
            var output = new StringWriter();
            var error = new StringWriter();

            // Пытаемся заранее оценить длительность файла, чтобы считать реальный прогресс.
            double? totalDurationSeconds = null;
            if (progress != null)
            {
                try
                {
                    var inputPath = TryExtractInputPath(arguments);
                    if (!string.IsNullOrWhiteSpace(inputPath) && File.Exists(inputPath))
                    {
                        totalDurationSeconds = await GetMediaDurationSecondsAsync(inputPath, ct).ConfigureAwait(false);
                    }
                }
                catch
                {
                    // Если не удалось получить длительность, просто не будем считать процент по времени
                    totalDurationSeconds = null;
                }

                // Всегда отправляем стартовый прогресс 0% для нового файла
                try { progress.Report(0); } catch { }
            }

            double lastReportedPercent = 0; // чтобы не спамить одинаковыми значениями

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    output.WriteLine(e.Data);
                }
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    error.WriteLine(e.Data);
                }
            };

            var tcs = new TaskCompletionSource<ProcessResult>();

            // Фоновая задача, которая оценивает прогресс по прошедшему времени относительно длительности файла
            System.Threading.CancellationTokenSource? progressCts = null;

            process.Exited += (_, _) =>
            {
                try
                {
                    if (progress != null)
                    {
                        // По факту завершения процесса выставляем 100%
                        if (lastReportedPercent < 100)
                        {
                            try { progress.Report(100); } catch { }
                        }
                    }
                }
                catch
                {
                    // Игнорируем ошибки визуального прогресса
                }
                finally
                {
                    try { progressCts?.Cancel(); } catch { }
                    progressCts?.Dispose();
                }

                tcs.TrySetResult(new ProcessResult
                {
                    ExitCode = process.ExitCode,
                    Output = output.ToString(),
                    Error = error.ToString()
                });
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Запускаем фоновый таймер прогресса ТОЛЬКО после старта процесса,
            // иначе process.HasExited будет true и цикл сразу завершится.
            if (progress != null)
            {
                progressCts = System.Threading.CancellationTokenSource.CreateLinkedTokenSource(ct);
                var token = progressCts.Token;

                var stopwatch = new System.Diagnostics.Stopwatch();

                _ = Task.Run(async () =>
                {
                    try
                    {
                        stopwatch.Start();
                        while (!token.IsCancellationRequested && !process.HasExited)
                        {
                            double percent;
                            var elapsed = stopwatch.Elapsed.TotalSeconds;

                            if (totalDurationSeconds.HasValue && totalDurationSeconds.Value > 0.1)
                            {
                                percent = Math.Min(99.0, Math.Max(0.0, elapsed / totalDurationSeconds.Value * 100.0));
                            }
                            else
                            {
                                // Синтетический прогресс, если не удалось узнать длительность файла
                                percent = Math.Min(99.0, lastReportedPercent + 1.0);
                            }

                            if (percent - lastReportedPercent >= 0.5)
                            {
                                lastReportedPercent = percent;
                                try { progress.Report(percent); } catch { }
                            }

                            await Task.Delay(200, token).ConfigureAwait(false);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // Нормально при отмене
                    }
                    catch
                    {
                        // Ошибки фона прогресса не должны падать весь процесс
                    }
                }, CancellationToken.None);
            }

            using (ct.Register(() => 
            {
                try { process.Kill(); } 
                catch { /* Ignore */ }
                tcs.TrySetCanceled(ct);
            }))
            {
                return await tcs.Task.ConfigureAwait(false);
            }
        }

        private string? ResolveFfmpegExecutable()
        {
            // 1) Explicit path provided via constructor (can be file or directory)
            if (!string.IsNullOrWhiteSpace(_ffmpegPath))
            {
                try
                {
                    if (Directory.Exists(_ffmpegPath))
                    {
                        var exeName = OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg";
                        var candidate = Path.Combine(_ffmpegPath, exeName);
                        if (File.Exists(candidate))
                        {
                            return candidate;
                        }
                    }
                    else if (File.Exists(_ffmpegPath))
                    {
                        return _ffmpegPath;
                    }
                }
                catch
                {
                    // Fallback to other strategies
                }
            }

            // 2) Per-user location used by FfmpegBootstrapService
            try
            {
                var baseDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Converter",
                    "ffmpeg");
                var exeName = OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg";
                var localExe = Path.Combine(baseDir, exeName);
                if (File.Exists(localExe))
                {
                    return localExe;
                }
            }
            catch
            {
                // Ignore and fall back to PATH
            }

            // 3) Let OS resolve from PATH
            var fallbackExeName = OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg";
            return fallbackExeName;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Add any cleanup if needed
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        ~FFmpegExecutor()
        {
            Dispose(disposing: false);
        }

        private class ProcessResult
        {
            public int ExitCode { get; set; }
            public string Output { get; set; } = string.Empty;
            public string Error { get; set; } = string.Empty;
        }
    }
}
