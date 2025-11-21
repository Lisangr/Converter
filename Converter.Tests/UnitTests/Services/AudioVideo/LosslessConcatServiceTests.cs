using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Converter.Services;
using FluentAssertions;
using Xunit;

namespace Converter.Tests.UnitTests.Services.AudioVideo;

public class LosslessConcatServiceTests
{
    [Fact]
    public async Task ConcatLosslessAsync_WithEmptyList_ShouldThrow()
    {
        // Act
        Func<Task> act = () => LosslessConcatService.ConcatLosslessAsync(new List<string>(), "out.mp4");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("inputFiles");
    }

    [Fact]
    public async Task ConcatLosslessAsync_WithDifferentExtensions_ShouldThrow()
    {
        // Arrange
        var files = new List<string> { "a.mp4", "b.mkv" };

        // Act
        Func<Task> act = () => LosslessConcatService.ConcatLosslessAsync(files, "out.mp4");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
