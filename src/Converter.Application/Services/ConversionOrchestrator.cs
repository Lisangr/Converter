using Converter.Application.Abstractions;
using Converter.Application.Builders;
using Converter.Domain.Models;
using Microsoft.Extensions.Logging;

namespace Converter.Application.Services;

public sealed class ConversionOrchestrator : IConversionOrchestrator
{
    private readonly IFFmpegExecutor _executor;
    private readonly ConversionCommandBuilder _commandBuilder;
    private readonly ILogger<ConversionOrchestrator> _logger;

    public ConversionOrchestrator(
        IFFmpegExecutor executor,
        ConversionCommandBuilder commandBuilder,
        ILogger<ConversionOrchestrator> logger)
    {
        _executor = executor;
        _commandBuilder = commandBuilder;
        _logger = logger;
    }

    public async Task<ConversionResult> ExecuteAsync(
        ConversionRequest request,
        IProgress<ConversionProgress> progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        Directory.CreateDirectory(request.OutputDirectory);

        var outputFile = Path.Combine(
            request.OutputDirectory,
            Path.ChangeExtension(Path.GetFileName(request.InputPath), request.Profile.Container));

        try
        {
            var mediaInfo = await _executor.ProbeAsync(request.InputPath, cancellationToken).ConfigureAwait(false);
            var command = _commandBuilder.Build(mediaInfo, request, outputFile);
            await _executor.RunAsync(command, request, progress, cancellationToken).ConfigureAwait(false);
            return new ConversionResult.Success(request.InputPath, outputFile, mediaInfo.Duration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Conversion failed for {File}", request.InputPath);
            return new ConversionResult.Failure(request.InputPath, ex.Message, ex);
        }
    }
}
