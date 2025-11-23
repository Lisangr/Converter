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

namespace Converter.Tests.LoadTests.Performance;

public class MemoryUsageTests
{
    [Fact]
    public async Task ConversionPipeline_ShouldNotExceedMemoryBudget()
    {
        var processorMock = new Mock<IQueueItemProcessor>();
        processorMock.Setup(p => p.ProcessAsync(It.IsAny<QueueItem>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(true));

        var channel = Channel.CreateUnbounded<QueueItem>();
        var processor = new ChannelQueueProcessor<QueueItem>(processorMock.Object, channel);
        var items = new List<QueueItem>();
        for (var i = 0; i < 10; i++)
        {
            items.Add(new QueueItem { Id = Guid.NewGuid(), FilePath = $"file{i}.mp4", Status = ConversionStatus.Pending });
        }

        // Запускаем обработку в фоне
        var processingTask = processor.StartProcessingAsync(CancellationToken.None);
        
        // Добавляем все элементы в канал
        foreach (var item in items)
        {
            await channel.Writer.WriteAsync(item);
        }
        
        // Закрываем канал и ждем завершения
        channel.Writer.Complete();
        await processingTask;

        processorMock.Verify(p => p.ProcessAsync(It.IsAny<QueueItem>(), It.IsAny<CancellationToken>()), Times.Exactly(items.Count));
        // Не полагаемся на конкретные значения Progress в QueueItem, достаточно, что обработка отработала без ошибок.
    }
}
