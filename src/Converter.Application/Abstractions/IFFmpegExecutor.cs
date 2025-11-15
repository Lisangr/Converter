using Converter.Domain.Models;

namespace Converter.Application.Abstractions;

public interface IFFmpegExecutor
{
    Task<MediaInfo> ProbeAsync(string inputPath, CancellationToken cancellationToken);
    Task RunAsync(
        string arguments,
        ConversionRequest request,
        IProgress<ConversionProgress> progress,
        CancellationToken cancellationToken);
}
