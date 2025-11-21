using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Converter.Application.Abstractions;
using Converter.Infrastructure.Ffmpeg;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Converter.Tests.UnitTests.Infrastructure;

public class FfmpegBootstrapServiceTests
{
    private readonly string _ffmpegDir;
    private readonly string _ffmpegExe;
    private readonly Mock<IFFmpegExecutor> _executorMock = new();
    private readonly Mock<ILogger<FfmpegBootstrapService>> _loggerMock = new();

    public FfmpegBootstrapServiceTests()
    {
        var baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Converter", "ffmpeg");
        _ffmpegDir = baseDir;
        _ffmpegExe = Path.Combine(baseDir, OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg");

        Directory.CreateDirectory(_ffmpegDir);
        File.WriteAllText(_ffmpegExe, string.Empty);
    }

    [Fact]
    public async Task EnsureFfmpegAsync_WhenBinaryExists_ShouldValidateAvailability()
    {
        // Arrange
        _executorMock.Setup(e => e.IsFfmpegAvailableAsync()).ReturnsAsync(true);
        var service = new FfmpegBootstrapService(_executorMock.Object, _loggerMock.Object);

        // Act
        await service.EnsureFfmpegAsync();

        // Assert
        _executorMock.Verify(e => e.IsFfmpegAvailableAsync(), Times.Once);
    }

    [Fact]
    public async Task StartAsync_WhenAvailabilityFails_ShouldThrow()
    {
        // Arrange
        _executorMock.Setup(e => e.IsFfmpegAvailableAsync()).ReturnsAsync(false);
        var service = new FfmpegBootstrapService(_executorMock.Object, _loggerMock.Object);

        // Act
        Func<Task> act = async () => await service.StartAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task StartAndStopAsync_ShouldRequestVersionAndHandleCancellation()
    {
        // Arrange
        _executorMock.Setup(e => e.IsFfmpegAvailableAsync()).ReturnsAsync(true);
        _executorMock.Setup(e => e.GetVersionAsync(It.IsAny<CancellationToken>())).ReturnsAsync("ffmpeg version test");
        var service = new FfmpegBootstrapService(_executorMock.Object, _loggerMock.Object);

        // Act
        await service.StartAsync(CancellationToken.None);
        await service.StopAsync(CancellationToken.None);

        // Assert
        _executorMock.Verify(e => e.GetVersionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
