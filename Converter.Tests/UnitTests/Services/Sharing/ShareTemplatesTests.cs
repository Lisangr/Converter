using Converter.Application.Models;
using FluentAssertions;
using Xunit;

namespace Converter.Tests.UnitTests.Services.Sharing;

public class ShareTemplatesTests
{
    [Fact]
    public void DiscordTemplate_ShouldIncludeFramedStats()
    {
        // Arrange
        var report = new ShareReport
        {
            FilesConverted = 3,
            TotalSpaceSaved = 1_073_741_824, // 1GB
            ProcessingTime = TimeSpan.FromMinutes(10),
            TopCodecs = new List<string> { "libx264" }
        };

        // Act
        var text = report.GetShareText(ShareFormat.Discord);

        // Assert
        text.Should().Contain("╔").And.Contain("╚");
        text.Should().Contain("3").And.Contain("1 GB");
        text.Should().Contain("20м".Replace("20", "10"));
    }

    [Fact]
    public void TwitterTemplate_ShouldContainHashtags()
    {
        // Arrange
        var report = new ShareReport
        {
            FilesConverted = 5,
            TotalSpaceSaved = 2_147_483_648, // 2GB
            ProcessingTime = TimeSpan.FromMinutes(30),
            MostUsedPreset = "YouTube"
        };

        // Act
        var text = report.GetShareText(ShareFormat.Twitter);

        // Assert
        text.Should().Contain("#VideoConverter");
        text.Should().Contain("5 видео");
        text.Should().Contain("2 GB");
    }

    [Fact]
    public void RedditTemplate_ShouldSummarizeResults()
    {
        // Arrange
        var report = new ShareReport
        {
            FilesConverted = 7,
            TotalSpaceSaved = 512 * 1024 * 1024,
            ProcessingTime = TimeSpan.FromMinutes(5),
            TopCodecs = new List<string> { "libx265" },
            MostUsedPreset = "HDR"
        };

        // Act
        var text = report.GetShareText(ShareFormat.Reddit);

        // Assert
        text.Should().Contain("7 видео файлов");
        text.Should().Contain("Популярный кодек: **libx265**");
        text.Should().Contain("Пресет: **HDR**");
    }
}
