using System.Globalization;
using Xabe.FFmpeg;

namespace Converter.Services;

/// <summary>
/// Provides methods for applying fade effects to videos.
/// </summary>
public static class FadeEffectsService
{
    /// <summary>
    /// Applies fade in and fade out effects to a video.
    /// </summary>
    /// <param name="inputPath">Path to the input video file.</param>
    /// <param name="outputPath">Path where the output file will be saved.</param>
    /// <param name="fadeInSeconds">Duration of the fade in effect in seconds.</param>
    /// <param name="fadeOutSeconds">Duration of the fade out effect in seconds.</param>
    /// <returns>Conversion result.</returns>
    /// <exception cref="ArgumentException">Thrown when fade durations are too long for the clip.</exception>
    public static async Task<IConversionResult> ApplyFadeInOutAsync(
        string inputPath,
        string outputPath,
        double fadeInSeconds = 1.0,
        double fadeOutSeconds = 1.0)
    {
        if (string.IsNullOrWhiteSpace(inputPath))
            throw new ArgumentNullException(nameof(inputPath));
        if (string.IsNullOrWhiteSpace(outputPath))
            throw new ArgumentNullException(nameof(outputPath));

        IMediaInfo info = await FFmpeg.GetMediaInfo(inputPath);
        var duration = info.Duration;

        double totalSeconds = duration.TotalSeconds;
        if (fadeInSeconds + fadeOutSeconds >= totalSeconds)
            throw new ArgumentException("Fade durations are too long for this clip.");

        string stOut = (totalSeconds - fadeOutSeconds)
            .ToString("0.###", CultureInfo.InvariantCulture);

        string dIn = fadeInSeconds.ToString("0.###", CultureInfo.InvariantCulture);
        string dOut = fadeOutSeconds.ToString("0.###", CultureInfo.InvariantCulture);

        string vf = $"fade=t=in:st=0:d={dIn},fade=t=out:st={stOut}:d={dOut}";
        string af = $"afade=t=in:st=0:d={dIn},afade=t=out:st={stOut}:d={dOut}";

        string args =
            $"-i \"{inputPath}\" " +
            $"-vf \"{vf}\" -af \"{af}\" " +
            "-c:v libx264 -preset medium -crf 18 " +
            "-c:a aac -b:a 192k " +
            $"\"{outputPath}\"";

        return await FFmpeg.Conversions.New().Start(args);
    }

    /// <summary>
    /// Creates a crossfade transition between two videos.
    /// Note: Both videos should have the same resolution, frame rate, and other properties.
    /// If they differ, consider preprocessing them to match first.
    /// </summary>
    /// <param name="input1">Path to the first input video.</param>
    /// <param name="input2">Path to the second input video.</param>
    /// <param name="outputPath">Path where the output file will be saved.</param>
    /// <param name="crossfadeSeconds">Duration of the crossfade effect in seconds.</param>
    /// <returns>Conversion result.</returns>
    /// <exception cref="ArgumentException">Thrown when crossfade duration is invalid.</exception>
    public static async Task<IConversionResult> CrossfadeAsync(
        string input1,
        string input2,
        string outputPath,
        double crossfadeSeconds = 1.0)
    {
        if (string.IsNullOrWhiteSpace(input1))
            throw new ArgumentNullException(nameof(input1));
        if (string.IsNullOrWhiteSpace(input2))
            throw new ArgumentNullException(nameof(input2));
        if (string.IsNullOrWhiteSpace(outputPath))
            throw new ArgumentNullException(nameof(outputPath));

        IMediaInfo info1 = await FFmpeg.GetMediaInfo(input1);
        IMediaInfo info2 = await FFmpeg.GetMediaInfo(input2);

        double d1 = info1.Duration.TotalSeconds;
        double d2 = info2.Duration.TotalSeconds;

        if (crossfadeSeconds <= 0 || crossfadeSeconds >= Math.Min(d1, d2))
            throw new ArgumentException("Invalid crossfade duration.");

        string dStr = crossfadeSeconds.ToString("0.###", CultureInfo.InvariantCulture);
        string offset = (d1 - crossfadeSeconds).ToString("0.###", CultureInfo.InvariantCulture);

        string filterComplex =
            $"[0:v][1:v]xfade=transition=fade:duration={dStr}:offset={offset}[v];" +
            $"[0:a][1:a]acrossfade=d={dStr}[a]";

        string args =
            $"-i \"{input1}\" -i \"{input2}\" " +
            $"-filter_complex \"{filterComplex}\" " +
            "-map \"[v]\" -map \"[a]\" " +
            "-c:v libx264 -preset medium -crf 18 " +
            "-c:a aac -b:a 192k " +
            $"\"{outputPath}\"";

        return await FFmpeg.Conversions.New().Start(args);
    }
}
