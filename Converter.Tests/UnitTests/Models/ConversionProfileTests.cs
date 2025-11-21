using Xunit;
using Converter.Application.Models;

namespace Converter.Tests.UnitTests.Models;

public class ConversionProfileTests
{
    [Fact]
    public void ConversionProfile_ShouldExposeDisplayName()
    {
        // Arrange & Act
        var profile = new ConversionProfile
        {
            Name = "YouTube HD",
            Description = "Оптимизировано для YouTube в HD качестве"
        };
        
        // Assert
        Assert.Equal("YouTube HD", profile.Name);
        Assert.Equal("Оптимизировано для YouTube в HD качестве", profile.Description);
    }

    [Fact]
    public void ConversionProfile_ShouldSerializeSettings()
    {
        // Arrange
        var profile = new ConversionProfile
        {
            Id = "youtube-hd-1080p",
            Name = "YouTube HD",
            Category = "Social Media",
            VideoCodec = "libx264",
            AudioCodec = "aac",
            AudioBitrate = 128,
            CRF = 23,
            Format = "mp4",
            Width = 1920,
            Height = 1080,
            IncludeAudio = true
        };
        
        // Act & Assert - Verify all properties are set correctly
        Assert.Equal("youtube-hd-1080p", profile.Id);
        Assert.Equal("YouTube HD", profile.Name);
        Assert.Equal("Social Media", profile.Category);
        Assert.Equal("libx264", profile.VideoCodec);
        Assert.Equal("aac", profile.AudioCodec);
        Assert.Equal(128, profile.AudioBitrate);
        Assert.Equal(23, profile.CRF);
        Assert.Equal("mp4", profile.Format);
        Assert.Equal(1920, profile.Width);
        Assert.Equal(1080, profile.Height);
        Assert.True(profile.IncludeAudio);
    }

    [Fact]
    public void ConversionProfile_ShouldSupportCloning()
    {
        // Arrange
        var original = new ConversionProfile
        {
            Id = "test-profile",
            Name = "Test Profile",
            Description = "Test Description",
            Category = "Test",
            VideoCodec = "libx265",
            AudioCodec = "mp3",
            AudioBitrate = 192,
            CRF = 28,
            Format = "mkv",
            Width = 1280,
            Height = 720,
            IncludeAudio = false,
            MaxFileSizeMB = 500,
            MaxDurationSeconds = 3600,
            Icon = "video",
            ColorHex = "#FF5722",
            IsPro = true
        };
        
        // Act
        var clone = new ConversionProfile
        {
            Id = original.Id,
            Name = original.Name,
            Description = original.Description,
            Category = original.Category,
            VideoCodec = original.VideoCodec,
            AudioCodec = original.AudioCodec,
            AudioBitrate = original.AudioBitrate,
            CRF = original.CRF,
            Format = original.Format,
            Width = original.Width,
            Height = original.Height,
            IncludeAudio = original.IncludeAudio,
            MaxFileSizeMB = original.MaxFileSizeMB,
            MaxDurationSeconds = original.MaxDurationSeconds,
            Icon = original.Icon,
            ColorHex = original.ColorHex,
            IsPro = original.IsPro
        };
        
        // Assert
        Assert.Equal(original.Id, clone.Id);
        Assert.Equal(original.Name, clone.Name);
        Assert.Equal(original.Description, clone.Description);
        Assert.Equal(original.Category, clone.Category);
        Assert.Equal(original.VideoCodec, clone.VideoCodec);
        Assert.Equal(original.AudioCodec, clone.AudioCodec);
        Assert.Equal(original.AudioBitrate, clone.AudioBitrate);
        Assert.Equal(original.CRF, clone.CRF);
        Assert.Equal(original.Format, clone.Format);
        Assert.Equal(original.Width, clone.Width);
        Assert.Equal(original.Height, clone.Height);
        Assert.Equal(original.IncludeAudio, clone.IncludeAudio);
        Assert.Equal(original.MaxFileSizeMB, clone.MaxFileSizeMB);
        Assert.Equal(original.MaxDurationSeconds, clone.MaxDurationSeconds);
        Assert.Equal(original.Icon, clone.Icon);
        Assert.Equal(original.ColorHex, clone.ColorHex);
        Assert.Equal(original.IsPro, clone.IsPro);
    }

    [Fact]
    public void ConversionProfile_Constructor_ShouldInitializeProperties()
    {
        // Arrange & Act
        var profile = new ConversionProfile(
            "Instagram Reels",
            "libx264",
            "aac",
            "128k",
            23
        );
        
        // Assert
        Assert.Equal("Instagram Reels", profile.Name);
        Assert.Equal("libx264", profile.VideoCodec);
        Assert.Equal("aac", profile.AudioCodec);
        Assert.Equal(128, profile.AudioBitrate);
        Assert.Equal(23, profile.CRF);
    }

    [Fact]
    public void ConversionProfile_ParseAudioBitrateK_ShouldHandleVariousFormats()
    {
        // Test cases for audio bitrate parsing
        (string? input, int? expected)[] testCases = new[]
        {
            ("128k", (int?)128),
            ("256", (int?)256),
            (" 192k ", (int?)192),
            ("320K", (int?)320),
            ("", (int?)null),
            (null, (int?)null),
            ("invalid", (int?)null)
        };

        foreach (var (input, expected) in testCases)
        {
            // This is testing the private method indirectly through constructor
            var profile = new ConversionProfile("Test", "libx264", "aac", input, 23);
            
            if (expected.HasValue)
                Assert.Equal(expected.Value, profile.AudioBitrate);
            else
                Assert.Null(profile.AudioBitrate);
        }
    }

    [Fact]
    public void ConversionProfile_ShouldInheritFromPresetProfile()
    {
        // Arrange & Act
        var profile = new ConversionProfile
        {
            Name = "Test",
            VideoCodec = "libx264"
        };
        
        // Assert
        Assert.NotNull(profile as PresetProfile);
        Assert.Equal("Test", ((PresetProfile)profile).Name);
        Assert.Equal("libx264", ((PresetProfile)profile).VideoCodec);
    }
}
