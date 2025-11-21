using System;
using System.Threading.Tasks;
using Converter.Services;
using FluentAssertions;
using Xunit;

namespace Converter.Tests.UnitTests.Services.AudioVideo;

public class FadeEffectsServiceTests
{
    [Fact]
    public async Task ApplyFadeInOutAsync_WithEmptyInputPath_ShouldThrow()
    {
        // Act
        Func<Task> act = () => FadeEffectsService.ApplyFadeInOutAsync("", "out.mp4");

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("inputPath");
    }

    [Fact]
    public async Task ApplyFadeInOutAsync_WithEmptyOutputPath_ShouldThrow()
    {
        // Act
        Func<Task> act = () => FadeEffectsService.ApplyFadeInOutAsync("in.mp4", "");

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("outputPath");
    }

    [Fact]
    public async Task CrossfadeAsync_WithEmptyInput1_ShouldThrow()
    {
        // Act
        Func<Task> act = () => FadeEffectsService.CrossfadeAsync("", "b.mp4", "out.mp4");

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("input1");
    }

    [Fact]
    public async Task CrossfadeAsync_WithEmptyInput2_ShouldThrow()
    {
        // Act
        Func<Task> act = () => FadeEffectsService.CrossfadeAsync("a.mp4", "", "out.mp4");

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("input2");
    }

    [Fact]
    public async Task CrossfadeAsync_WithEmptyOutputPath_ShouldThrow()
    {
        // Act
        Func<Task> act = () => FadeEffectsService.CrossfadeAsync("a.mp4", "b.mp4", "");

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("outputPath");
    }
}
