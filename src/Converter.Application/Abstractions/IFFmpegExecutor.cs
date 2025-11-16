// IFFmpegExecutor.cs
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Converter.Application.Abstractions;

public interface IFFmpegExecutor
{
    Task ProbeAsync(string inputPath, CancellationToken ct);
    Task<int> ExecuteAsync(string arguments, IProgress<double> progress, CancellationToken ct);
    Task<string> GetVersionAsync(CancellationToken ct = default);
    Task<bool> IsFfmpegAvailableAsync(CancellationToken ct = default);
    Task<string> GetMediaInfoAsync(string inputPath, CancellationToken ct = default);
}