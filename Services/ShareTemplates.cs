using System;
using System.Linq;
using Converter.Application.Models;

namespace Converter.Services;

public static class ShareTemplates
{
    public static string GetKickstarterUpdate(ShareReport report)
    {
        return "# 🎉 Update: Amazing Results!" + Environment.NewLine + Environment.NewLine +
               $"Just tested the converter with **{report.FilesConverted} real files** and the results are incredible!" + Environment.NewLine + Environment.NewLine +
               "## 📊 Stats:" + Environment.NewLine +
               $"- **Space saved:** {FormatBytes(report.TotalSpaceSaved)}" + Environment.NewLine +
               $"- **Processing time:** {FormatDuration(report.ProcessingTime)}" + Environment.NewLine +
               $"- **Most used codec:** {report.TopCodecs.FirstOrDefault() ?? "N/A"}" + Environment.NewLine + Environment.NewLine +
               "This is why we need your support to make this tool even better!" + Environment.NewLine + Environment.NewLine +
               "Back us now: [link]";
    }

    public static string GetInstagramStory(ShareReport report)
    {
        return $"💾 Compressed {report.FilesConverted} videos" + Environment.NewLine +
               $"Saved {FormatBytes(report.TotalSpaceSaved)}! 🎉" + Environment.NewLine + Environment.NewLine +
               $"⏱️ In just {FormatDuration(report.ProcessingTime)}" + Environment.NewLine + Environment.NewLine +
               "Try it yourself! 👆" + Environment.NewLine +
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
