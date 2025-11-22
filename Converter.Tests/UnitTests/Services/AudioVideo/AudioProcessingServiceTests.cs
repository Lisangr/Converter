using System.Collections.Generic;
using Converter.Domain.Models;
using Converter.Services;
using FluentAssertions;
using Xunit;

namespace Converter.Tests.UnitTests.Services.AudioVideo;

public class AudioProcessingServiceTests
{
    [Fact]
    public void BuildAudioFilterString_WithNullOptions_ShouldReturnNull()
    {
        AudioProcessingService.BuildAudioFilterString(null).Should().BeNull();
    }

    [Fact]
    public void BuildAudioFilterString_WithNormalizationAndNoiseReduction_ShouldContainBothFilters()
    {
        var options = new AudioProcessingOptions
        {
            NormalizeVolume = true,
            NormalizationMode = VolumeNormalizationMode.Peak,
            NoiseReduction = true,
            NoiseReductionStrength = NoiseReductionStrength.Medium
        };

        var filter = AudioProcessingService.BuildAudioFilterString(options);

        filter.Should().NotBeNull();
        filter.Should().Contain("loudnorm");
        filter.Should().Contain("afftdn");
    }

    [Fact]
    public void BuildAudioFilterString_WithCustomEqualizer_ShouldUseCustomBands()
    {
        var options = new AudioProcessingOptions
        {
            UseEqualizer = true,
            EqualizerPreset = EqualizerPreset.Custom,
            CustomEQBands = new Dictionary<int, double> { [100] = 3, [1000] = -2 }
        };

        var filter = AudioProcessingService.BuildAudioFilterString(options);

        filter.Should().NotBeNull();
        filter.Should().Contain("equalizer=f=100");
        filter.Should().Contain("equalizer=f=1000");
    }
}
