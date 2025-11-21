using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Converter.Infrastructure.Ffmpeg;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Converter.Tests.UnitTests.Infrastructure;

public class ThumbnailProviderTests
{
    [Fact]
    public async Task GetThumbnailAsync_WhenExecutorSucceeds_ShouldReturnStream()
    {
        // Arrange
        var executorMock = new Mock<IFFmpegExecutor>();
        executorMock.Setup(e => e.ExecuteAsync(It.IsAny<string>(), It.IsAny<IProgress<double>>(), It.IsAny<CancellationToken>()))
            .Returns<string, IProgress<double>, CancellationToken>((args, _, _) =>
            {
                var match = Regex.Match(args, "\"(?<path>[^\"]+)\"$", RegexOptions.RightToLeft);
                var outputPath = match.Groups["path"].Value;
                File.WriteAllBytes(outputPath, new byte[] { 1, 2, 3 });
                return Task.FromResult(0);
            });

        var provider = new ThumbnailProvider(executorMock.Object, Mock.Of<ILogger<ThumbnailProvider>>());

        // Act
        using var stream = await provider.GetThumbnailAsync("video.mp4", 320, 240, CancellationToken.None);

        // Assert
        stream.Should().NotBeNull();
        stream.Length.Should().BeGreaterThan(0);
        executorMock.Verify(e => e.ExecuteAsync(It.Is<string>(a => a.Contains("video.mp4") && a.Contains("320") && a.Contains("240")),
            It.IsAny<IProgress<double>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetThumbnailAsync_WhenExecutorFails_ShouldThrow()
    {
        // Arrange
        var executorMock = new Mock<IFFmpegExecutor>();
        executorMock.Setup(e => e.ExecuteAsync(It.IsAny<string>(), It.IsAny<IProgress<double>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        var provider = new ThumbnailProvider(executorMock.Object, Mock.Of<ILogger<ThumbnailProvider>>());

        // Act
        Func<Task> act = async () => await provider.GetThumbnailAsync("video.mp4", 120, 90, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetThumbnailAsync_WithInvalidArguments_ShouldThrow()
    {
        // Arrange
        var provider = new ThumbnailProvider(Mock.Of<IFFmpegExecutor>(), Mock.Of<ILogger<ThumbnailProvider>>());

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => provider.GetThumbnailAsync(string.Empty, 100, 100, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => provider.GetThumbnailAsync("video.mp4", 0, 100, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => provider.GetThumbnailAsync("video.mp4", 100, 0, CancellationToken.None));
    }
}
