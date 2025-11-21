using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Converter.Application.Models;

public class ShareReport
{
    public DateTime GeneratedAt { get; set; }
    public int FilesConverted { get; set; }
    public long TotalSpaceSaved { get; set; }
    public TimeSpan TotalTimeSaved { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public List<string> TopCodecs { get; set; } = new();
    public string MostUsedPreset { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Emoji { get; set; } = "";
    public Color AccentColor { get; set; } = Color.FromArgb(76, 175, 80);

    public string GetShareText(ShareFormat format)
    {
        return format switch
        {
            ShareFormat.Twitter => GenerateTwitterText(),
            ShareFormat.Reddit => GenerateRedditText(),
            ShareFormat.Discord => GenerateDiscordText(),
            ShareFormat.Plain => GeneratePlainText(),
            _ => GeneratePlainText()
        };
    }

    private string GenerateTwitterText()
    {
        return $"💾 Сжал {FilesConverted} видео и сэкономил {FormatBytes(TotalSpaceSaved)}!\n\n" +
               $"⏱️ Обработка: {FormatDuration(ProcessingTime)}\n" +
               $"🚀 Используя #VideoConverter\n\n" +
               $"Попробуй сам: [ссылка]";
    }

    private string GenerateRedditText()
    {
        return $"## 📊 Результаты конвертации\n\n" +
               $"Только что закончил конвертацию {FilesConverted} видео файлов!\n\n" +
               $"**Статистика:**\n" +
               $"- 💾 Сэкономлено места: **{FormatBytes(TotalSpaceSaved)}**\n" +
               $"- ⏱️ Время обработки: **{FormatDuration(ProcessingTime)}**\n" +
               $"- 🎬 Популярный кодек: **{TopCodecs.FirstOrDefault() ?? "N/A"}**\n" +
               $"- 📱 Пресет: **{MostUsedPreset}**\n\n" +
               $"Использовал: VideoConverter";
    }

    private string GenerateDiscordText()
    {
        var codec = TopCodecs.FirstOrDefault() ?? "N/A";
        return $"```\n" +
               $"╔════════════════════════════════╗\n" +
               $"║   📊 РЕЗУЛЬТАТЫ КОНВЕРТАЦИИ   ║\n" +
               $"╠════════════════════════════════╣\n" +
               $"║ Файлов:      {FilesConverted,15} ║\n" +
               $"║ Сэкономлено: {FormatBytes(TotalSpaceSaved),15} ║\n" +
               $"║ Время:       {FormatDuration(ProcessingTime),15} ║\n" +
               $"║ Кодек:       {codec,15} ║\n" +
               $"╚════════════════════════════════╝\n" +
               $"```\n" +
               $"Powered by VideoConverter 🚀";
    }

    private string GeneratePlainText()
    {
        return $"Результаты конвертации\n" +
               $"═══════════════════════\n\n" +
               $"Файлов обработано: {FilesConverted}\n" +
               $"Сэкономлено места: {FormatBytes(TotalSpaceSaved)}\n" +
               $"Время обработки: {FormatDuration(ProcessingTime)}\n" +
               $"Популярный кодек: {TopCodecs.FirstOrDefault() ?? "N/A"}\n" +
               $"Использованный пресет: {MostUsedPreset}";
    }

    private string FormatBytes(long bytes)
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

    private string FormatDuration(TimeSpan ts)
    {
        if (ts.TotalHours >= 1)
            return $"{(int)ts.TotalHours}ч {ts.Minutes}м";
        if (ts.TotalMinutes >= 1)
            return $"{(int)ts.TotalMinutes}м {ts.Seconds}с";
        return $"{ts.Seconds}с";
    }
}

public enum ShareFormat
{
    Twitter,
    Reddit,
    Discord,
    Plain
}