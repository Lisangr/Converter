using System;
using System.Collections.Generic;
using Converter.Domain.Models;
using Converter.Services;
using FluentAssertions;
using Moq;
using Xabe.FFmpeg;
using Xunit;

namespace Converter.Tests.UnitTests.Services;

public class AudioProcessingServiceTests
{
    [Fact]
    public void BuildAudioFilterString_ReturnsNull_WhenOptionsAreNullOrEmpty()
    {
        AudioProcessingService.BuildAudioFilterString(null).Should().BeNull();

        var options = new AudioProcessingOptions();

        AudioProcessingService.BuildAudioFilterString(options).Should().BeNull();
    }

    [Fact]
    public void BuildAudioFilterString_ComposesNormalizationNoiseReductionAndFade()
    {
        var options = new AudioProcessingOptions
        {
            NormalizeVolume = true,
            NormalizationMode = VolumeNormalizationMode.Spotify,
            NoiseReduction = true,
            NoiseReductionStrength = NoiseReductionStrength.Strong,
            FadeInDuration = 1.5,
            FadeOutDuration = 2,
            TotalDuration = 10
        };

        var filter = AudioProcessingService.BuildAudioFilterString(options);

        filter.Should().Contain("loudnorm=I=-14:TP=-1:LRA=11");
        filter.Should().Contain("afftdn=nr=30:nf=-30");
        filter.Should().Contain("afade=t=in:d=1.5");
        filter.Should().Contain("afade=t=out:st=8:d=2");
    }

    [Fact]
    public void BuildAudioFilterString_UsesEqualizerPresetOrCustomBands()
    {
        var presetOptions = new AudioProcessingOptions
        {
            UseEqualizer = true,
            EqualizerPreset = EqualizerPreset.Rock
        };

        AudioProcessingService.BuildAudioFilterString(presetOptions)
            .Should().Contain("equalizer=f=100:t=q:w=1:g=5");

        var customOptions = new AudioProcessingOptions
        {
            UseEqualizer = true,
            EqualizerPreset = EqualizerPreset.Custom,
            CustomEQBands = new Dictionary<int, double> { [100] = 3.5, [1000] = -1 }
        };

        var customFilter = AudioProcessingService.BuildAudioFilterString(customOptions);

        customFilter.Should().Contain("equalizer=f=100:t=q:w=1:g=3.5");
        customFilter.Should().Contain("equalizer=f=1000:t=q:w=1:g=-1");
    }

    [Fact]
    public void ApplyAudioProcessing_AppendsFiltersToConversion()
    {
        var conversion = new Mock<IConversion>();
        var options = new AudioProcessingOptions
        {
            NormalizeVolume = true,
            NormalizationMode = VolumeNormalizationMode.Peak
        };

        AudioProcessingService.ApplyAudioProcessing(conversion.Object, options);

        conversion.Verify(c => c.AddParameter(It.Is<string>(s => s.Contains("-af \"loudnorm=I=-16:TP=-1.5:LRA=11\""))), Times.Once);
    }
}
