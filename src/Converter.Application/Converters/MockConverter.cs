using System;
using System.Threading;
using System.Threading.Tasks;
using Converter.Application.Abstractions;
using Converter.Application.Models;
using Converter.Domain.Models;

namespace Converter.Application.Converters;

/// <summary>
/// Реализация конвертера для выполнения операций конвертации.
/// </summary>
public class MockConverter : IConverter
{
    private readonly IConversionOrchestrator _orchestrator;

    public MockConverter(IConversionOrchestrator orchestrator)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
    }

    public async Task<ConversionResult> ConvertAsync(QueueItem item, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));

        // Создаем профиль конвертации по умолчанию
        var profile = new ConversionProfile("Mock", "libx264", "aac", "128k", 23);
        var request = new ConversionRequest(item.FilePath, item.OutputPath ?? "output.mp4", profile);

        try
        {
            var orchestratorProgress = new Progress<int>(p => progress?.Report(p));
            var outcome = await _orchestrator.ConvertAsync(request, orchestratorProgress, cancellationToken);

            return new ConversionResult
            {
                Success = outcome.Success,
                ErrorMessage = outcome.ErrorMessage,
                OutputPath = outcome.Success ? request.OutputPath : null,
                OutputFileSize = outcome.OutputSize ?? 0
            };
        }
        catch (Exception ex)
        {
            return new ConversionResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}

/// <summary>
/// Реальный конвертер на основе FFmpeg.
/// </summary>
public class FfmpegConverter : IConverter
{
    private readonly IConversionOrchestrator _orchestrator;

    public FfmpegConverter(IConversionOrchestrator orchestrator)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
    }

    public async Task<ConversionResult> ConvertAsync(QueueItem item, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));
        if (string.IsNullOrEmpty(item.FilePath)) throw new ArgumentException("File path cannot be null or empty", nameof(item));

        try
        {
            // Создаем запрос на конвертацию на основе настроек элемента
            var profile = new ConversionProfile("Default", "libx264", "aac", "128k", 23);
            var outputPath = item.OutputPath ?? System.IO.Path.ChangeExtension(item.FilePath, ".mp4");
            var request = new ConversionRequest(item.FilePath, outputPath, profile);

            var orchestratorProgress = new Progress<int>(p => progress?.Report(p));
            var outcome = await _orchestrator.ConvertAsync(request, orchestratorProgress, cancellationToken);

            return new ConversionResult
            {
                Success = outcome.Success,
                ErrorMessage = outcome.ErrorMessage,
                OutputPath = outcome.Success ? outputPath : null,
                OutputFileSize = outcome.OutputSize ?? 0
            };
        }
        catch (Exception ex)
        {
            return new ConversionResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}