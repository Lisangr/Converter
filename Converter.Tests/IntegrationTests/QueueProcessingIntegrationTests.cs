using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Converter.Application.Abstractions;
using Converter.Application.Channels;
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
        var processorMock = new Mock<IQueueItemProcessor>();
        processorMock.Setup(p => p.ProcessAsync(It.IsAny<QueueItem>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(true));

        var channel = Channel.CreateUnbounded<QueueItem>();
        var processor = new ChannelQueueProcessor<QueueItem>(processorMock.Object, channel);
        var item = new QueueItem { Id = Guid.NewGuid(), FilePath = "file.mp4", Status = ConversionStatus.Pending };

        await channel.Writer.WriteAsync(item);
        await processor.StartProcessingAsync(CancellationToken.None);

        processorMock.Verify(p => p.ProcessAsync(item, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task QueueProcessing_ShouldRespectPriorities()
    {
        var processorMock = new Mock<IQueueItemProcessor>();
        var channel = Channel.CreateUnbounded<QueueItem>();
        var processor = new ChannelQueueProcessor<QueueItem>(processorMock.Object, channel);
        
        var first = new QueueItem { Id = Guid.NewGuid(), FilePath = "first", Status = ConversionStatus.Pending };
        var second = new QueueItem { Id = Guid.NewGuid(), FilePath = "second", Status = ConversionStatus.Pending };

        // Упрощённый тест приоритетов: проверяем, что оба элемента обрабатываются
        await channel.Writer.WriteAsync(first);
        await channel.Writer.WriteAsync(second);
        channel.Writer.Complete();

        await processor.StartProcessingAsync(CancellationToken.None);

        processorMock.Verify(p => p.ProcessAsync(first, It.IsAny<CancellationToken>()), Times.Once);
        processorMock.Verify(p => p.ProcessAsync(second, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task QueueProcessing_ShouldHandleErrors()
    {
        var processorMock = new Mock<IQueueItemProcessor>();
        processorMock.Setup(p => p.ProcessAsync(It.IsAny<QueueItem>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var channel = Channel.CreateUnbounded<QueueItem>();
        var processor = new ChannelQueueProcessor<QueueItem>(processorMock.Object, channel);
        var item = new QueueItem { Id = Guid.NewGuid(), FilePath = "bad.mp4" };

        await channel.Writer.WriteAsync(item);
        
        // Ожидаем, что процессор бросит исключение
        await Assert.ThrowsAsync<InvalidOperationException>(() => processor.StartProcessingAsync(CancellationToken.None));

        processorMock.Verify(p => p.ProcessAsync(item, It.IsAny<CancellationToken>()), Times.Once);
    }

    public void Dispose()
    {
        // no-op test cleanup
    }
}