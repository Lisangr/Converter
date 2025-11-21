using System.Globalization;

namespace Converter.Application.Models;

public static class TimelineUtils
{
    public static IReadOnlyList<TimelineSegment> Normalize(
        IEnumerable<TimelineSegment> segments)
    {
        var ordered = segments
            .Where(s => s.Enabled)
            .OrderBy(s => s.Start)
            .ToList();

        foreach (var s in ordered)
            s.Validate();

        if (ordered.Count <= 1)
            return ordered;

        var result = new List<TimelineSegment>();
        var current = ordered[0];

        for (int i = 1; i < ordered.Count; i++)
        {
            var next = ordered[i];
            // если пересечение или вплотную — склеиваем
            if (next.Start <= current.End)
            {
                current.End = (next.End > current.End) ? next.End : current.End;
            }
            else
            {
                result.Add(current);
                current = next;
            }
        }

        result.Add(current);
        return result;
    }

    /// <summary>
    /// Инвертирует список "вырезать" -> "оставить" по общей длительности.
    /// </summary>
    public static IReadOnlyList<TimelineSegment> Invert(
        IEnumerable<TimelineSegment> removeSegments,
        TimeSpan fullDuration)
    {
        var normalized = Normalize(removeSegments);
        var result = new List<TimelineSegment>();

        var cursor = TimeSpan.Zero;

        foreach (var cut in normalized)
        {
            if (cut.Start > cursor)
            {
                result.Add(new TimelineSegment
                {
                    Start = cursor,
                    End = cut.Start,
                    Enabled = true,
                    Label = "Keep"
                });
            }

            cursor = cut.End;
        }

        if (cursor < fullDuration)
        {
            result.Add(new TimelineSegment
            {
                Start = cursor,
                End = fullDuration,
                Enabled = true,
                Label = "Keep"
            });
        }

        return result;
    }

    public static string ToSeconds(TimeSpan ts)
        => ts.TotalSeconds.ToString("0.###", CultureInfo.InvariantCulture);
}