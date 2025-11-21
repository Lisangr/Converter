using Xunit;
using Converter.Domain.Models;

namespace Converter.Tests.UnitTests.Models;

public class AudioProcessingOptionsTests
{
    [Fact]
    public void AudioOptions_ShouldNormalizeByDefault()
    {
        // Arrange & Act
        var options = new AudioProcessingOptions();
        
        // Assert
        Assert.False(options.NormalizeVolume);
        Assert.Equal(VolumeNormalizationMode.Peak, options.NormalizationMode);
        Assert.False(options.NoiseReduction);
        Assert.Equal(NoiseReductionStrength.Medium, options.NoiseReductionStrength);
        Assert.False(options.UseEqualizer);
        Assert.Equal(EqualizerPreset.None, options.EqualizerPreset);
    }

    [Fact]
    public void AudioOptions_ShouldAllowNoiseReduction()
    {
        // Arrange
        var options = new AudioProcessingOptions();
        
        // Act
        options.NoiseReduction = true;
        options.NoiseReductionStrength = NoiseReductionStrength.Strong;
        
        // Assert
        Assert.True(options.NoiseReduction);
        Assert.Equal(NoiseReductionStrength.Strong, options.NoiseReductionStrength);
    }

    [Fact]
    public void AudioOptions_ShouldConfigureEqualizerBands()
    {
        // Arrange
        var options = new AudioProcessingOptions();
        
        // Act
        options.UseEqualizer = true;
        options.EqualizerPreset = EqualizerPreset.Bass;
        options.CustomEQBands[60] = 5.0;
        options.CustomEQBands[170] = 3.0;
        options.CustomEQBands[310] = 1.0;
        
        // Assert
        Assert.True(options.UseEqualizer);
        Assert.Equal(EqualizerPreset.Bass, options.EqualizerPreset);
        Assert.Equal(3, options.CustomEQBands.Count);
        Assert.Equal(5.0, options.CustomEQBands[60]);
        Assert.Equal(3.0, options.CustomEQBands[170]);
        Assert.Equal(1.0, options.CustomEQBands[310]);
    }

    [Fact]
    public void AudioOptions_Clone_ShouldCreateIndependentCopy()
    {
        // Arrange
        var original = new AudioProcessingOptions
        {
            NormalizeVolume = true,
            NoiseReduction = true,
            UseEqualizer = true,
            EqualizerPreset = EqualizerPreset.Rock,
            FadeInDuration = 2.0,
            FadeOutDuration = 3.0,
            TotalDuration = 300.0
        };
        original.CustomEQBands[60] = 5.0;
        original.CustomEQBands[170] = 3.0;
        
        // Act
        var clone = original.Clone();
        
        // Modify original
        original.NormalizeVolume = false;
        original.NoiseReduction = false;
        original.CustomEQBands[60] = 10.0;
        original.CustomEQBands[1000] = 8.0;
        
        // Assert
        Assert.True(clone.NormalizeVolume);
        Assert.True(clone.NoiseReduction);
        Assert.True(clone.UseEqualizer);
        Assert.Equal(EqualizerPreset.Rock, clone.EqualizerPreset);
        Assert.Equal(2.0, clone.FadeInDuration);
        Assert.Equal(3.0, clone.FadeOutDuration);
        Assert.Equal(300.0, clone.TotalDuration);
        
        // Verify EQ bands are independent
        Assert.Equal(2, clone.CustomEQBands.Count);
        Assert.Equal(5.0, clone.CustomEQBands[60]);
        Assert.Equal(3.0, clone.CustomEQBands[170]);
        Assert.False(clone.CustomEQBands.ContainsKey(1000));
        
        // Verify original is modified
        Assert.False(original.NormalizeVolume);
        Assert.False(original.NoiseReduction);
        Assert.Equal(3, original.CustomEQBands.Count);
        Assert.Equal(10.0, original.CustomEQBands[60]);
    }

    [Fact]
    public void AudioProcessingOptions_ShouldHandleAllEqualizerPresets()
    {
        // Arrange & Act & Assert
        var presets = Enum.GetValues<EqualizerPreset>();
        
        foreach (var preset in presets)
        {
            var options = new AudioProcessingOptions { EqualizerPreset = preset };
            Assert.Equal(preset, options.EqualizerPreset);
        }
    }

    [Fact]
    public void AudioProcessingOptions_ShouldHandleAllNoiseReductionStrengths()
    {
        // Arrange & Act & Assert
        var strengths = Enum.GetValues<NoiseReductionStrength>();
        
        foreach (var strength in strengths)
        {
            var options = new AudioProcessingOptions { NoiseReductionStrength = strength };
            Assert.Equal(strength, options.NoiseReductionStrength);
        }
    }

    [Fact]
    public void AudioProcessingOptions_ShouldHandleAllNormalizationModes()
    {
        // Arrange & Act & Assert
        var modes = Enum.GetValues<VolumeNormalizationMode>();
        
        foreach (var mode in modes)
        {
            var options = new AudioProcessingOptions { NormalizationMode = mode };
            Assert.Equal(mode, options.NormalizationMode);
        }
    }
}
