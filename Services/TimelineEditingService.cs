using System.Text;
using Converter.Application.Models;
using Xabe.FFmpeg;

namespace Converter.Services;

public static class TimelineEditingService
{
    /// <summary>
    /// Combines multiple segments into a single output file.
    /// When mode is KeepOnly: uses the segments as-is.
    /// When mode is Remove: inverts the segments first.
    /// </summary>
    public static async Task<IConversionResult> CutToSingleFileAsync(
        string inputPath,
        string outputPath,
        IEnumerable<TimelineSegment> rawSegments,
        SegmentEditMode mode)
    {
        if (string.IsNullOrWhiteSpace(inputPath))
            throw new ArgumentNullException(nameof(inputPath));

        if (string.IsNullOrWhiteSpace(outputPath))
            throw new ArgumentNullException(nameof(outputPath));

        IMediaInfo info = await FFmpeg.GetMediaInfo(inputPath);
        var duration = info.Duration;

        IReadOnlyList<TimelineSegment> segments;

        if (mode == SegmentEditMode.KeepOnly)
        {
            segments = TimelineUtils.Normalize(rawSegments);
        }
        else
        {
            segments = TimelineUtils.Invert(rawSegments, duration);
        }

        if (segments.Count == 0)
            throw new InvalidOperationException("No active segments to process.");

        string filterComplex = BuildConcatFilter(segments);

        // Using re-encoding (libx264/aac) for precise timing
        string args =
            $"-i \"{inputPath}\" " +
            $"-filter_complex \"{filterComplex}\" " +
            "-map \"[v]\" -map \"[a]\" " +
            "-c:v libx264 -preset medium -crf 18 " +
            "-c:a aac -b:a 192k " +
            $"\"{outputPath}\"";

        return await FFmpeg.Conversions.New().Start(args);
    }

    private static string BuildConcatFilter(IReadOnlyList<TimelineSegment> segments)
    {
        var sb = new StringBuilder();

        var vLabels = new List<string>();
        var aLabels = new List<string>();

        for (int i = 0; i < segments.Count; i++)
        {
            var seg = segments[i];
            var v = $"v{i}";
            var a = $"a{i}";

            string start = TimelineUtils.ToSeconds(seg.Start);
            string end = TimelineUtils.ToSeconds(seg.End);

            // [0:v]trim=... [v0]; [0:a]atrim=... [a0];
            sb.Append(
                $"[0:v]trim=start={start}:end={end},setpts=PTS-STARTPTS[{v}];" +
                $"[0:a]atrim=start={start}:end={end},asetpts=PTS-STARTPTS[{a}];");

            vLabels.Add(v);
            aLabels.Add(a);
        }

        // [v0][a0][v1][a1]...concat=n=N:v=1:a=1[v][a]
        for (int i = 0; i < segments.Count; i++)
        {
            sb.Append($"[{vLabels[i]}][{aLabels[i]}]");
        }

        sb.Append($"concat=n={segments.Count}:v=1:a=1[v][a]");

        return sb.ToString();
    }
}
