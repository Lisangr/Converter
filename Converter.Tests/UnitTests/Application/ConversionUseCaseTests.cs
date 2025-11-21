using System;
using System.Threading;
using System.Threading.Tasks;
using Converter.Application.Abstractions;
using Converter.Application.Models;
using Converter.Application.Services;
using Converter.Domain.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Converter.Tests.UnitTests.Application;

public class ConversionUseCaseTests
{
    private readonly Mock<IProfileProvider> _profileProviderMock = new(MockBehavior.Strict);
    private readonly Mock<IOutputPathBuilder> _pathBuilderMock = new(MockBehavior.Strict);
    private readonly Mock<IConversionOrchestrator> _orchestratorMock = new(MockBehavior.Strict);
    private readonly Mock<ILogger<ConversionUseCase>> _loggerMock = new();

    private ConversionUseCase CreateSut()
        => new(_profileProviderMock.Object, _pathBuilderMock.Object, _orchestratorMock.Object, _loggerMock.Object);

    [Fact]
    public async Task ExecuteAsync_WithNullItem_ShouldThrow()
    {
        var sut = CreateSut();

        var act = async () => await sut.ExecuteAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ExecuteAsync_WhenOrchestratorSucceeds_ShouldReturnSuccessAndPopulateOutput()
    {
        var item = new QueueItem { FilePath = "input.mp4" };
        var profile = new ConversionProfile("Default", "h264", "aac", "128k", 23);
        const string outputPath = "out/output.mp4";
        var outcome = new ConversionOutcome(true, 2048, null);

        _profileProviderMock
            .Setup(p => p.GetDefaultProfileAsync())
            .ReturnsAsync(profile);
        _pathBuilderMock
            .Setup(b => b.BuildOutputPath(item, profile))
            .Returns(outputPath);
        _orchestratorMock
            .Setup(o => o.ConvertAsync(
                It.Is<Converter.Application.Models.ConversionRequest>(r => r.InputPath == item.FilePath && r.OutputPath == outputPath),
                It.IsAny<IProgress<int>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(outcome);

        var sut = CreateSut();

        var result = await sut.ExecuteAsync(item);

        result.Success.Should().BeTrue();
        result.OutputPath.Should().Be(outputPath);
        result.OutputFileSize.Should().Be(outcome.OutputSize);
        item.OutputPath.Should().Be(outputPath);

        _profileProviderMock.VerifyAll();
        _pathBuilderMock.VerifyAll();
        _orchestratorMock.VerifyAll();
    }

    [Fact]
    public async Task ExecuteAsync_WhenOrchestratorFails_ShouldReturnError()
    {
        var item = new QueueItem { FilePath = "input.mp4" };
        var profile = new ConversionProfile("Default", "h264", "aac", "128k", 23);
        var outcome = new ConversionOutcome(false, null, "ffmpeg failed");

        _profileProviderMock
            .Setup(p => p.GetDefaultProfileAsync())
            .ReturnsAsync(profile);
        _pathBuilderMock
            .Setup(b => b.BuildOutputPath(item, profile))
            .Returns("output.mp4");
        _orchestratorMock
            .Setup(o => o.ConvertAsync(It.IsAny<Converter.Application.Models.ConversionRequest>(), It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(outcome);

        var sut = CreateSut();

        var result = await sut.ExecuteAsync(item);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be(outcome.ErrorMessage);
    }
}
