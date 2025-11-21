using System;
using System.Collections.Generic;
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

namespace Converter.Tests.UnitTests.Services
{
    public class QueueProcessorTests
    {
        private readonly Mock<IQueueRepository> _queueRepositoryMock;
        private readonly Mock<IQueueStore> _queueStoreMock;
        private readonly Mock<IConversionUseCase> _conversionUseCaseMock;
        private readonly Mock<ILogger<QueueProcessor>> _loggerMock;
        private readonly QueueProcessor _processor;

        public QueueProcessorTests()
        {
            _queueRepositoryMock = new Mock<IQueueRepository>(MockBehavior.Strict);
            _queueStoreMock = new Mock<IQueueStore>(MockBehavior.Strict);
            _conversionUseCaseMock = new Mock<IConversionUseCase>(MockBehavior.Strict);
            _loggerMock = new Mock<ILogger<QueueProcessor>>();

            _processor = new QueueProcessor(
                _queueRepositoryMock.Object,
                _queueStoreMock.Object,
                _conversionUseCaseMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task StartProcessingAsync_ShouldSetRunningAndNotPaused_AndQueryPendingItems()
        {
            // Arrange
            _queueRepositoryMock
                .Setup(r => r.GetPendingItemsAsync())
                .ReturnsAsync(new List<QueueItem>());

            // Act
            await _processor.StartProcessingAsync();

            // Assert
            _processor.IsRunning.Should().BeTrue();
            _processor.IsPaused.Should().BeFalse();
            _queueRepositoryMock.Verify(r => r.GetPendingItemsAsync(), Times.Once);
        }

        [Fact]
        public async Task ProcessItemAsync_WithNullItem_ShouldThrow()
        {
            // Act
            Func<Task> act = () => _processor.ProcessItemAsync(null!);

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task ProcessItemAsync_WhenItemAlreadyReserved_ShouldSkipProcessing()
        {
            // Arrange
            var item = CreateItem();

            _queueStoreMock
                .Setup(s => s.TryReserveAsync(item.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            await _processor.ProcessItemAsync(item);

            // Assert
            _conversionUseCaseMock.Verify(
                u => u.ExecuteAsync(It.IsAny<QueueItem>(), It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task ProcessItemAsync_WithSuccessfulResult_ShouldCompleteItemAndRaiseEvents()
        {
            // Arrange
            var item = CreateItem();
            var result = new ConversionResult
            {
                Success = true,
                OutputFileSize = 1024
            };

            var itemStarted = false;
            var itemCompleted = false;

            _processor.ItemStarted += (_, q) =>
            {
                if (q.Id == item.Id)
                {
                    itemStarted = true;
                }
            };

            _processor.ItemCompleted += (_, q) =>
            {
                if (q.Id == item.Id)
                {
                    itemCompleted = true;
                }
            };

            _queueStoreMock
                .Setup(s => s.TryReserveAsync(item.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _queueRepositoryMock
                .Setup(r => r.UpdateAsync(It.IsAny<QueueItem>()))
                .Returns(Task.CompletedTask);

            _conversionUseCaseMock
                .Setup(u => u.ExecuteAsync(item, It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            _queueStoreMock
                .Setup(s => s.CompleteAsync(
                    item.Id,
                    ConversionStatus.Completed,
                    null,
                    result.OutputFileSize,
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _processor.ProcessItemAsync(item);

            // Assert
            item.Status.Should().Be(ConversionStatus.Completed);
            item.OutputFileSizeBytes.Should().Be(result.OutputFileSize);
            itemStarted.Should().BeTrue();
            itemCompleted.Should().BeTrue();

            _queueStoreMock.VerifyAll();
            _queueRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<QueueItem>()), Times.AtLeastOnce);
            _conversionUseCaseMock.VerifyAll();
        }

        [Fact]
        public async Task ProcessItemAsync_WithFailedResult_ShouldMarkItemFailedAndRaiseEvent()
        {
            // Arrange
            var item = CreateItem();
            var errorMessage = "Conversion failed";

            var result = new ConversionResult
            {
                Success = false,
                ErrorMessage = errorMessage
            };

            var itemFailed = false;

            _processor.ItemFailed += (_, q) =>
            {
                if (q.Id == item.Id)
                {
                    itemFailed = true;
                }
            };

            _queueStoreMock
                .Setup(s => s.TryReserveAsync(item.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _queueRepositoryMock
                .Setup(r => r.UpdateAsync(It.IsAny<QueueItem>()))
                .Returns(Task.CompletedTask);

            _conversionUseCaseMock
                .Setup(u => u.ExecuteAsync(item, It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(result);

            _queueStoreMock
                .Setup(s => s.CompleteAsync(
                    item.Id,
                    ConversionStatus.Failed,
                    errorMessage,
                    null,
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _processor.ProcessItemAsync(item);

            // Assert
            item.Status.Should().Be(ConversionStatus.Failed);
            item.ErrorMessage.Should().Be(errorMessage);
            itemFailed.Should().BeTrue();

            _queueStoreMock.VerifyAll();
            _queueRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<QueueItem>()), Times.AtLeastOnce);
            _conversionUseCaseMock.VerifyAll();
        }

        private static QueueItem CreateItem()
        {
            return new QueueItem
            {
                Id = Guid.NewGuid(),
                FilePath = "test.mp4",
                Status = ConversionStatus.Pending,
                AddedAt = DateTime.UtcNow
            };
        }
    }
}
