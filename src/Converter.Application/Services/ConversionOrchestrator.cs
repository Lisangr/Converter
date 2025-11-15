using Converter.Application.Interfaces;
using Converter.Domain.Models;
using Microsoft.Extensions.Logging;

namespace Converter.Application.Services;

public sealed class ConversionOrchestrator(IFFmpegExecutor executor, ConversionCommandBuilder commandBuilder, ILogger<ConversionOrchestrator> logger) : IConversionOrchestrator
{
    private readonly IFFmpegExecutor _executor = executor;
    private readonly ConversionCommandBuilder _commandBuilder = commandBuilder;
    private readonly ILogger<ConversionOrchestrator> _logger = logger;

    public async Task<ConversionResult> ExecuteAsync(ConversionRequest request, IProgress<ConversionProgress> progress, CancellationToken cancellationToken)
    {
        try
        {
            var mediaInfo = await _executor.ProbeAsync(request.InputPath, cancellationToken).ConfigureAwait(false);
            var outputPath = Path.Combine(request.OutputDirectory, Path.GetFileNameWithoutExtension(request.InputPath) + $".{request.Profile.Container}");
            var command = _commandBuilder.Build(mediaInfo, request.Profile, request.InputPath, outputPath);
            await _executor.ExecuteAsync(command, progress, cancellationToken).ConfigureAwait(false);
            return new ConversionResult.Success(request.InputPath, outputPath, mediaInfo.Video.Duration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Conversion failed for {Input}", request.InputPath);
            return new ConversionResult.Failure(request.InputPath, ex.Message);
        }
    }
}
