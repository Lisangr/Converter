using Converter.Domain.Models;

namespace Converter.Application.Interfaces;

public interface IFFmpegExecutor
{
    Task<MediaInfo> ProbeAsync(string inputPath, CancellationToken cancellationToken);
    Task ExecuteAsync(ConversionCommand command, IProgress<ConversionProgress> progress, CancellationToken cancellationToken);
    string ResolveExecutable();
}
