namespace Converter.Domain.Models;

public sealed record ConversionProfile(
    string Name,
    string Container,
    string VideoCodec,
    string AudioCodec,
    int? VideoBitrateKbps,
    int? AudioBitrateKbps,
    IReadOnlyDictionary<string, string> AdditionalArguments);

public sealed record ConversionRequest(
    string InputPath,
    string OutputDirectory,
    ConversionProfile Profile,
    IReadOnlyDictionary<string, string> Metadata,
    CancellationToken CancellationToken);

public abstract record ConversionResult
{
    private ConversionResult() { }

    public sealed record Success(string InputPath, string OutputPath, TimeSpan Duration) : ConversionResult;
    public sealed record Failure(string InputPath, string Reason) : ConversionResult;
}

public sealed record ConversionProgress(double Percentage, TimeSpan? EstimatedRemaining, string Stage);

public sealed record MediaStreamInfo(string Codec, string? PixelFormat, string? Resolution, TimeSpan Duration);

public sealed record MediaInfo(MediaStreamInfo Video, MediaStreamInfo Audio);

public sealed record QueueItem(Guid Id, ConversionRequest Request);

public sealed record ThumbnailRequest(string MediaPath, TimeSpan CapturePosition, int Width, int Height);

public sealed record ConversionCommand(IReadOnlyList<string> Arguments);
