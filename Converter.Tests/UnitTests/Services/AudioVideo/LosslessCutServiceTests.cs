using System;
using System.Threading.Tasks;
using Converter.Services;
using FluentAssertions;
using Xunit;

namespace Converter.Tests.UnitTests.Services.AudioVideo;

public class LosslessCutServiceTests
{
    [Fact]
    public async Task CutLosslessAsync_WithInvalidTimes_ShouldThrow()
    {
        // Act
        Func<Task> act = () => LosslessCutService.CutLosslessAsync("in.mp4", "out.mp4", TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(5));

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CutReencodeEdgesAsync_WithInvalidTimes_ShouldThrow()
    {
        // Act
        Func<Task> act = () => LosslessCutService.CutReencodeEdgesAsync("in.mp4", "out.mp4", TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(5));

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
