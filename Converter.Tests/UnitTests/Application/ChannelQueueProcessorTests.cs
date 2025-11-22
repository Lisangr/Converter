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

public class ChannelQueueProcessorTests
{
    private readonly Mock<IQueueRepository> _queueRepository = new();
    private readonly Mock<IQueueStore> _queueStore = new();
    private readonly Mock<IConversionUseCase> _conversionUseCase = new();
    private readonly Mock<ILogger<ChannelQueueProcessor>> _logger = new();
    private readonly Mock<IUiDispatcher> _uiDispatcher = new();
    private readonly ChannelQueueProcessor _processor;

    public ChannelQueueProcessorTests()
    {
        _processor = new ChannelQueueProcessor(
            _queueRepository.Object,
            _queueStore.Object,
            _conversionUseCase.Object,
            _logger.Object,
            _uiDispatcher.Object);
    }

    [Fact]
    public async Task ProcessItemAsync_WhenReserved_ShouldUpdateStatusAndRaiseEvents()
    {
        // Arrange
        var item = new QueueItem { Id = Guid.NewGuid(), Status = ConversionStatus.Pending };
        var outcome = new ConversionResult { Success = true, OutputFileSize = 1234 };
        _queueStore.Setup(s => s.TryReserveAsync(item.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _conversionUseCase.Setup(c => c.ExecuteAsync(item, It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(outcome);

        QueueItem? started = null;
        QueueItem? completed = null;
        _processor.ItemStarted += (_, i) => started = i;
        _processor.ItemCompleted += (_, i) => completed = i;

        // Act
        await _processor.ProcessItemAsync(item, CancellationToken.None);

        // Assert
        started.Should().BeSameAs(item);
        completed.Should().BeSameAs(item);
        item.Status.Should().Be(ConversionStatus.Completed);
        item.OutputFileSizeBytes.Should().Be(outcome.OutputFileSize);

        _queueStore.Verify(s => s.CompleteAsync(item.Id, ConversionStatus.Completed, null, outcome.OutputFileSize,
            It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Once);
        _conversionUseCase.Verify(c => c.ExecuteAsync(item, It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _queueRepository.Verify(r => r.UpdateAsync(It.Is<QueueItem>(q => q.Id == item.Id && q.Status == ConversionStatus.Completed)),
            Times.Once);
    }

    [Fact]
    public async Task ProcessItemAsync_WhenConversionFails_ShouldMarkFailed()
    {
        // Arrange
        var item = new QueueItem { Id = Guid.NewGuid(), Status = ConversionStatus.Pending };
        var outcome = new ConversionResult { Success = false, ErrorMessage = "boom" };
        _queueStore.Setup(s => s.TryReserveAsync(item.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _conversionUseCase.Setup(c => c.ExecuteAsync(item, It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(outcome);

        QueueItem? failed = null;
        _processor.ItemFailed += (_, i) => failed = i;

        // Act
        await _processor.ProcessItemAsync(item, CancellationToken.None);

        // Assert
        failed.Should().BeSameAs(item);
        item.Status.Should().Be(ConversionStatus.Failed);
        item.ErrorMessage.Should().Be(outcome.ErrorMessage);

        _queueStore.Verify(s => s.CompleteAsync(item.Id, ConversionStatus.Failed, outcome.ErrorMessage,
            null, It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Once);
        _queueRepository.Verify(r => r.UpdateAsync(It.Is<QueueItem>(q => q.Id == item.Id && q.Status == ConversionStatus.Failed)),
            Times.Once);
    }

    [Fact]
    public async Task ProcessItemAsync_WhenReservationRejected_ShouldSkipProcessing()
    {
        // Arrange
        var item = new QueueItem { Id = Guid.NewGuid(), Status = ConversionStatus.Pending };
        _queueStore.Setup(s => s.TryReserveAsync(item.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        await _processor.ProcessItemAsync(item, CancellationToken.None);

        // Assert
        _conversionUseCase.Verify(c => c.ExecuteAsync(It.IsAny<QueueItem>(), It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _queueRepository.Verify(r => r.UpdateAsync(It.IsAny<QueueItem>()), Times.Never);
    }
}
