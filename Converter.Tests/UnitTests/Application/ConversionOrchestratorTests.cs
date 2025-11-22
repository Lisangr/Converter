using System;
using System.Threading;
using System.Threading.Tasks;
using Converter.Application.Abstractions;
using Converter.Application.Builders;
using Converter.Application.Models;
using Converter.Application.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Converter.Tests.UnitTests.Application;

public class ConversionOrchestratorTests
{
    private readonly Mock<IFFmpegExecutor> _executorMock = new(MockBehavior.Strict);
    private readonly Mock<IConversionCommandBuilder> _builderMock = new(MockBehavior.Strict);
    private readonly Mock<ILogger<ConversionOrchestrator>> _loggerMock = new();

    private ConversionOrchestrator CreateSut()
        => new(_executorMock.Object, _builderMock.Object, _loggerMock.Object);

    [Fact]
    public async Task ConvertAsync_WithSuccessfulExecution_ShouldReturnSuccess()
    {
        var request = new ConversionRequest("input.mkv", "output.mp4", new ConversionProfile("p", "v", "a", "128k", 23));
        var args = "-i input -o output";

        _builderMock.Setup(b => b.Build(request)).Returns(args);
        _executorMock
            .Setup(e => e.ExecuteAsync(args, It.IsAny<IProgress<double>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var sut = CreateSut();

        var result = await sut.ConvertAsync(request, new Progress<int>(_ => { }), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        _builderMock.VerifyAll();
        _executorMock.VerifyAll();
    }

    [Fact]
    public async Task ConvertAsync_ShouldPropagateProgressFromExecutor()
    {
        var request = new ConversionRequest("input.mkv", "output.mp4", new ConversionProfile("p", "v", "a", "128k", 23));
        var args = "-i input -o output";
        int? reportedProgress = null;

        _builderMock.Setup(b => b.Build(request)).Returns(args);
        _executorMock
            .Setup(e => e.ExecuteAsync(args, It.IsAny<IProgress<double>>(), It.IsAny<CancellationToken>()))
            .Callback<string, IProgress<double>, CancellationToken>((_, progress, _) =>
            {
                progress.Report(50.4);
            })
            .ReturnsAsync(0);

        var sut = CreateSut();

        var result = await sut.ConvertAsync(request, new Progress<int>(p => reportedProgress = p), CancellationToken.None);

        result.Success.Should().BeTrue();
        reportedProgress.Should().Be(50); // clamped and rounded
    }

    [Fact]
    public async Task ConvertAsync_WhenExecutorFails_ShouldReturnError()
    {
        var request = new ConversionRequest("input.mkv", "output.mp4", new ConversionProfile("p", "v", "a", "128k", 23));
        var args = "-i input -o output";

        _builderMock.Setup(b => b.Build(request)).Returns(args);
        _executorMock
            .Setup(e => e.ExecuteAsync(args, It.IsAny<IProgress<double>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var sut = CreateSut();

        var result = await sut.ConvertAsync(request, new Progress<int>(_ => { }), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("boom");
    }
}
