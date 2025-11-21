using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Converter.Application.Abstractions;
using Converter.Application.Models;
using Converter.Application.Services;
using Converter.Domain.Models;
using Converter.Infrastructure.Ffmpeg;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Converter.Tests.IntegrationTests;

public class EndToEndConversionTests : IDisposable
{
    private readonly Mock<IFFmpegExecutor> _executorMock;
    private readonly Mock<IConversionCommandBuilder> _builderMock;
    private readonly Mock<ILogger<ConversionOrchestrator>> _loggerMock;
    private readonly Mock<IProfileProvider> _profileProviderMock;
    private readonly Mock<IOutputPathBuilder> _pathBuilderMock;
    private readonly Mock<ILogger<ConversionUseCase>> _useCaseLoggerMock;
    private readonly Mock<ILogger<QueueProcessor>> _processorLoggerMock;
    private readonly Mock<IQueueRepository> _queueRepositoryMock;
    private readonly Mock<IQueueStore> _queueStoreMock;
    private readonly ConversionOrchestrator _orchestrator;
    private readonly ConversionUseCase _useCase;
    private readonly QueueProcessor _processor;
    private readonly string _testDirectory;

    public EndToEndConversionTests()
    {
        _executorMock = new Mock<IFFmpegExecutor>();
        _builderMock = new Mock<IConversionCommandBuilder>();
        _loggerMock = new Mock<ILogger<ConversionOrchestrator>>();
        _profileProviderMock = new Mock<IProfileProvider>();
        _pathBuilderMock = new Mock<IOutputPathBuilder>();
        _useCaseLoggerMock = new Mock<ILogger<ConversionUseCase>>();
        _processorLoggerMock = new Mock<ILogger<QueueProcessor>>();
        _queueRepositoryMock = new Mock<IQueueRepository>();
        _queueStoreMock = new Mock<IQueueStore>();

        _orchestrator = new ConversionOrchestrator(
            _executorMock.Object, 
            _builderMock.Object, 
            _loggerMock.Object);

        _useCase = new ConversionUseCase(
            _profileProviderMock.Object,
            _pathBuilderMock.Object,
            _orchestrator,
            _useCaseLoggerMock.Object);

        _processor = new QueueProcessor(
            _queueRepositoryMock.Object,
            _queueStoreMock.Object,
            _useCase,
            _processorLoggerMock.Object);

        _testDirectory = Path.Combine(Path.GetTempPath(), "ConverterIntegrationTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_testDirectory);
    }

    [Fact]
    public async Task FullConversion_ShouldProduceExpectedOutput()
    {
        // Arrange
        var inputFile = Path.Combine(_testDirectory, "input.mp4");
        var outputFile = Path.Combine(_testDirectory, "output.mp4");
        var profile = new ConversionProfile("Test", "libx264", "aac", "128k", 23);
        var queueItem = new QueueItem
        {
            Id = Guid.NewGuid(),
            FilePath = inputFile,
            Status = ConversionStatus.Pending,
            Settings = new ConversionSettings
            {
                VideoCodec = "libx264",
                AudioCodec = "aac",
                Crf = 23
            }
        };

        // Создаем тестовый файл
        await File.WriteAllTextAsync(inputFile, "test video content");

        // Настройка моков
        var request = new ConversionRequest(inputFile, outputFile, profile);
        var args = new FFmpegArguments("ffmpeg", "-i input.mp4 -c:v libx264 -c:a aac -b:a 128k -crf 23 output.mp4");

        _profileProviderMock.Setup(p => p.GetDefaultProfileAsync())
            .ReturnsAsync(profile);
        _pathBuilderMock.Setup(b => b.BuildOutputPath(queueItem, profile))
            .Returns(outputFile);
        _builderMock.Setup(b => b.Build(request)).Returns(args);
        _executorMock.Setup(e => e.ExecuteAsync(args, It.IsAny<IProgress<double>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _queueStoreMock.Setup(s => s.TryReserveAsync(queueItem.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _queueRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<QueueItem>()))
            .Returns(Task.CompletedTask);
        _queueStoreMock.Setup(s => s.CompleteAsync(
            queueItem.Id, 
            ConversionStatus.Completed, 
            null, 
            It.IsAny<long?>(), 
            It.IsAny<DateTime?>(), 
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var itemStarted = false;
        var itemCompleted = false;
        _processor.ItemStarted += (_, item) => { if (item.Id == queueItem.Id) itemStarted = true; };
        _processor.ItemCompleted += (_, item) => { if (item.Id == queueItem.Id) itemCompleted = true; };

        // Act
        await _processor.ProcessItemAsync(queueItem);

        // Assert
        itemStarted.Should().BeTrue();
        itemCompleted.Should().BeTrue();
        queueItem.Status.Should().Be(ConversionStatus.Completed);
        queueItem.OutputPath.Should().Be(outputFile);
        
        _executorMock.Verify(e => e.ExecuteAsync(args, It.IsAny<IProgress<double>>(), It.IsAny<CancellationToken>()), Times.Once);
        _queueRepositoryMock.Verify(r => r.UpdateAsync(It.Is<QueueItem>(i => i.Status == ConversionStatus.Completed)), Times.AtLeastOnce);
    }

    [Fact]
    public async Task FullConversion_ShouldReportProgress()
    {
        // Arrange
        var inputFile = Path.Combine(_testDirectory, "progress_test.mp4");
        var outputFile = Path.Combine(_testDirectory, "progress_output.mp4");
        var profile = new ConversionProfile("ProgressTest", "libx264", "aac", "128k", 23);
        var queueItem = new QueueItem
        {
            Id = Guid.NewGuid(),
            FilePath = inputFile,
            Status = ConversionStatus.Pending
        };

        await File.WriteAllTextAsync(inputFile, "progress test content");

        var request = new ConversionRequest(inputFile, outputFile, profile);
        var args = new FFmpegArguments("ffmpeg", "-i progress_test.mp4 -c:v libx264 progress_output.mp4");

        var progressUpdates = new List<int>();
        var progress = new Progress<int>(p => progressUpdates.Add(p));

        _profileProviderMock.Setup(p => p.GetDefaultProfileAsync()).ReturnsAsync(profile);
        _pathBuilderMock.Setup(b => b.BuildOutputPath(queueItem, profile)).Returns(outputFile);
        _builderMock.Setup(b => b.Build(request)).Returns(args);
        
        var progressValue = 0;
        _executorMock.Setup(e => e.ExecuteAsync(args, It.IsAny<IProgress<double>>(), It.IsAny<CancellationToken>()))
            .Callback<FFmpegArguments, IProgress<double>, CancellationToken>((_, prog, _) =>
            {
                // Симулируем прогресс
                for (int i = 0; i <= 100; i += 25)
                {
                    prog.Report(i);
                    progressValue = i;
                }
            })
            .ReturnsAsync(0);

        _queueStoreMock.Setup(s => s.TryReserveAsync(queueItem.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _queueRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<QueueItem>())).Returns(Task.CompletedTask);
        _queueStoreMock.Setup(s => s.CompleteAsync(
            queueItem.Id, ConversionStatus.Completed, null, It.IsAny<long?>(), 
            It.IsAny<DateTime?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        await _processor.ProcessItemAsync(queueItem, progress);

        // Assert
        progressUpdates.Should().NotBeEmpty();
        progressUpdates.Should().ContainInOrder(0, 25, 50, 75, 100);
        queueItem.Progress.Should().Be(100);
    }

    [Fact]
    public async Task FullConversion_ShouldHandleCancellation()
    {
        // Arrange
        var inputFile = Path.Combine(_testDirectory, "cancel_test.mp4");
        var outputFile = Path.Combine(_testDirectory, "cancel_output.mp4");
        var profile = new ConversionProfile("CancelTest", "libx264", "aac", "128k", 23);
        var queueItem = new QueueItem
        {
            Id = Guid.NewGuid(),
            FilePath = inputFile,
            Status = ConversionStatus.Pending
        };

        await File.WriteAllTextAsync(inputFile, "cancellation test content");

        var request = new ConversionRequest(inputFile, outputFile, profile);
        var args = new FFmpegArguments("ffmpeg", "-i cancel_test.mp4 -c:v libx264 cancel_output.mp4");

        using var cts = new CancellationTokenSource();
        
        _profileProviderMock.Setup(p => p.GetDefaultProfileAsync()).ReturnsAsync(profile);
        _pathBuilderMock.Setup(b => b.BuildOutputPath(queueItem, profile)).Returns(outputFile);
        _builderMock.Setup(b => b.Build(request)).Returns(args);
        
        _executorMock.Setup(e => e.ExecuteAsync(args, It.IsAny<IProgress<double>>(), cts.Token))
            .ThrowsAsync(new OperationCanceledException());
        
        _queueStoreMock.Setup(s => s.TryReserveAsync(queueItem.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _queueRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<QueueItem>())).Returns(Task.CompletedTask);
        _queueStoreMock.Setup(s => s.CompleteAsync(
            queueItem.Id, ConversionStatus.Failed, It.IsAny<string>(), null, 
            It.IsAny<DateTime?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var itemFailed = false;
        _processor.ItemFailed += (_, item) => { if (item.Id == queueItem.Id) itemFailed = true; };

        // Act
        cts.Cancel();
        await _processor.ProcessItemAsync(queueItem);

        // Assert
        itemFailed.Should().BeTrue();
        queueItem.Status.Should().Be(ConversionStatus.Failed);
        queueItem.ErrorMessage.Should().Contain("canceled");
        
        _executorMock.Verify(e => e.ExecuteAsync(args, It.IsAny<IProgress<double>>(), cts.Token), Times.Once);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
        _processor?.Dispose();
    }
}