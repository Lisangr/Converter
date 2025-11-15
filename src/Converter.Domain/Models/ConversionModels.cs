namespace Converter.Domain.Models;

public sealed record ConversionProfile(
    string Name,
    string Container,
    string VideoCodec,
    string AudioCodec,
    IReadOnlyDictionary<string, string>? ExtraParameters = null)
{
    public IReadOnlyDictionary<string, string> ExtraParameters { get; init; } =
        ExtraParameters ?? new Dictionary<string, string>();
}

public sealed record ConversionRequest(
    string InputPath,
    string OutputDirectory,
    ConversionProfile Profile,
    IReadOnlyDictionary<string, string>? Metadata = null)
{
    public IReadOnlyDictionary<string, string> Metadata { get; init; } =
        Metadata ?? new Dictionary<string, string>();
}

public sealed record ConversionProgress(
    double Percentage,
    TimeSpan? EstimatedRemaining,
    string? CurrentFile = null);

public abstract record ConversionResult
{
    private ConversionResult() { }

    public sealed record Success(
        string InputPath,
        string OutputPath,
        TimeSpan Duration) : ConversionResult;

    public sealed record Failure(
        string InputPath,
        string Error,
        Exception? Exception = null) : ConversionResult;
}

public sealed record MediaInfo(
    TimeSpan Duration,
    int Width,
    int Height,
    double FrameRate);
