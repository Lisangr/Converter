using Xunit;
using Converter.Domain.Models;

namespace Converter.Tests.UnitTests.Models;

public class ConversionSettingsTests
{
    [Fact]
    public void ConversionSettings_ShouldValidateBitrate()
    {
        // Arrange & Act
        var settings = new ConversionSettings();
        
        // Assert - Default values
        Assert.Null(settings.Bitrate);
        Assert.Null(settings.Width);
        Assert.Null(settings.Height);
        Assert.Null(settings.AudioBitrate);
        Assert.Null(settings.Crf);
        Assert.Null(settings.Threads);
        
        // Set values
        settings.Bitrate = 2000;
        settings.Width = 1920;
        settings.Height = 1080;
        settings.AudioBitrate = 128;
        settings.Crf = 23;
        settings.Threads = 4;
        
        // Assert - Set values
        Assert.Equal(2000, settings.Bitrate);
        Assert.Equal(1920, settings.Width);
        Assert.Equal(1080, settings.Height);
        Assert.Equal(128, settings.AudioBitrate);
        Assert.Equal(23, settings.Crf);
        Assert.Equal(4, settings.Threads);
    }

    [Fact]
    public void ConversionSettings_ShouldAllowCustomOutput()
    {
        // Arrange
        var settings = new ConversionSettings
        {
            ContainerFormat = "mkv",
            PresetName = "YouTube HD"
        };
        
        // Act & Assert
        Assert.Equal("mkv", settings.ContainerFormat);
        Assert.Equal("YouTube HD", settings.PresetName);
        
        // Test other output-related properties
        settings.CopyVideo = true;
        settings.CopyAudio = false;
        settings.EnableAudio = false;
        
        Assert.True(settings.CopyVideo);
        Assert.False(settings.CopyAudio);
        Assert.False(settings.EnableAudio);
    }

    [Fact]
    public void ConversionSettings_ShouldCloneProperly()
    {
        // Arrange
        var original = new ConversionSettings
        {
            VideoCodec = "libx265",
            Bitrate = 3000,
            Width = 2560,
            Height = 1440,
            AudioCodec = "opus",
            AudioBitrate = 160,
            PresetName = "High Quality",
            ContainerFormat = "mp4",
            Crf = 20,
            EnableAudio = true,
            CopyVideo = false,
            CopyAudio = false,
            UseHardwareAcceleration = true,
            Threads = 8,
            AudioProcessing = new AudioProcessingOptions
            {
                NormalizeVolume = true,
                NoiseReduction = true,
                UseEqualizer = true,
                EqualizerPreset = EqualizerPreset.Bass
            }
        };
        
        // Act
        var clone = new ConversionSettings
        {
            VideoCodec = original.VideoCodec,
            Bitrate = original.Bitrate,
            Width = original.Width,
            Height = original.Height,
            AudioCodec = original.AudioCodec,
            AudioBitrate = original.AudioBitrate,
            PresetName = original.PresetName,
            ContainerFormat = original.ContainerFormat,
            Crf = original.Crf,
            EnableAudio = original.EnableAudio,
            CopyVideo = original.CopyVideo,
            CopyAudio = original.CopyAudio,
            UseHardwareAcceleration = original.UseHardwareAcceleration,
            Threads = original.Threads,
            AudioProcessing = original.AudioProcessing?.Clone()
        };
        
        // Modify original
        original.VideoCodec = "libx264";
        original.Bitrate = 1500;
        original.AudioProcessing!.NormalizeVolume = false;
        
        // Assert - Clone should have original values
        Assert.Equal("libx265", clone.VideoCodec);
        Assert.Equal(3000, clone.Bitrate);
        Assert.True(clone.AudioProcessing!.NormalizeVolume);
        Assert.True(clone.AudioProcessing.NoiseReduction);
        Assert.True(clone.AudioProcessing.UseEqualizer);
        Assert.Equal(EqualizerPreset.Bass, clone.AudioProcessing.EqualizerPreset);
        
        // Assert - Original should be modified
        Assert.Equal("libx264", original.VideoCodec);
        Assert.Equal(1500, original.Bitrate);
        Assert.False(original.AudioProcessing.NormalizeVolume);
    }

    [Fact]
    public void ConversionSettings_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var settings = new ConversionSettings();
        
        // Assert
        Assert.Equal("libx264", settings.VideoCodec);
        Assert.Equal("aac", settings.AudioCodec);
        Assert.Equal("mp4", settings.ContainerFormat);
        Assert.True(settings.EnableAudio);
        Assert.False(settings.CopyVideo);
        Assert.False(settings.CopyAudio);
        Assert.False(settings.UseHardwareAcceleration);
    }

    [Fact]
    public void ConversionSettings_ShouldHandleAudioProcessing()
    {
        // Arrange
        var settings = new ConversionSettings();
        
        // Act
        settings.AudioProcessing = new AudioProcessingOptions
        {
            NormalizeVolume = true,
            NoiseReduction = true,
            FadeInDuration = 2.0,
            FadeOutDuration = 3.0,
            TotalDuration = 120.0
        };
        
        // Assert
        Assert.NotNull(settings.AudioProcessing);
        Assert.True(settings.AudioProcessing.NormalizeVolume);
        Assert.True(settings.AudioProcessing.NoiseReduction);
        Assert.Equal(2.0, settings.AudioProcessing.FadeInDuration);
        Assert.Equal(3.0, settings.AudioProcessing.FadeOutDuration);
        Assert.Equal(120.0, settings.AudioProcessing.TotalDuration);
    }

    [Fact]
    public void ConversionSettings_ShouldHandleNullAudioProcessing()
    {
        // Arrange & Act
        var settings = new ConversionSettings();
        
        // Assert
        Assert.Null(settings.AudioProcessing);
    }

    [Fact]
    public void ConversionSettings_ShouldValidateContainerFormats()
    {
        // Test various container formats
        var formats = new[] { "mp4", "mkv", "avi", "mov", "webm", "flv" };
        
        foreach (var format in formats)
        {
            var settings = new ConversionSettings { ContainerFormat = format };
            Assert.Equal(format, settings.ContainerFormat);
        }
    }

    [Fact]
    public void ConversionSettings_ShouldValidateVideoCodecs()
    {
        // Test various video codecs
        var codecs = new[] { "libx264", "libx265", "libvpx-vp9", "libaom-av1" };
        
        foreach (var codec in codecs)
        {
            var settings = new ConversionSettings { VideoCodec = codec };
            Assert.Equal(codec, settings.VideoCodec);
        }
    }

    [Fact]
    public void ConversionSettings_ShouldValidateAudioCodecs()
    {
        // Test various audio codecs
        var codecs = new[] { "aac", "mp3", "opus", "flac", "libvorbis" };
        
        foreach (var codec in codecs)
        {
            var settings = new ConversionSettings { AudioCodec = codec };
            Assert.Equal(codec, settings.AudioCodec);
        }
    }
}
