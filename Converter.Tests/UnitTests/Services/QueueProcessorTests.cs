using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Converter.Application.Abstractions;
using Converter.Application.Services;
using Converter.Domain.Models;
using Converter.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Converter.Tests.UnitTests.Services
{
    public class QueueProcessorTests
    {
        private readonly Mock<IQueueRepository> _mockQueueRepository;
        private readonly Mock<IQueueStore> _mockQueueStore;
        private readonly Mock<IConversionUseCase> _mockConversionUseCase;
        private readonly Mock<ILogger<QueueProcessor>> _mockLogger;
        private readonly QueueProcessor _queueProcessor;
        private readonly List<QueueItem> _testItems;
        private readonly List<EventLogEntry> _eventLog;

        private class EventLogEntry
        {
            public string EventType { get; set; } = string.Empty;
            public QueueItem? Item { get; set; }
        }

        public QueueProcessorTests()
        {
            _mockQueueRepository = new Mock<IQueueRepository>();
            _mockQueueStore = new Mock<IQueueStore>();
            _mockConversionUseCase = new Mock<IConversionUseCase>();
            _mockLogger = new Mock<ILogger<QueueProcessor>>();
            
            _queueProcessor = new QueueProcessor(
                _mockQueueRepository.Object,
                _mockQueueStore.Object,
                _mockConversionUseCase.Object,
                _mockLogger.Object);

            _testItems = new List<QueueItem>
            {
                new QueueItem { Id = Guid.NewGuid(), FilePath = "test1.mp4", Status = ConversionStatus.Pending },
                new QueueItem { Id = Guid.NewGuid(), FilePath = "test2.mp4", Status = ConversionStatus.Pending },
                new QueueItem { Id = Guid.NewGuid(), FilePath = "test3.mp4", Status = ConversionStatus.Pending }
            };

            _eventLog = new List<EventLogEntry>();
            SubscribeToEvents();
        }

        private void SubscribeToEvents()
        {
            _queueProcessor.ItemStarted += (sender, item) => _eventLog.Add(new EventLogEntry { EventType = "ItemStarted", Item = item });
            _queueProcessor.ItemCompleted += (sender, item) => _eventLog.Add(new EventLogEntry { EventType = "ItemCompleted", Item = item });
            _queueProcessor.ItemFailed += (sender, item) => _eventLog.Add(new EventLogEntry { EventType = "ItemFailed", Item = item });
            _queueProcessor.QueueCompleted += (sender, args) => _eventLog.Add(new EventLogEntry { EventType = "QueueCompleted" });
        }

        [Fact]
        public async Task StartProcessingAsync_WhenNotRunning_ShouldStartSuccessfully()
        {
            // Arrange
            _mockQueueRepository.Setup(x => x.GetPendingItemsAsync())
                .ReturnsAsync(new List<QueueItem>());

            // Act
            await _queueProcessor.StartProcessingAsync();

            // Assert
            _queueProcessor.IsRunning.Should().BeTrue();
            _queueProcessor.IsPaused.Should().BeFalse();
        }

        [Fact]
        public async Task StartProcessingAsync_WhenAlreadyRunning_ShouldNotStartAgain()
        {
            // Arrange
            _mockQueueRepository.Setup(x => x.GetPendingItemsAsync())
                .ReturnsAsync(new List<QueueItem>());
            
            await _queueProcessor.StartProcessingAsync();

            // Act
            await _queueProcessor.StartProcessingAsync();

            // Assert
            // Should only start once
            _queueProcessor.IsRunning.Should().BeTrue();
        }

        [Fact]
        public async Task StopProcessingAsync_WhenRunning_ShouldStopSuccessfully()
        {
            // Arrange
            _mockQueueRepository.Setup(x => x.GetPendingItemsAsync())
                .ReturnsAsync(new List<QueueItem>());
            
            await _queueProcessor.StartProcessingAsync();

            // Act
            await _queueProcessor.StopProcessingAsync();

            // Assert
            _queueProcessor.IsRunning.Should().BeFalse();
        }

        [Fact]
        public async Task ProcessItemAsync_WithValidItem_ShouldProcessSuccessfully()
        {
            // Arrange
            var item = _testItems[0];
            var conversionResult = new ConversionResult 
            { 
                Success = true, 
                OutputFileSize = 1024 * 1024, 
                OutputPath = "output.mp4" 
            };

            _mockQueueStore.Setup(x => x.TryReserveAsync(item.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Эмулируем реальные вызовы прогресса внутри use-case
            _mockConversionUseCase
                .Setup(x => x.ExecuteAsync(item, It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .Returns<QueueItem, IProgress<int>, CancellationToken>((queueItem, progress, ct) =>
                {
                    progress.Report(10);
                    progress.Report(50);
                    progress.Report(100);
                    return Task.FromResult(conversionResult);
                });

            // Act
            await _queueProcessor.ProcessItemAsync(item);

            // Assert
            item.Status.Should().Be(ConversionStatus.Completed);
            item.OutputFileSizeBytes.Should().Be(conversionResult.OutputFileSize);
            
            _mockQueueRepository.Verify(x => x.UpdateAsync(It.IsAny<QueueItem>()), Times.AtLeastOnce);
            _mockQueueStore.Verify(x => x.TryReserveAsync(item.Id, It.IsAny<CancellationToken>()), Times.Once);
            _mockQueueStore.Verify(x => x.CompleteAsync(item.Id, ConversionStatus.Completed, null, conversionResult.OutputFileSize, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ProcessItemAsync_WithFailedConversion_ShouldMarkAsFailed()
        {
            // Arrange
            var item = _testItems[0];
            var conversionResult = new ConversionResult 
            { 
                Success = false, 
                ErrorMessage = "Conversion failed" 
            };

            _mockQueueStore.Setup(x => x.TryReserveAsync(item.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Эмулируем, что use-case действительно репортит прогресс
            _mockConversionUseCase
                .Setup(x => x.ExecuteAsync(item, It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .Returns<QueueItem, IProgress<int>, CancellationToken>((queueItem, progress, ct) =>
                {
                    progress.Report(10);
                    progress.Report(50);
                    progress.Report(100);
                    return Task.FromResult(conversionResult);
                });

            // Act
            await _queueProcessor.ProcessItemAsync(item);

            // Assert
            item.Status.Should().Be(ConversionStatus.Failed);
            item.ErrorMessage.Should().Be("Conversion failed");
            
            _mockQueueStore.Verify(x => x.CompleteAsync(item.Id, ConversionStatus.Failed, conversionResult.ErrorMessage, null, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ProcessItemAsync_WithItemAlreadyReserved_ShouldSkipProcessing()
        {
            // Arrange
            var item = _testItems[0];
            
            _mockQueueStore.Setup(x => x.TryReserveAsync(item.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            await _queueProcessor.ProcessItemAsync(item);

            // Assert
            _mockConversionUseCase.Verify(x => x.ExecuteAsync(It.IsAny<QueueItem>(), It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ProcessItemAsync_WithNullItem_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _queueProcessor.ProcessItemAsync(null!));
        }

        [Fact]
        public async Task PauseProcessingAsync_WhenRunning_ShouldPauseSuccessfully()
        {
            // Arrange
            _mockQueueRepository.Setup(x => x.GetPendingItemsAsync())
                .ReturnsAsync(new List<QueueItem>());
            
            await _queueProcessor.StartProcessingAsync();

            // Act
            await _queueProcessor.PauseProcessingAsync();

            // Assert
            _queueProcessor.IsPaused.Should().BeTrue();
        }

        [Fact]
        public async Task ResumeProcessingAsync_WhenPaused_ShouldResumeSuccessfully()
        {
            // Arrange
            _mockQueueRepository.Setup(x => x.GetPendingItemsAsync())
                .ReturnsAsync(new List<QueueItem>());
            
            await _queueProcessor.StartProcessingAsync();
            await _queueProcessor.PauseProcessingAsync();

            // Act
            await _queueProcessor.ResumeProcessingAsync();

            // Assert
            _queueProcessor.IsPaused.Should().BeFalse();
        }

        [Fact]
        public async Task ProcessItemAsync_ExceptionThrown_ShouldMarkAsFailed()
        {
            // Arrange
            var item = _testItems[0];
            var exception = new InvalidOperationException("Test exception");

            _mockQueueStore.Setup(x => x.TryReserveAsync(item.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mockConversionUseCase.Setup(x => x.ExecuteAsync(item, It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _queueProcessor.ProcessItemAsync(item));

            ex.Message.Should().Be("Test exception");
            
            // Verify the item was marked as failed
            item.Status.Should().Be(ConversionStatus.Failed);
            _mockQueueStore.Verify(x => x.CompleteAsync(item.Id, ConversionStatus.Failed, exception.Message, null, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ProcessItemAsync_WithCancellationToken_CanBeCancelled()
        {
            // Arrange
            var item = _testItems[0];
            var cts = new CancellationTokenSource();
            var exception = new OperationCanceledException();

            _mockQueueStore.Setup(x => x.TryReserveAsync(item.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mockConversionUseCase.Setup(x => x.ExecuteAsync(item, It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                _queueProcessor.ProcessItemAsync(item, cts.Token));
        }

        [Fact]
        public async Task ProcessItemAsync_ShouldRaiseProgressEvents()
        {
            // Arrange
            var item = _testItems[0];
            var conversionResult = new ConversionResult { Success = true, OutputFileSize = 1024 };
            var progressEvents = new List<(int progress, string status)>();

            _mockQueueStore.Setup(x => x.TryReserveAsync(item.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mockConversionUseCase
                .Setup(x => x.ExecuteAsync(item, It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .Returns<QueueItem, IProgress<int>, CancellationToken>((queueItem, progress, ct) =>
                {
                    progress.Report(10);
                    progress.Report(50);
                    progress.Report(100);
                    return Task.FromResult(conversionResult);
                });

            // Subscribe to progress events
            _queueProcessor.ProgressChanged += (sender, args) => 
                progressEvents.Add((args.Progress, args.Status));

            // Act
            await _queueProcessor.ProcessItemAsync(item);

            // Assert
            progressEvents.Should().NotBeEmpty(); // At least one progress update should occur
        }

        [Fact]
        public async Task ProcessItemAsync_ShouldRaiseItemStartedEvent()
        {
            // Arrange
            var item = _testItems[0];
            var conversionResult = new ConversionResult { Success = true, OutputFileSize = 1024 };

            _mockQueueStore.Setup(x => x.TryReserveAsync(item.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mockConversionUseCase.Setup(x => x.ExecuteAsync(item, It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(conversionResult);

            // Act
            await _queueProcessor.ProcessItemAsync(item);

            // Assert
            var startedEvent = _eventLog.Find(e => 
                e is { EventType: "ItemStarted", Item: QueueItem item } && item.Id == item.Id);
            startedEvent.Should().NotBeNull();
        }

        [Fact]
        public async Task ProcessItemAsync_WithSuccess_ShouldRaiseItemCompletedEvent()
        {
            // Arrange
            var item = _testItems[0];
            var conversionResult = new ConversionResult { Success = true, OutputFileSize = 1024 };

            _mockQueueStore.Setup(x => x.TryReserveAsync(item.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mockConversionUseCase.Setup(x => x.ExecuteAsync(item, It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(conversionResult);

            // Act
            await _queueProcessor.ProcessItemAsync(item);

            // Assert
            var completedEvent = _eventLog.Find(e => e is { EventType: "ItemCompleted", Item: QueueItem item } && item.Id == item.Id);
            completedEvent.Should().NotBeNull();
        }

        [Fact]
        public async Task ProcessItemAsync_WithFailure_ShouldRaiseItemFailedEvent()
        {
            // Arrange
            var item = _testItems[0];
            var conversionResult = new ConversionResult { Success = false, ErrorMessage = "Test error" };

            _mockQueueStore.Setup(x => x.TryReserveAsync(item.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mockConversionUseCase.Setup(x => x.ExecuteAsync(item, It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(conversionResult);

            // Act
            await _queueProcessor.ProcessItemAsync(item);

            // Assert
            var failedEvent = _eventLog.Find(e => e is { EventType: "ItemFailed", Item: QueueItem item } && item.Id == item.Id);
            failedEvent.Should().NotBeNull();
        }
    }
}