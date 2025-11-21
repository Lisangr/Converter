using System;
using System.Collections.Generic;
using Converter.Models;
using FluentAssertions;
using Xunit;

namespace Converter.Tests.UnitTests.Models;

public class TimelineUtilsTests
{
    [Fact]
    public void Normalize_MergesOverlappingAndAdjacentSegments()
    {
        var segments = new List<TimelineSegment>
        {
            new() { Start = TimeSpan.Zero, End = TimeSpan.FromSeconds(3), Enabled = true },
            new() { Start = TimeSpan.FromSeconds(2), End = TimeSpan.FromSeconds(5), Enabled = true },
            new() { Start = TimeSpan.FromSeconds(5), End = TimeSpan.FromSeconds(7), Enabled = true }
        };

        var normalized = TimelineUtils.Normalize(segments);

        normalized.Should().HaveCount(1);
        normalized[0].Start.Should().Be(TimeSpan.Zero);
        normalized[0].End.Should().Be(TimeSpan.FromSeconds(7));
    }

    [Fact]
    public void Invert_ReturnsKeepSegments_ForRemoveList()
    {
        var remove = new List<TimelineSegment>
        {
            new() { Start = TimeSpan.FromSeconds(2), End = TimeSpan.FromSeconds(4) },
            new() { Start = TimeSpan.FromSeconds(6), End = TimeSpan.FromSeconds(7) }
        };

        var keep = TimelineUtils.Invert(remove, TimeSpan.FromSeconds(10));

        keep.Should().HaveCount(3);
        keep[0].Start.Should().Be(TimeSpan.Zero);
        keep[0].End.Should().Be(TimeSpan.FromSeconds(2));
        keep[1].Start.Should().Be(TimeSpan.FromSeconds(4));
        keep[1].End.Should().Be(TimeSpan.FromSeconds(6));
        keep[2].Start.Should().Be(TimeSpan.FromSeconds(7));
        keep[2].End.Should().Be(TimeSpan.FromSeconds(10));
        keep.Should().OnlyContain(s => s.Enabled && s.Label == "Keep");
    }
}
