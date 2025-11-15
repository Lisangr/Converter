using Xabe.FFmpeg;

namespace Converter.Services;

/// <summary>
/// Provides methods for lossless video cutting operations.
/// </summary>
public static class LosslessCutService
{
    /// <summary>
    /// Performs fast cutting without re-encoding (cuts on keyframes).
    /// Note: Cutting is done on the nearest keyframe, which might result in imprecise cuts.
    /// For frame-accurate cutting, use <see cref="CutReencodeEdgesAsync"/>.
    /// </summary>
    /// <param name="inputPath">Path to the input video file.</param>
    /// <param name="outputPath">Path where the output file will be saved.</param>
    /// <param name="start">Start time of the segment to keep.</param>
    /// <param name="end">End time of the segment to keep.</param>
    /// <returns>Conversion result.</returns>
    public static async Task<IConversionResult> CutLosslessAsync(
        string inputPath,
        string outputPath,
        TimeSpan start,
        TimeSpan end)
    {
        if (string.IsNullOrWhiteSpace(inputPath))
            throw new ArgumentNullException(nameof(inputPath));
        if (string.IsNullOrWhiteSpace(outputPath))
            throw new ArgumentNullException(nameof(outputPath));
        if (end <= start)
            throw new ArgumentException("End time must be greater than start time.");

        string ss = start.ToFFmpeg();
        string to = end.ToFFmpeg();

        // Using -ss before -i for faster seeking (but less precise)
        string args =
            $"-ss {ss} -to {to} -i \"{inputPath}\" " +
            "-c copy -avoid_negative_ts make_zero " +
            $"\"{outputPath}\"";

        return await FFmpeg.Conversions.New().Start(args);
    }

    /// <summary>
    /// Performs frame-accurate cutting by re-encoding the video.
    /// Slower than <see cref="CutLosslessAsync"/> but provides precise cuts.
    /// </summary>
    /// <param name="inputPath">Path to the input video file.</param>
    /// <param name="outputPath">Path where the output file will be saved.</param>
    /// <param name="start">Start time of the segment to keep.</param>
    /// <param name="end">End time of the segment to keep.</param>
    /// <returns>Conversion result.</returns>
    public static async Task<IConversionResult> CutReencodeEdgesAsync(
        string inputPath,
        string outputPath,
        TimeSpan start,
        TimeSpan end)
    {
        if (string.IsNullOrWhiteSpace(inputPath))
            throw new ArgumentNullException(nameof(inputPath));
        if (string.IsNullOrWhiteSpace(outputPath))
            throw new ArgumentNullException(nameof(outputPath));
        if (end <= start)
            throw new ArgumentException("End time must be greater than start time.");

        string ss = start.ToFFmpeg();
        string to = end.ToFFmpeg();

        // Using -ss after -i for frame accuracy (slower but more precise)
        string args =
            $"-i \"{inputPath}\" -ss {ss} -to {to} " +
            "-c:v libx264 -preset medium -crf 18 " +
            "-c:a copy " +
            $"\"{outputPath}\"";

        return await FFmpeg.Conversions.New().Start(args);
    }
}
