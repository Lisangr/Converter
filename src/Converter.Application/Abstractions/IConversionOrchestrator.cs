using System.Threading;
using System.Threading.Tasks;

namespace Converter.Application.Abstractions;

public interface IConversionOrchestrator
{
    Task ProbeAsync(string filePath, CancellationToken ct);
    Task<ConversionOutcome> ConvertAsync(ConversionRequest request, IProgress<int> progress, CancellationToken ct);
}

public sealed record ConversionRequest(string InputPath, string OutputPath, ConversionProfile Profile);
public sealed record ConversionProfile(string Name, string VideoCodec, string AudioCodec, string? AudioBitrateK, int? Crf);
public sealed record ConversionOutcome(bool Success, long? OutputSize, string? ErrorMessage);
