using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Converter.Infrastructure.Ffmpeg;
using FluentAssertions;
using Xunit;

namespace Converter.Tests.UnitTests.ErrorHandling;

public class FfmpegErrorHandlingTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldThrowWhenExecutableMissing()
    {
        // Arrange
        var executor = new FFmpegExecutor(ffmpegPath: "/non-existent/ffmpeg");

        // Act
        Func<Task> act = () => executor.ExecuteAsync("-version", new Progress<double>(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task GetVersionAsync_ShouldReturnOutputWhenExecutableAvailable()
    {
        // Arrange
        var executor = new FFmpegExecutor(ffmpegPath: "/bin/echo");

        // Act
        var version = await executor.GetVersionAsync();

        // Assert
        version.Should().Contain("echo");
    }
}
