using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Converter.Infrastructure.Ffmpeg;
using FluentAssertions;
using Xunit;

namespace Converter.Tests.UnitTests.Infrastructure;

public class FFmpegExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_WithEmptyArguments_ShouldThrow()
    {
        // Arrange
        var executor = new FFmpegExecutor(ffmpegPath: null, logger: null);

        // Act
        Func<Task> act = async () => await executor.ExecuteAsync(string.Empty, new Progress<double>(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ProbeAsync_WithEmptyPath_ShouldThrow()
    {
        // Arrange
        var executor = new FFmpegExecutor();

        // Act
        Func<Task> act = async () => await executor.ProbeAsync(string.Empty, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetMediaInfoAsync_WithEmptyPath_ShouldThrow()
    {
        // Arrange
        var executor = new FFmpegExecutor();

        // Act
        Func<Task> act = async () => await executor.GetMediaInfoAsync(string.Empty, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithMissingExecutable_ShouldThrowFileNotFound()
    {
        // Arrange
        var executor = new FFmpegExecutor(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));

        // Act
        Func<Task> act = async () => await executor.ExecuteAsync("-version", new Progress<double>(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task IsFfmpegAvailableAsync_WhenExecutableMissing_ShouldReturnFalse()
    {
        // Arrange
        var executor = new FFmpegExecutor(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));

        // Act
        var available = await executor.IsFfmpegAvailableAsync();

        // Assert
        available.Should().BeFalse();
    }
}
