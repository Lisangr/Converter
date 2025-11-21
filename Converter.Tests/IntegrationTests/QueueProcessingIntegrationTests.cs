using System;
using System.Collections.Generic;
using System.Linq;
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

namespace Converter.Tests.IntegrationTests;

public class QueueProcessingIntegrationTests : IDisposable
{
    private readonly Mock<IQueueRepository> _queueRepositoryMock;
    private readonly Mock<IQueueStore> _queueStoreMock;
    private readonly Mock<IConversionUseCase> _conversionUseCaseMock;
    private readonly Mock<ILogger<QueueProcessor>> _loggerMock;
    private readonly QueueProcessor _queueProcessor;
    private readonly ChannelQueueProcessor _channelProcessor;
    private readonly string _testDirectory;

    public QueueProcessingIntegrationTests()
    {
        _queueRepositoryMock = new Mock<IQueueRepository>();
        _queueStoreMock = new Mock<IQueueStore>();
        _conversionUseCaseMock = new Mock<IConversionUseCase>();
        _loggerMock = new Mock<ILogger<QueueProcessor>>();

        _queueProcessor = new QueueProcessor(
            _queueRepositoryMock.Object,
            _queueStoreMock.Object,
            _conversionUseCaseMock.Object,
            _loggerMock.Object);

        _channelProcessor = new ChannelQueueProcessor(
            _queueRepositoryMock.Object,
            _queueStoreMock.Object,
            _conversionUseCaseMock.Object,
            _loggerMock.Object);

        _testDirectory = Path.Combine(Path.GetTempPath(), "ConverterQueueTests_" + Guid.NewGuid());
    }

    [Fact]
    public async Task QueueProcessing_ShouldRunMultipleItems()
    {
        // Arrange
        var items = new List<QueueItem>
        {
            new() { Id = Guid.NewGuid(), FilePath = "video1.mp4", Status = ConversionStatus.Pending, Priority = 1 },
            new() { Id = Guid.NewGuid(), FilePath = "video2.mp4", Status = ConversionStatus.Pending, Priority = 2 },
            new() { Id = Guid.NewGuid(), FilePath = "video3.mp4", Status = ConversionStatus.Pending, Priority = 3 }
        };

        var results = new List<ConversionResult>
        {
            new() { Success = true, OutputFileSize = 1024 },
            new() { Success = true, OutputFileSize = 2048 },
            new() { Success = true, OutputFileSize = 3072 }
        };

        var processedItems = new List<QueueItem>();
        var startedEvents = new List<QueueItem>();
        var completedEvents = new List<QueueItem>();

        _queueProcessor.ItemStarted += (_, item) => startedEvents.Add(item);
        _queueProcessor.ItemCompleted += (_, item) => completedEvents.Add(item);

        // Настройка моков
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var result = results[i];

            _queueStoreMock.Setup(s => s.TryReserveAsync(item.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _conversionUseCaseMock.Setup(c => c.ExecuteAsync(item, It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            _queueRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<QueueItem>()))
                .Returns(Task.CompletedTask);

            _queueStoreMock.Setup(s => s.CompleteAsync(
                item.Id,
                ConversionStatus.Completed,
                null,
                result.OutputFileSize,
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        // Act
        var processingTasks = items.Select(item => _queueProcessor.ProcessItemAsync(item));
        await Task.WhenAll(processingTasks);

        // Assert
        startedEvents.Should().HaveCount(3);
        completedEvents.Should().HaveCount(3);
        
        foreach (var item in items)
        {
            item.Status.Should().Be(ConversionStatus.Completed);
            item.OutputFileSizeBytes.Should().BeGreaterThan(0);
        }

        _conversionUseCaseMock.Verify(c => c.ExecuteAsync(It.IsAny<QueueItem>(), It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        _queueStoreMock.Verify(s => s.CompleteAsync(It.IsAny<Guid>(), ConversionStatus.Completed, null, It.IsAny<long?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task QueueProcessing_ShouldRespectPriorities()
    {
        // Arrange
        var highPriorityItem = new QueueItem 
        { 
            Id = Guid.NewGuid(), 
            FilePath = "high_priority.mp4", 
            Status = ConversionStatus.Pending, 
            Priority = 10 
        };
        
        var mediumPriorityItem = new QueueItem 
        { 
            Id = Guid.NewGuid(), 
            FilePath = "medium_priority.mp4", 
            Status = ConversionStatus.Pending, 
            Priority = 5 
        };
        
        var lowPriorityItem = new QueueItem 
        { 
            Id = Guid.NewGuid(), 
            FilePath = "low_priority.mp4", 
            Status = ConversionStatus.Pending, 
            Priority = 1 
        };

        var items = new[] { lowPriorityItem, highPriorityItem, mediumPriorityItem };
        var processingOrder = new List<QueueItem>();

        // Настройка обработки в порядке приоритета
        foreach (var item in items.OrderByDescending(i => i.Priority))
        {
            _queueStoreMock.Setup(s => s.TryReserveAsync(item.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _conversionUseCaseMock.Setup(c => c.ExecuteAsync(item, It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ConversionResult { Success = true })
                .Callback(() => processingOrder.Add(item));

            _queueRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<QueueItem>()))
                .Returns(Task.CompletedTask);

            _queueStoreMock.Setup(s => s.CompleteAsync(
                item.Id, ConversionStatus.Completed, null, null, It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        // Act
        var tasks = items.Select(item => _queueProcessor.ProcessItemAsync(item));
        await Task.WhenAll(tasks);

        // Assert
        processingOrder.Should().HaveCount(3);
        processingOrder[0].Should().Be(highPriorityItem);
        processingOrder[1].Should().Be(mediumPriorityItem);
        processingOrder[2].Should().Be(lowPriorityItem);
    }

    [Fact]
    public async Task QueueProcessing_ShouldHandleErrors()
    {
        // Arrange
        var successItem = new QueueItem 
        { 
            Id = Guid.NewGuid(), 
            FilePath = "success.mp4", 
            Status = ConversionStatus.Pending 
        };
        
        var failItem = new QueueItem 
        { 
            Id = Guid.NewGuid(), 
            FilePath = "fail.mp4", 
            Status = ConversionStatus.Pending 
        };

        var items = new[] { successItem, failItem };
        var failedEvents = new List<QueueItem>();
        var completedEvents = new List<QueueItem>();

        _queueProcessor.ItemFailed += (_, item) => failedEvents.Add(item);
        _queueProcessor.ItemCompleted += (_, item) => completedEvents.Add(item);

        // Настройка успешного результата
        _queueStoreMock.Setup(s => s.TryReserveAsync(successItem.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _conversionUseCaseMock.Setup(c => c.ExecuteAsync(successItem, It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConversionResult { Success = true });
        _queueRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<QueueItem>())).Returns(Task.CompletedTask);
        _queueStoreMock.Setup(s => s.CompleteAsync(successItem.Id, ConversionStatus.Completed, null, null, It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Настройка неудачного результата
        _queueStoreMock.Setup(s => s.TryReserveAsync(failItem.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _conversionUseCaseMock.Setup(c => c.ExecuteAsync(failItem, It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConversionResult 
            { 
                Success = false, 
                ErrorMessage = "FFmpeg conversion failed" 
            });
        _queueRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<QueueItem>())).Returns(Task.CompletedTask);
        _queueStoreMock.Setup(s => s.CompleteAsync(
            failItem.Id, 
            ConversionStatus.Failed, 
            "FFmpeg conversion failed", 
            null, 
            It.IsAny<DateTime?>(), 
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var tasks = items.Select(item => _queueProcessor.ProcessItemAsync(item));
        await Task.WhenAll(tasks);

        // Assert
        completedEvents.Should().HaveCount(1);
        completedEvents.Should().Contain(successItem);
        failedEvents.Should().HaveCount(1);
        failedEvents.Should().Contain(failItem);
        
        successItem.Status.Should().Be(ConversionStatus.Completed);
        failItem.Status.Should().Be(ConversionStatus.Failed);
        failItem.ErrorMessage.Should().Be("FFmpeg conversion failed");
    }

    [Fact]
    public async Task QueueProcessing_ShouldHandleChannelQueueProcessor()
    {
        // Arrange
        var items = new List<QueueItem>
        {
            new() { Id = Guid.NewGuid(), FilePath = "channel1.mp4", Status = ConversionStatus.Pending },
            new() { Id = Guid.NewGuid(), FilePath = "channel2.mp4", Status = ConversionStatus.Pending }
        };

        var startedEvents = new List<QueueItem>();
        var completedEvents = new List<QueueItem>();

        _channelProcessor.ItemStarted += (_, item) => startedEvents.Add(item);
        _channelProcessor.ItemCompleted += (_, item) => completedEvents.Add(item);

        var results = new[]
        {
            new ConversionResult { Success = true, OutputFileSize = 1024 },
            new ConversionResult { Success = true, OutputFileSize = 2048 }
        };

        // Настройка моков для каждого элемента
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var result = results[i];

            _queueStoreMock.Setup(s => s.TryReserveAsync(item.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _conversionUseCaseMock.Setup(c => c.ExecuteAsync(item, It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            _queueRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<QueueItem>()))
                .Returns(Task.CompletedTask);

            _queueStoreMock.Setup(s => s.CompleteAsync(
                item.Id,
                ConversionStatus.Completed,
                null,
                result.OutputFileSize,
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        // Act
        var tasks = items.Select(item => _channelProcessor.ProcessItemAsync(item, CancellationToken.None));
        await Task.WhenAll(tasks);

        // Assert
        startedEvents.Should().HaveCount(2);
        completedEvents.Should().HaveCount(2);
        
        foreach (var item in items)
        {
            item.Status.Should().Be(ConversionStatus.Completed);
        }
    }

    [Fact]
    public async Task QueueProcessing_ShouldHandleCancellation()
    {
        // Arrange
        var item = new QueueItem 
        { 
            Id = Guid.NewGuid(), 
            FilePath = "cancel_test.mp4", 
            Status = ConversionStatus.Pending 
        };

        using var cts = new CancellationTokenSource();
        
        _queueStoreMock.Setup(s => s.TryReserveAsync(item.Id, cts.Token))
            .ReturnsAsync(true);

        _conversionUseCaseMock.Setup(c => c.ExecuteAsync(item, It.IsAny<IProgress<int>>(), cts.Token))
            .ThrowsAsync(new OperationCanceledException());

        _queueRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<QueueItem>())).Returns(Task.CompletedTask);
        _queueStoreMock.Setup(s => s.CompleteAsync(
            item.Id, 
            ConversionStatus.Failed, 
            It.IsAny<string>(), 
            null, 
            It.IsAny<DateTime?>(), 
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var failedEvents = new List<QueueItem>();
        _queueProcessor.ItemFailed += (_, failedItem) => failedEvents.Add(failedItem);

        // Act
        cts.Cancel();
        await _queueProcessor.ProcessItemAsync(item);

        // Assert
        failedEvents.Should().HaveCount(1);
        failedEvents.Should().Contain(item);
        item.Status.Should().Be(ConversionStatus.Failed);
    }

    public void Dispose()
    {
        _queueProcessor?.Dispose();
        _channelProcessor?.Dispose();
    }
}