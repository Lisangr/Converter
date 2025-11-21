using System;
using Converter.Models;
using FluentAssertions;
using Xunit;

namespace Converter.Tests.UnitTests.Models;

public class TimelineSegmentTests
{
    [Fact]
    public void Validate_Throws_WhenEndEarlierThanStart()
    {
        var segment = new TimelineSegment
        {
            Start = TimeSpan.FromSeconds(10),
            End = TimeSpan.FromSeconds(5)
        };

        Action act = segment.Validate;

        act.Should().Throw<InvalidOperationException>()
            .Which.Message.Should().StartWith("Invalid segment [00:00:10");
    }

    [Fact]
    public void Duration_ComputedFromStartAndEnd()
    {
        var segment = new TimelineSegment
        {
            Start = TimeSpan.FromSeconds(2),
            End = TimeSpan.FromSeconds(5)
        };

        segment.Duration.Should().Be(TimeSpan.FromSeconds(3));
    }
}
