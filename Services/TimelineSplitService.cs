using Converter.Models;
using Xabe.FFmpeg;

namespace Converter.Services;

public static class TimelineSplitService
{
    /// <summary>
    /// Splits input video into separate files for each segment.
    /// When copyStreams is true, uses -c copy for lossless splitting (faster but less precise).
    /// When false, re-encodes the video for frame-accurate splitting.
    /// </summary>
    public static async Task<IReadOnlyList<IConversionResult>> SplitBySegmentsAsync(
        string inputPath,
        string outputFolder,
        IEnumerable<TimelineSegment> rawSegments,
        bool copyStreams,
        string outputExtension = ".mp4")
    {
        if (string.IsNullOrWhiteSpace(inputPath))
            throw new ArgumentNullException(nameof(inputPath));
            
        if (string.IsNullOrWhiteSpace(outputFolder))
            throw new ArgumentNullException(nameof(outputFolder));

        Directory.CreateDirectory(outputFolder);

        var segments = TimelineUtils.Normalize(rawSegments);
        var results = new List<IConversionResult>();

        int index = 1;
        foreach (var seg in segments)
        {
            string output = Path.Combine(
                outputFolder,
                $"{Path.GetFileNameWithoutExtension(inputPath)}_part{index:D2}{outputExtension}");

            string start = seg.Start.ToFFmpeg();
            string end = seg.End.ToFFmpeg();

            // Use -c copy for lossless splitting (faster but less precise)
            // or re-encode for frame-accurate splitting
            string codecPart = copyStreams 
                ? "-c copy" 
                : "-c:v libx264 -preset medium -crf 18 -c:a aac -b:a 192k";

            string args =
                $"-ss {start} -to {end} -i \"{inputPath}\" " +
                $"{codecPart} -avoid_negative_ts make_zero \"{output}\"";

            var result = await FFmpeg.Conversions.New().Start(args);
            results.Add(result);

            index++;
        }

        return results;
    }
}
