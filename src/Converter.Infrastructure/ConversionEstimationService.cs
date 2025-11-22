using System;
using System.Threading;
using System.Threading.Tasks;
using Converter.Application.Abstractions;
using Converter.Application.Models;

namespace Converter.Infrastructure;

/// <summary>
/// Сервис оценки конвертации видео.
/// Обертка вокруг существующего EstimationService.
/// </summary>
public class ConversionEstimationService : IConversionEstimationService
{
    private readonly Converter.Services.EstimationService _estimationService;

    public ConversionEstimationService(Converter.Services.EstimationService estimationService)
    {
        _estimationService = estimationService ?? throw new ArgumentNullException(nameof(estimationService));
    }

    public Task<ConversionEstimate> EstimateConversion(
        string inputFilePath,
        int targetBitrateKbps,
        int? targetWidth,
        int? targetHeight,
        string videoCodec,
        bool includeAudio,
        int? audioBitrateKbps,
        int? crf = null,
        bool audioCopy = false,
        CancellationToken ct = default)
    {
        return _estimationService.EstimateConversion(
            inputFilePath,
            targetBitrateKbps,
            targetWidth,
            targetHeight,
            videoCodec,
            includeAudio,
            audioBitrateKbps,
            crf,
            audioCopy,
            ct);
    }
}
