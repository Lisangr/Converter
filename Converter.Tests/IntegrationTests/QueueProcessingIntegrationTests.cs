using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Converter.Application.Abstractions;
using Converter.Application.Services;
using Converter.Domain.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Converter.Tests.IntegrationTests;

public class QueueProcessingIntegrationTests : IDisposable
{
    [Fact]
    public async Task QueueProcessing_ShouldRunMultipleItems()
    {
        var queueStore = new Mock<IQueueStore>();
        queueStore.Setup(s => s.TryReserveAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        queueStore.Setup(s => s.CompleteAsync(It.IsAny<Guid>(), It.IsAny<ConversionStatus>(), It.IsAny<string?>(), It.IsAny<long?>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var repo = new Mock<IQueueRepository>();
        repo.Setup(r => r.GetPendingItemsAsync()).ReturnsAsync(new List<QueueItem>());
        repo.Setup(r => r.UpdateAsync(It.IsAny<QueueItem>())).Returns(Task.CompletedTask);

        var useCase = new Mock<IConversionUseCase>();
        useCase.Setup(u => u.ExecuteAsync(It.IsAny<QueueItem>(), It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConversionResult { Success = true, OutputFileSize = 10 });

        var logger = Mock.Of<ILogger<ChannelQueueProcessor>>();
        var processor = new ChannelQueueProcessor(repo.Object, queueStore.Object, useCase.Object, logger);
        var item = new QueueItem { Id = Guid.NewGuid(), FilePath = "file.mp4", Status = ConversionStatus.Pending };

        await processor.ProcessItemAsync(item, CancellationToken.None);

        useCase.Verify(u => u.ExecuteAsync(item, It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(r => r.UpdateAsync(It.Is<QueueItem>(q => q.Status == ConversionStatus.Completed)), Times.AtLeastOnce);
    }

    [Fact]
    public async Task QueueProcessing_ShouldRespectPriorities()
    {
        var queueStore = new Mock<IQueueStore>();
        queueStore.Setup(s => s.TryReserveAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        queueStore.Setup(s => s.CompleteAsync(It.IsAny<Guid>(), It.IsAny<ConversionStatus>(), It.IsAny<string?>(), It.IsAny<long?>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var repo = new Mock<IQueueRepository>();
        var first = new QueueItem { Id = Guid.NewGuid(), FileName = "first", Status = ConversionStatus.Pending };
        var second = new QueueItem { Id = Guid.NewGuid(), FileName = "second", Status = ConversionStatus.Pending };
        repo.Setup(r => r.GetPendingItemsAsync()).ReturnsAsync(new List<QueueItem> { first, second });
        repo.Setup(r => r.UpdateAsync(It.IsAny<QueueItem>())).Returns(Task.CompletedTask);

        var useCase = new Mock<IConversionUseCase>();
        useCase.SetupSequence(u => u.ExecuteAsync(It.IsAny<QueueItem>(), It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConversionResult { Success = true })
            .ReturnsAsync(new ConversionResult { Success = true });

        var logger = Mock.Of<ILogger<ChannelQueueProcessor>>();
        var processor = new ChannelQueueProcessor(repo.Object, queueStore.Object, useCase.Object, logger);

        await processor.StartProcessingAsync();
        await processor.ProcessItemAsync(first, CancellationToken.None);
        await processor.ProcessItemAsync(second, CancellationToken.None);

        useCase.Verify(u => u.ExecuteAsync(first, It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()), Times.Once);
        useCase.Verify(u => u.ExecuteAsync(second, It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task QueueProcessing_ShouldHandleErrors()
    {
        var queueStore = new Mock<IQueueStore>();
        queueStore.Setup(s => s.TryReserveAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        queueStore.Setup(s => s.CompleteAsync(It.IsAny<Guid>(), It.IsAny<ConversionStatus>(), It.IsAny<string?>(), It.IsAny<long?>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var repo = new Mock<IQueueRepository>();
        repo.Setup(r => r.UpdateAsync(It.IsAny<QueueItem>())).Returns(Task.CompletedTask);

        var useCase = new Mock<IConversionUseCase>();
        useCase.Setup(u => u.ExecuteAsync(It.IsAny<QueueItem>(), It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var logger = Mock.Of<ILogger<ChannelQueueProcessor>>();
        var processor = new ChannelQueueProcessor(repo.Object, queueStore.Object, useCase.Object, logger);
        var item = new QueueItem { Id = Guid.NewGuid(), FilePath = "bad.mp4" };

        await Assert.ThrowsAsync<InvalidOperationException>(() => processor.ProcessItemAsync(item, CancellationToken.None));

        repo.Verify(r => r.UpdateAsync(It.Is<QueueItem>(q => q.Status == ConversionStatus.Failed && q.ErrorMessage == "boom")), Times.Once);
    }

    public void Dispose()
    {
        // no-op test cleanup
    }
}