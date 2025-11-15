using System.Diagnostics;
using Converter.Application.Abstractions;

namespace Converter.Infrastructure.Ffmpeg;

public sealed class FFmpegExecutor : IFFmpegExecutor
{
    private readonly string? _ffmpegPath;

    public FFmpegExecutor(string? ffmpegPath = null)
    {
        _ffmpegPath = ffmpegPath;
    }

    public Task ProbeAsync(string inputPath, CancellationToken ct)
    {
        // Placeholder: Xabe probing or ffprobe invocation can be added here.
        return Task.CompletedTask;
    }

    public async Task<int> ExecuteAsync(string arguments, IProgress<double> progress, CancellationToken ct)
    {
        // Minimal shim: run ffmpeg process if available, else simulate success.
        var exe = ResolveFfmpegExecutable();
        if (exe == null)
        {
            // Simulate execution for environments without ffmpeg.
            for (int i = 0; i <= 100; i += 10)
            {
                ct.ThrowIfCancellationRequested();
                progress.Report(i);
                await Task.Delay(100, ct).ConfigureAwait(false);
            }
            return 0;
        }

        var psi = new ProcessStartInfo
        {
            FileName = exe,
            Arguments = arguments,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        process.ErrorDataReceived += (_, e) => { /* parse progress if desired */ };
        process.OutputDataReceived += (_, e) => { };
        process.Start();
        process.BeginErrorReadLine();
        process.BeginOutputReadLine();
        await process.WaitForExitAsync(ct).ConfigureAwait(false);
        return process.ExitCode;
    }

    private string? ResolveFfmpegExecutable()
    {
        if (!string.IsNullOrWhiteSpace(_ffmpegPath) && File.Exists(_ffmpegPath)) return _ffmpegPath;
        // Try PATH
        return "ffmpeg"; // let OS resolve; may fail which is acceptable for a shim
    }
}
