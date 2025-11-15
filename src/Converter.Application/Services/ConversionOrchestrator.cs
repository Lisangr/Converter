using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Converter.Application.Abstractions;
using Converter.Application.Builders;
using Microsoft.Extensions.Logging;

namespace Converter.Application.Services;

public sealed class ConversionOrchestrator : IConversionOrchestrator
{
    private readonly IFFmpegExecutor _executor;
    private readonly IConversionCommandBuilder _builder;
    private readonly ILogger<ConversionOrchestrator> _logger;

    public ConversionOrchestrator(IFFmpegExecutor executor, IConversionCommandBuilder builder, ILogger<ConversionOrchestrator> logger)
    {
        _executor = executor;
        _builder = builder;
        _logger = logger;
    }

    public Task ProbeAsync(string filePath, CancellationToken ct)
        => _executor.ProbeAsync(filePath, ct);

    public async Task<ConversionOutcome> ConvertAsync(ConversionRequest request, IProgress<int> progress, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Starting conversion: {Input} -> {Output}", request.InputPath, request.OutputPath);
            var args = _builder.Build(request);
            var adapter = new Progress<double>(v => progress.Report((int)Math.Clamp(Math.Round(v), 0, 100)));
            var code = await _executor.ExecuteAsync(args, adapter, ct).ConfigureAwait(false);
            if (code == 0)
            {
                long? outputSize = null;
                try { if (File.Exists(request.OutputPath)) outputSize = new FileInfo(request.OutputPath).Length; } catch { }
                _logger.LogInformation("Conversion succeeded: {Output} size={Size}", request.OutputPath, outputSize);
                return new ConversionOutcome(true, outputSize, null);
            }
            _logger.LogError("FFmpeg exited with code {Code} for {Output}", code, request.OutputPath);
            return new ConversionOutcome(false, null, $"FFmpeg exited with code {code}");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Conversion canceled: {Input}", request.InputPath);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Conversion failed: {Input}", request.InputPath);
            return new ConversionOutcome(false, null, ex.Message);
        }
    }
}
