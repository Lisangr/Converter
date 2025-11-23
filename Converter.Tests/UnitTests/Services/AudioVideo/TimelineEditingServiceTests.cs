using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Converter.Services;
using Converter.Domain.Models;
using FluentAssertions;
using Xunit;

namespace Converter.Tests.UnitTests.Services.AudioVideo;

public class TimelineEditingServiceTests
{
    [Fact]
    public async Task CutToSingleFileAsync_WithEmptyInputPath_ShouldThrow()
    {
        // Act
        Func<Task> act = () => TimelineEditingService.CutToSingleFileAsync("", "out.mp4", Array.Empty<TimelineSegment>(), Converter.Application.Models.SegmentEditMode.KeepOnly);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("inputPath");
    }

    [Fact]
    public async Task CutToSingleFileAsync_WithEmptyOutputPath_ShouldThrow()
    {
        // Act
        Func<Task> act = () => TimelineEditingService.CutToSingleFileAsync("in.mp4", "", Array.Empty<TimelineSegment>(), Converter.Application.Models.SegmentEditMode.KeepOnly);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("outputPath");
    }
}
