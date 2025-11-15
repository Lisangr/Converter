using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Converter.Models;

namespace Converter.Services;

public class ShareService
{
    public ShareReport? GenerateReport(List<QueueItem> completedItems)
    {
        var successfulItems = completedItems
            .Where(x => x.Status == ConversionStatus.Completed)
            .ToList();

        if (!successfulItems.Any())
        {
            return null;
        }

        var totalProcessingTime = TimeSpan.FromTicks(
            successfulItems.Sum(x => x.ConversionDuration?.Ticks ?? 0));

        var report = new ShareReport
        {
            GeneratedAt = DateTime.Now,
            FilesConverted = successfulItems.Count,
            TotalSpaceSaved = successfulItems.Sum(x =>
                Math.Max(0, x.FileSizeBytes - (x.OutputFileSizeBytes ?? x.FileSizeBytes))),
            ProcessingTime = totalProcessingTime,
            TopCodecs = GetTopCodecs(successfulItems),
            MostUsedPreset = GetMostUsedPreset(successfulItems),
            Title = "Конвертация завершена!",
            Subtitle = $"{successfulItems.Count} файлов обработано",
            Emoji = "🎉",
            AccentColor = Color.FromArgb(76, 175, 80)
        };

        const double avgInternetSpeedMbps = 10;
        var spaceSavedMb = report.TotalSpaceSaved / (1024.0 * 1024.0);
        var timeSavedSeconds = (spaceSavedMb * 8) / avgInternetSpeedMbps;
        report.TotalTimeSaved = TimeSpan.FromSeconds(timeSavedSeconds);

        return report;
    }

    private List<string> GetTopCodecs(List<QueueItem> items)
    {
        return items
            .Select(x => x.Settings?.VideoCodec)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .GroupBy(x => x)
            .OrderByDescending(g => g.Count())
            .Take(3)
            .Select(g => g.Key!)
            .ToList();
    }

    private string GetMostUsedPreset(List<QueueItem> items)
    {
        return items
            .Select(x => x.Settings?.PresetName)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .GroupBy(x => x)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault()?.Key ?? "Не указан";
    }

    public void CopyToClipboard(string text)
    {
        try
        {
            Clipboard.SetText(text);
        }
        catch (Exception)
        {
            Thread.Sleep(100);
            Clipboard.SetText(text);
        }
    }

    public async Task<string> GenerateImageReport(ShareReport report, string outputPath)
    {
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return await Task.Run(() =>
        {
            using var bitmap = new Bitmap(800, 600);
            using var graphics = Graphics.FromImage(bitmap);

            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            using (var brush = new LinearGradientBrush(
                       new Rectangle(0, 0, 800, 600),
                       Color.FromArgb(30, 30, 30),
                       Color.FromArgb(50, 50, 50),
                       45f))
            {
                graphics.FillRectangle(brush, 0, 0, 800, 600);
            }

            using var titleFont = new Font("Segoe UI", 36, FontStyle.Bold);
            var titleText = $"{report.Emoji} {report.Title}".Trim();
            var titleSize = graphics.MeasureString(titleText, titleFont);
            graphics.DrawString(
                titleText,
                titleFont,
                Brushes.White,
                new PointF(400 - titleSize.Width / 2, 50));

            using var subtitleFont = new Font("Segoe UI", 18);
            var subtitleSize = graphics.MeasureString(report.Subtitle, subtitleFont);
            using var subtitleBrush = new SolidBrush(Color.FromArgb(180, 180, 180));
            graphics.DrawString(
                report.Subtitle,
                subtitleFont,
                subtitleBrush,
                new PointF(400 - subtitleSize.Width / 2, 110));

            using (var pen = new Pen(Color.FromArgb(100, 100, 100), 2))
            {
                graphics.DrawLine(pen, 100, 160, 700, 160);
            }

            using var statsFont = new Font("Segoe UI", 24, FontStyle.Bold);
            using var labelFont = new Font("Segoe UI", 14);

            var yPos = 200;
            DrawStatItem(graphics, report, "📁 Файлов обработано",
                report.FilesConverted.ToString(), 150, yPos, statsFont, labelFont);
            yPos += 80;

            DrawStatItem(graphics, report, "💾 Сэкономлено места",
                FormatBytes(report.TotalSpaceSaved), 150, yPos, statsFont, labelFont);
            yPos += 80;

            DrawStatItem(graphics, report, "⏱️ Время обработки",
                FormatDuration(report.ProcessingTime), 150, yPos, statsFont, labelFont);
            yPos += 80;

            DrawStatItem(graphics, report, "🎬 Популярный кодек",
                report.TopCodecs.FirstOrDefault() ?? "N/A", 150, yPos, statsFont, labelFont);

            using var footerFont = new Font("Segoe UI", 12);
            var footerText = $"Создано в VideoConverter • {report.GeneratedAt:dd.MM.yyyy HH:mm}";
            var footerSize = graphics.MeasureString(footerText, footerFont);
            using var footerBrush = new SolidBrush(Color.FromArgb(150, 150, 150));
            graphics.DrawString(
                footerText,
                footerFont,
                footerBrush,
                new PointF(400 - footerSize.Width / 2, 550));

            bitmap.Save(outputPath, ImageFormat.Png);
            return outputPath;
        });
    }

    private void DrawStatItem(Graphics g, ShareReport report, string label, string value,
        int x, int y, Font valueFont, Font labelFont)
    {
        using var labelBrush = new SolidBrush(Color.FromArgb(180, 180, 180));
        g.DrawString(label, labelFont, labelBrush, new PointF(x, y));

        var valueSize = g.MeasureString(value, valueFont);
        var accent = Color.FromArgb(40, report.AccentColor);
        using var highlightBrush = new SolidBrush(accent);
        var rect = new Rectangle(x + 450, y - 5, (int)valueSize.Width + 20, (int)valueSize.Height + 10);
        g.FillRoundedRectangle(highlightBrush, rect, 10);

        g.DrawString(value, valueFont, Brushes.White, new PointF(x + 460, y));
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

internal static class GraphicsExtensions
{
    public static void FillRoundedRectangle(this Graphics graphics, Brush brush, Rectangle bounds, int radius)
    {
        using var path = CreateRoundedRectanglePath(bounds, radius);
        graphics.FillPath(brush, path);
    }

    private static GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        int diameter = radius * 2;
        var arc = new Rectangle(rect.Location, new Size(diameter, diameter));

        path.AddArc(arc, 180, 90);
        arc.X = rect.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = rect.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = rect.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }
}
