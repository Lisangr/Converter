using Xunit;
using Converter.Application.Models;
using System.Drawing;

namespace Converter.Tests.UnitTests.Models;

public class ShareReportTests
{
    [Fact]
    public void ShareReport_ShouldIncludeStatistics()
    {
        // Arrange
        var report = new ShareReport
        {
            GeneratedAt = DateTime.Now,
            FilesConverted = 15,
            TotalSpaceSaved = 2147483648L, // 2GB
            TotalTimeSaved = TimeSpan.FromMinutes(45),
            ProcessingTime = TimeSpan.FromMinutes(20),
            TopCodecs = new List<string> { "libx264", "libx265", "libvpx-vp9" },
            MostUsedPreset = "YouTube HD",
            Title = "Конвертация завершена!",
            Subtitle = "Отличная работа",
            Emoji = "🎉",
            AccentColor = Color.FromArgb(76, 175, 80) // Green
        };
        
        // Assert
        Assert.Equal(15, report.FilesConverted);
        Assert.Equal(2147483648L, report.TotalSpaceSaved);
        Assert.Equal(TimeSpan.FromMinutes(45), report.TotalTimeSaved);
        Assert.Equal(TimeSpan.FromMinutes(20), report.ProcessingTime);
        Assert.Equal(3, report.TopCodecs.Count);
        Assert.Equal("libx264", report.TopCodecs[0]);
        Assert.Equal("YouTube HD", report.MostUsedPreset);
        Assert.Equal("Конвертация завершена!", report.Title);
        Assert.Equal("Отличная работа", report.Subtitle);
        Assert.Equal("🎉", report.Emoji);
        Assert.Equal(Color.FromArgb(76, 175, 80), report.AccentColor);
    }

    [Fact]
    public void ShareReport_ShouldFormatMarkdown()
    {
        // Arrange
        var report = new ShareReport
        {
            FilesConverted = 10,
            TotalSpaceSaved = 1073741824L, // 1GB
            ProcessingTime = TimeSpan.FromMinutes(30),
            TopCodecs = new List<string> { "libx264" },
            MostUsedPreset = "Instagram Reels"
        };
        
        // Act
        var plainText = report.GetShareText(ShareFormat.Plain);
        
        // Assert
        Assert.Contains("Результаты конвертации", plainText);
        Assert.Contains("Файлов обработано: 10", plainText);
        Assert.Contains("Сэкономлено места: 1 GB", plainText);
        Assert.Contains("Время обработки: 30м 0с", plainText);
        Assert.Contains("Популярный кодек: libx264", plainText);
        Assert.Contains("Использованный пресет: Instagram Reels", plainText);
    }

    [Fact]
    public void ShareReport_ShouldHandleEmptyQueue()
    {
        // Arrange
        var report = new ShareReport
        {
            FilesConverted = 0,
            TotalSpaceSaved = 0,
            TotalTimeSaved = TimeSpan.Zero,
            ProcessingTime = TimeSpan.Zero,
            TopCodecs = new List<string>(),
            MostUsedPreset = ""
        };
        
        // Act
        var plainText = report.GetShareText(ShareFormat.Plain);
        var twitterText = report.GetShareText(ShareFormat.Twitter);
        var redditText = report.GetShareText(ShareFormat.Reddit);
        
        // Assert
        Assert.Contains("Файлов обработано: 0", plainText);
        Assert.Contains("Сэкономлено места: 0 B", plainText);
        Assert.Contains("Время обработки: 0с", plainText);
        
        Assert.Contains("0 видео", twitterText);
        Assert.Contains("0 B", twitterText);
        
        Assert.Contains("0 видео файлов", redditText);
        Assert.Contains("N/A", redditText); // No popular codec
    }

    [Fact]
    public void ShareReport_ShouldFormatTwitterText()
    {
        // Arrange
        var report = new ShareReport
        {
            FilesConverted = 25,
            TotalSpaceSaved = 5368709120L, // 5GB
            ProcessingTime = TimeSpan.FromHours(1),
            MostUsedPreset = "YouTube HD"
        };
        
        // Act
        var twitterText = report.GetShareText(ShareFormat.Twitter);
        
        // Assert
        Assert.Contains("💾 Сжал 25 видео и сэкономил 5 GB!", twitterText);
        Assert.Contains("⏱️ Обработка: 1ч 0м", twitterText);
        Assert.Contains("🚀 Используя #VideoConverter", twitterText);
        Assert.Contains("Попробуй сам: [ссылка]", twitterText);
    }

    [Fact]
    public void ShareReport_ShouldFormatRedditText()
    {
        // Arrange
        var report = new ShareReport
        {
            FilesConverted = 12,
            TotalSpaceSaved = 3221225472L, // 3GB
            ProcessingTime = TimeSpan.FromMinutes(45),
            TopCodecs = new List<string> { "libx265", "libx264" },
            MostUsedPreset = "High Quality"
        };
        
        // Act
        var redditText = report.GetShareText(ShareFormat.Reddit);
        
        // Assert
        Assert.Contains("## 📊 Результаты конвертации", redditText);
        Assert.Contains("Только что закончил конвертацию 12 видео файлов!", redditText);
        Assert.Contains("**Статистика:**", redditText);
        Assert.Contains("- 💾 Сэкономлено места: **3 GB**", redditText);
        Assert.Contains("- ⏱️ Время обработки: **45м 0с**", redditText);
        Assert.Contains("- 🎬 Популярный кодек: **libx265**", redditText);
        Assert.Contains("- 📱 Пресет: **High Quality**", redditText);
        Assert.Contains("Использовал: VideoConverter", redditText);
    }

    [Fact]
    public void ShareReport_ShouldFormatDiscordText()
    {
        // Arrange
        var report = new ShareReport
        {
            FilesConverted = 8,
            TotalSpaceSaved = 1073741824L, // 1GB
            ProcessingTime = TimeSpan.FromMinutes(20),
            TopCodecs = new List<string> { "libx264" }
        };
        
        // Act
        var discordText = report.GetShareText(ShareFormat.Discord);
        
        // Assert
        Assert.Contains("╔════════════════════════════════╗", discordText);
        Assert.Contains("📊 РЕЗУЛЬТАТЫ КОНВЕРТАЦИИ", discordText);
        Assert.Contains("Файлов:", discordText);
        Assert.Contains("8", discordText);
        Assert.Contains("Сэкономлено:", discordText);
        Assert.Contains("1 GB", discordText);
        Assert.Contains("Время:", discordText);
        Assert.Contains("20м 0с", discordText);
        Assert.Contains("Кодек:", discordText);
        Assert.Contains("libx264", discordText);
        Assert.Contains("╚════════════════════════════════╝", discordText);
        Assert.Contains("Powered by VideoConverter 🚀", discordText);
    }

    [Fact]
    public void ShareReport_ShouldHandleEmptyTopCodecs()
    {
        // Arrange
        var report = new ShareReport
        {
            FilesConverted = 5,
            TopCodecs = new List<string>() // Empty list
        };
        
        // Act
        var text = report.GetShareText(ShareFormat.Plain);
        
        // Assert
        Assert.Contains("Популярный кодек: N/A", text);
    }

    [Fact]
    public void ShareReport_ShouldFormatVariousFileSizes()
    {
        // Test different file size scenarios
        var testCases = new[]
        {
            (1024L, "KB"),           // 1KB
            (1048576L, "MB"),        // 1MB
            (1073741824L, "GB"),     // 1GB
            (1099511627776L, "TB"),  // 1TB
            (1536L, "KB"),           // ~1.5KB
            (1610612736L, "GB")      // ~1.5GB
        };

        foreach (var (bytes, expectedUnit) in testCases)
        {
            var report = new ShareReport
            {
                TotalSpaceSaved = bytes
            };
            
            var text = report.GetShareText(ShareFormat.Plain);
            Assert.Contains(expectedUnit, text);

            // For fractional cases ensure "1.5" or "1,5" appears
            if (bytes == 1536L || bytes == 1610612736L)
            {
                Assert.True(text.Contains("1.5") || text.Contains("1,5"));
            }
        }
    }

    [Fact]
    public void ShareReport_ShouldFormatVariousDurations()
    {
        // Test different duration scenarios
        var testCases = new[]
        {
            (TimeSpan.FromSeconds(30), "30с"),
            (TimeSpan.FromMinutes(1), "1м 0с"),
            (TimeSpan.FromMinutes(90), "1ч 30м"),
            (TimeSpan.FromHours(2), "2ч 0м"),
            (TimeSpan.FromHours(25), "25ч 0м")
        };

        foreach (var (duration, expectedFormatted) in testCases)
        {
            var report = new ShareReport
            {
                ProcessingTime = duration
            };
            
            var text = report.GetShareText(ShareFormat.Plain);
            Assert.Contains(expectedFormatted, text);
        }
    }

    [Fact]
    public void ShareReport_ShouldUseDefaultValues()
    {
        // Arrange & Act
        var report = new ShareReport();
        
        // Assert - Should have default values
        Assert.NotNull(report.TopCodecs);
        Assert.Empty(report.TopCodecs);
        Assert.Equal(string.Empty, report.Title);
        Assert.Equal(string.Empty, report.Subtitle);
        Assert.Equal("", report.Emoji);
        Assert.Equal(Color.FromArgb(76, 175, 80), report.AccentColor);
        Assert.Equal(DateTime.MinValue, report.GeneratedAt);
        Assert.Equal(0, report.FilesConverted);
        Assert.Equal(0L, report.TotalSpaceSaved);
        Assert.Equal(TimeSpan.Zero, report.TotalTimeSaved);
        Assert.Equal(TimeSpan.Zero, report.ProcessingTime);
        Assert.Equal(string.Empty, report.MostUsedPreset);
    }

    [Fact]
    public void ShareReport_ShouldHandleAllShareFormats()
    {
        // Arrange
        var report = new ShareReport
        {
            FilesConverted = 5,
            TotalSpaceSaved = 1073741824L,
            ProcessingTime = TimeSpan.FromMinutes(15),
            TopCodecs = new List<string> { "libx264" },
            MostUsedPreset = "Test Preset"
        };
        
        // Act & Assert - All formats should work
        var twitterText = report.GetShareText(ShareFormat.Twitter);
        var redditText = report.GetShareText(ShareFormat.Reddit);
        var discordText = report.GetShareText(ShareFormat.Discord);
        var plainText = report.GetShareText(ShareFormat.Plain);
        
        Assert.NotNull(twitterText);
        Assert.NotNull(redditText);
        Assert.NotNull(discordText);
        Assert.NotNull(plainText);
        
        Assert.Contains("5 видео", twitterText);
        Assert.Contains("5 видео файлов", redditText);
        Assert.Contains("Файлов:", discordText);
        Assert.Contains("5", discordText);
        Assert.Contains("Файлов обработано: 5", plainText);
    }

    [Fact]
    public void ShareReport_ShouldHandleLargeNumbers()
    {
        // Arrange - Test with very large numbers
        var report = new ShareReport
        {
            FilesConverted = 999999,
            TotalSpaceSaved = 109951162777600L, // 100TB
            ProcessingTime = TimeSpan.FromDays(365), // 1 year
            TopCodecs = new List<string> { "libx265", "libx264", "libvpx-vp9", "libaom-av1", "libvpx" }
        };
        
        // Act
        var text = report.GetShareText(ShareFormat.Plain);
        
        // Assert
        Assert.Contains("999999", text);
        Assert.Contains("100 TB", text);
        // Duration formatting now expressed in hours/minutes
        Assert.Contains("8760ч 0м", text);
    }

    [Fact]
    public void ShareReport_ShouldPreserveCustomFormatting()
    {
        // Arrange
        var report = new ShareReport
        {
            Title = "🎬 Конвертация Фильмов",
            Subtitle = "Большая партия обработана",
            Emoji = "🎭",
            AccentColor = Color.FromArgb(156, 39, 176) // Purple
        };
        
        // Act
        var text = report.GetShareText(ShareFormat.Plain);
        
        // Assert - Custom values should be preserved
        Assert.Equal("🎬 Конвертация Фильмов", report.Title);
        Assert.Equal("Большая партия обработана", report.Subtitle);
        Assert.Equal("🎭", report.Emoji);
        Assert.Equal(Color.FromArgb(156, 39, 176), report.AccentColor);
    }
}
