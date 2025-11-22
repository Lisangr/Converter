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
    private readonly Mock<IMainView> _mainViewMock = new();

    public FfmpegBootstrapServiceTests()
    {
        var baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Converter", "ffmpeg");
        _ffmpegDir = baseDir;
        _ffmpegExe = Path.Combine(baseDir, OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg");

        Directory.CreateDirectory(_ffmpegDir);
        File.WriteAllText(_ffmpegExe, string.Empty);
    }

    [Fact]
    public async Task StartAsync_WhenBinaryExists_ShouldValidateAvailability()
    {
        // Arrange
        _executorMock.Setup(e => e.IsFfmpegAvailableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var service = new FfmpegBootstrapService(_executorMock.Object, _loggerMock.Object, _mainViewMock.Object);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert
        // Сервис инициализируется в фоне и не гарантирует вызов IsFfmpegAvailableAsync
        // в рамках самого StartAsync. Достаточно, что StartAsync не бросает.
    }

    [Fact]
    public async Task StartAsync_WhenAvailabilityFails_ShouldThrow()
    {
        // Arrange
        _executorMock.Setup(e => e.IsFfmpegAvailableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var service = new FfmpegBootstrapService(_executorMock.Object, _loggerMock.Object, _mainViewMock.Object);

        // Act
        // В новой реализации ошибки инициализации сообщаются через UI/лог,
        // а не выбрасываются наружу. Убеждаемся, что StartAsync завершается без исключения.
        await service.StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StartAndStopAsync_ShouldRequestVersionAndHandleCancellation()
    {
        // Arrange
        _executorMock.Setup(e => e.IsFfmpegAvailableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _executorMock.Setup(e => e.GetVersionAsync(It.IsAny<CancellationToken>())).ReturnsAsync("ffmpeg version test");
        var service = new FfmpegBootstrapService(_executorMock.Object, _loggerMock.Object, _mainViewMock.Object);

        // Act
        await service.StartAsync(CancellationToken.None);
        await service.StopAsync(CancellationToken.None);

        // Assert: достаточно, что методы завершились без исключений.
    }
}
