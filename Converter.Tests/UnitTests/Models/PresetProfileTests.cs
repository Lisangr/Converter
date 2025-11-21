using Xunit;
using Converter.Application.Models;

namespace Converter.Tests.UnitTests.Models;

public class PresetProfileTests
{
    [Fact]
    public void PresetProfile_ShouldExposeName()
    {
        // Arrange & Act
        var preset = new PresetProfile
        {
            Id = "youtube-hd",
            Name = "YouTube HD",
            Description = "Оптимизировано для загрузки на YouTube в HD качестве"
        };
        
        // Assert
        Assert.Equal("youtube-hd", preset.Id);
        Assert.Equal("YouTube HD", preset.Name);
        Assert.Equal("Оптимизировано для загрузки на YouTube в HD качестве", preset.Description);
    }

    [Fact]
    public void PresetProfile_ShouldMapToConversionSettings()
    {
        // Arrange
        var preset = new PresetProfile
        {
            Id = "instagram-reels",
            Name = "Instagram Reels",
            Category = "Social Media",
            VideoCodec = "libx264",
            AudioCodec = "aac",
            AudioBitrate = 128,
            CRF = 23,
            Format = "mp4",
            Width = 1080,
            Height = 1920,
            IncludeAudio = true,
            MaxFileSizeMB = 100,
            MaxDurationSeconds = 60
        };
        
        // Act & Assert - Verify all conversion settings are properly mapped
        Assert.Equal("libx264", preset.VideoCodec);
        Assert.Equal("aac", preset.AudioCodec);
        Assert.Equal(128, preset.AudioBitrate);
        Assert.Equal(23, preset.CRF);
        Assert.Equal("mp4", preset.Format);
        Assert.Equal(1080, preset.Width);
        Assert.Equal(1920, preset.Height);
        Assert.True(preset.IncludeAudio);
        Assert.Equal(100, preset.MaxFileSizeMB);
        Assert.Equal(60, preset.MaxDurationSeconds);
    }

    [Fact]
    public void PresetProfile_ShouldIndicateCategory()
    {
        // Arrange & Act
        var socialMediaPreset = new PresetProfile
        {
            Name = "TikTok",
            Category = "Social Media"
        };
        
        var compressionPreset = new PresetProfile
        {
            Name = "Maximum Compression",
            Category = "Compression"
        };
        
        var platformPreset = new PresetProfile
        {
            Name = "Vimeo 4K",
            Category = "Video Platforms"
        };
        
        // Assert
        Assert.Equal("Social Media", socialMediaPreset.Category);
        Assert.Equal("Compression", compressionPreset.Category);
        Assert.Equal("Video Platforms", platformPreset.Category);
    }

    [Fact]
    public void PresetProfile_ShouldHandleVideoSettings()
    {
        // Arrange
        var preset = new PresetProfile
        {
            VideoCodec = "libx265",
            Bitrate = 5000,
            Width = 3840,
            Height = 2160,
            CRF = 18,
            Format = "mkv"
        };
        
        // Assert
        Assert.Equal("libx265", preset.VideoCodec);
        Assert.Equal(5000, preset.Bitrate);
        Assert.Equal(3840, preset.Width);
        Assert.Equal(2160, preset.Height);
        Assert.Equal(18, preset.CRF);
        Assert.Equal("mkv", preset.Format);
    }

    [Fact]
    public void PresetProfile_ShouldHandleAudioSettings()
    {
        // Arrange
        var preset = new PresetProfile
        {
            AudioCodec = "flac",
            AudioBitrate = 320,
            IncludeAudio = true
        };
        
        // Test audio disabled
        var noAudioPreset = new PresetProfile
        {
            AudioCodec = "aac",
            AudioBitrate = 128,
            IncludeAudio = false
        };
        
        // Assert
        Assert.Equal("flac", preset.AudioCodec);
        Assert.Equal(320, preset.AudioBitrate);
        Assert.True(preset.IncludeAudio);
        
        Assert.Equal("aac", noAudioPreset.AudioCodec);
        Assert.Equal(128, noAudioPreset.AudioBitrate);
        Assert.False(noAudioPreset.IncludeAudio);
    }

    [Fact]
    public void PresetProfile_ShouldHandleConstraints()
    {
        // Arrange
        var preset = new PresetProfile
        {
            MaxFileSizeMB = 500,
            MaxDurationSeconds = 1800 // 30 minutes
        };
        
        // Assert
        Assert.Equal(500, preset.MaxFileSizeMB);
        Assert.Equal(1800, preset.MaxDurationSeconds);
    }

    [Fact]
    public void PresetProfile_ShouldHandleUISettings()
    {
        // Arrange
        var preset = new PresetProfile
        {
            Name = "Pro Preset",
            Icon = "video-camera",
            ColorHex = "#FF5722",
            IsPro = true
        };
        
        // Assert
        Assert.Equal("Pro Preset", preset.Name);
        Assert.Equal("video-camera", preset.Icon);
        Assert.Equal("#FF5722", preset.ColorHex);
        Assert.True(preset.IsPro);
    }

    [Fact]
    public void PresetProfile_ShouldHandleNullAndEmptyValues()
    {
        // Arrange & Act
        var preset = new PresetProfile();
        
        // Assert - Default values should be empty strings or null
        Assert.Equal(string.Empty, preset.Id);
        Assert.Equal(string.Empty, preset.Name);
        Assert.Equal(string.Empty, preset.Description);
        Assert.Equal(string.Empty, preset.Category);
        Assert.Null(preset.VideoCodec);
        Assert.Null(preset.Bitrate);
        Assert.Null(preset.Width);
        Assert.Null(preset.Height);
        Assert.Null(preset.CRF);
        Assert.Null(preset.Format);
        Assert.Null(preset.AudioCodec);
        Assert.Null(preset.AudioBitrate);
        Assert.False(preset.IncludeAudio);
        Assert.Null(preset.MaxFileSizeMB);
        Assert.Null(preset.MaxDurationSeconds);
        Assert.Null(preset.Icon);
        Assert.Null(preset.ColorHex);
        Assert.False(preset.IsPro);
    }

    [Fact]
    public void PresetProfile_ShouldSupportDifferentCategories()
    {
        // Test various preset categories
        var categories = new[]
        {
            "Social Media",
            "Video Platforms", 
            "Compression",
            "Archival",
            "Streaming",
            "Mobile",
            "Web",
            "Custom"
        };
        
        foreach (var category in categories)
        {
            var preset = new PresetProfile
            {
                Name = $"{category} Preset",
                Category = category
            };
            
            Assert.Equal(category, preset.Category);
            Assert.Equal($"{category} Preset", preset.Name);
        }
    }

    [Fact]
    public void PresetProfile_ShouldHandleEdgeCaseValues()
    {
        // Test edge cases
        var preset = new PresetProfile
        {
            Bitrate = 0, // Minimum bitrate
            AudioBitrate = 0,
            Width = 1, // Minimum width
            Height = 1, // Minimum height
            MaxFileSizeMB = 1, // Minimum file size
            MaxDurationSeconds = 1, // Minimum duration
            IncludeAudio = true
        };
        
        Assert.Equal(0, preset.Bitrate);
        Assert.Equal(0, preset.AudioBitrate);
        Assert.Equal(1, preset.Width);
        Assert.Equal(1, preset.Height);
        Assert.Equal(1, preset.MaxFileSizeMB);
        Assert.Equal(1, preset.MaxDurationSeconds);
        Assert.True(preset.IncludeAudio);
    }
}
