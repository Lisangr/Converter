using System;
using Converter.Models;
using FluentAssertions;
using Xunit;

namespace Converter.Tests.UnitTests.Models;

public class ConversionEstimateTests
{
    [Fact]
    public void FormattingProperties_ReturnHumanFriendlyValues()
    {
        var estimate = new ConversionEstimate
        {
            InputFileSizeBytes = 1024 * 1024 * 2, // 2 MB
            EstimatedOutputSizeBytes = 1024 * 500, // ~500 KB
            SpaceSavedBytes = 1024 * 1024 * 2 - 1024 * 500,
            EstimatedDuration = TimeSpan.FromMinutes(2) + TimeSpan.FromSeconds(5),
            CompressionRatio = 0.25
        };

        estimate.InputSizeFormatted.Should().Be("2 MB");
        estimate.OutputSizeFormatted.Should().Be("500 KB");
        estimate.SpaceSavedFormatted.Should().Be("1.52 MB");
        estimate.DurationFormatted.Should().Be("2 мин 5 сек");
        estimate.CompressionPercent.Should().Be(25);
        estimate.SavingsPercent.Should().Be(75);
    }

    [Fact]
    public void FormatDuration_UsesHours_WhenLongerThanOneHour()
    {
        var estimate = new ConversionEstimate { EstimatedDuration = TimeSpan.FromHours(1) + TimeSpan.FromMinutes(15) };

        estimate.DurationFormatted.Should().Be("1 ч 15 мин");
    }
}
