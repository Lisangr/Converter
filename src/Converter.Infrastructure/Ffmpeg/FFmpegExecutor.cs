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
    public sealed class FFmpegExecutor : IFFmpegExecutor, IDisposable
    {
        private readonly string? _ffmpegPath;
        private readonly ILogger<FFmpegExecutor>? _logger;
        private bool _disposed = false;

        public FFmpegExecutor(string? ffmpegPath = null, ILogger<FFmpegExecutor>? logger = null)
        {
            _ffmpegPath = ffmpegPath;
            _logger = logger;
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

            process.OutputDataReceived += (_, e) => { if (e.Data != null) output.WriteLine(e.Data); };
            process.ErrorDataReceived += (_, e) => { if (e.Data != null) error.WriteLine(e.Data); };

            var tcs = new TaskCompletionSource<ProcessResult>();

            process.Exited += (_, _) =>
            {
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

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                // Add any cleanup if needed
            }
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
