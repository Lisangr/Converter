using System;
using System.Threading.Tasks;
using Converter.Domain.Models;
using Converter.Services;
using FluentAssertions;
using Xunit;

namespace Converter.Tests.UnitTests.Services.AudioVideo;

public class TimelineSplitServiceTests
{
    [Fact]
    public async Task SplitBySegmentsAsync_WithEmptyInputPath_ShouldThrow()
    {
        // Act
        Func<Task> act = () => TimelineSplitService.SplitBySegmentsAsync("", "out", Array.Empty<TimelineSegment>(), true);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("inputPath");
    }

    [Fact]
    public async Task SplitBySegmentsAsync_WithEmptyOutputFolder_ShouldThrow()
    {
        // Act
        Func<Task> act = () => TimelineSplitService.SplitBySegmentsAsync("in.mp4", "", Array.Empty<TimelineSegment>(), true);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("outputFolder");
    }
}
