using Xunit;
using Converter.Domain.Models;

namespace Converter.Tests.UnitTests.Models;

public class AppSettingsTests
{
    [Fact]
    public void AppSettings_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var settings = new AppSettings();
        
        // Assert
        Assert.True(settings.AutoDownloadFfmpeg);
        Assert.True(settings.CheckForUpdatesOnStartup);
        Assert.True(settings.SaveQueueOnExit);
        Assert.False(settings.AutoStartConversions);
        Assert.Equal(Environment.ProcessorCount, settings.MaxConcurrentConversions);
        Assert.True(settings.UseHardwareAcceleration);
        Assert.True(settings.ShowPreviewThumbnails);
        Assert.Equal(100, settings.ThumbnailCacheSize);
        Assert.True(settings.ValidateInputFiles);
        Assert.True(settings.CheckDiskSpace);
        Assert.Equal(1000, settings.MinFreeSpaceMB);
        Assert.False(settings.EnableDetailedLogging);
    }

    [Fact]
    public void AppSettings_ShouldOverrideWithUserValues()
    {
        // Arrange
        var settings = new AppSettings();
        
        // Act
        settings.FfmpegPath = "/custom/ffmpeg/path";
        settings.MaxConcurrentConversions = 4;
        settings.MinFreeSpaceMB = 2048;
        settings.EnableDetailedLogging = true;
        
        // Assert
        Assert.Equal("/custom/ffmpeg/path", settings.FfmpegPath);
        Assert.Equal(4, settings.MaxConcurrentConversions);
        Assert.Equal(2048, settings.MinFreeSpaceMB);
        Assert.True(settings.EnableDetailedLogging);
    }

    [Fact]
    public void AppSettings_ShouldValidatePaths()
    {
        // Arrange
        var settings = new AppSettings();
        
        // Act & Assert
        Assert.Null(settings.FfmpegPath); // Should be null by default
        Assert.Null(settings.DefaultOutputFolder);
        Assert.Null(settings.TempFolderPath);
        Assert.Null(settings.LogFilePath);
    }

    [Fact]
    public void AppSettings_Clone_ShouldCreateIndependentCopy()
    {
        // Arrange
        var original = new AppSettings
        {
            FfmpegPath = "/original/path",
            MaxConcurrentConversions = 2,
            MinFreeSpaceMB = 500
        };
        
        // Act
        var clone = original.Clone();
        
        // Modify original
        original.FfmpegPath = "/modified/path";
        original.MaxConcurrentConversions = 8;
        
        // Assert
        Assert.Equal("/original/path", clone.FfmpegPath);
        Assert.Equal(2, clone.MaxConcurrentConversions);
        Assert.Equal(500, clone.MinFreeSpaceMB);
        
        // Verify original is modified
        Assert.Equal("/modified/path", original.FfmpegPath);
        Assert.Equal(8, original.MaxConcurrentConversions);
    }

    [Fact]
    public void AppSettings_ShouldHandleNullValues()
    {
        // Arrange & Act
        var settings = new AppSettings();
        
        // Assert - all nullable properties should be null by default
        Assert.Null(settings.FfmpegPath);
        Assert.Null(settings.DefaultOutputFolder);
        Assert.Null(settings.TempFolderPath);
        Assert.Null(settings.LogFilePath);
    }
}
