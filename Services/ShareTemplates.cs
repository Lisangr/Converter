using System;
using System.Linq;
using Converter.Models;

namespace Converter.Services;

public static class ShareTemplates
{
    public static string GetKickstarterUpdate(ShareReport report)
    {
        return $"# 🎉 Update: Amazing Results!

" +
               $"Just tested the converter with **{report.FilesConverted} real files** and the results are incredible!

" +
               "## 📊 Stats:
" +
               $"- **Space saved:** {FormatBytes(report.TotalSpaceSaved)}
" +
               $"- **Processing time:** {FormatDuration(report.ProcessingTime)}
" +
               $"- **Most used codec:** {report.TopCodecs.FirstOrDefault() ?? "N/A"}

" +
               "This is why we need your support to make this tool even better!

" +
               "Back us now: [link]";
    }

    public static string GetInstagramStory(ShareReport report)
    {
        return $"💾 Compressed {report.FilesConverted} videos
" +
               $"Saved {FormatBytes(report.TotalSpaceSaved)}! 🎉

" +
               $"⏱️ In just {FormatDuration(report.ProcessingTime)}

" +
               "Try it yourself! 👆
" +
               "#VideoCompression #Tech #Productivity";
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = Math.Max(0, bytes);
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    private static string FormatDuration(TimeSpan ts)
    {
        if (ts.TotalHours >= 1)
            return $"{(int)ts.TotalHours}h {ts.Minutes}m";
        if (ts.TotalMinutes >= 1)
            return $"{(int)ts.TotalMinutes}m {ts.Seconds}s";
        return $"{ts.Seconds}s";
    }
}
