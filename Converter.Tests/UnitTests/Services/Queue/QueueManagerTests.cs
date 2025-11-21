using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Converter.Application.Abstractions;
using Converter.Application.Services.Queue;
using Converter.Domain.Models;
using FluentAssertions;
using Moq;
using Xunit;

namespace Converter.Tests.UnitTests.Services.Queue;

public class QueueManagerTests
{
    [Fact]
    public async Task AddItem_ShouldRespectPriorities()
    {
        // Arrange
        var processor = new Mock<IQueueItemProcessor>();
        processor.SetupSequence(p => p.ProcessAsync(It.IsAny<QueueItem>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(true);

        var manager = new QueueManager(processor.Object, maxConcurrent: 1);
        var high = new QueueItem { Priority = 10 };
        var low = new QueueItem { Priority = 1 };

        // Act
        var lowTask = manager.EnqueueAsync(low);
        var highTask = manager.EnqueueAsync(high);
        await Task.WhenAll(lowTask, highTask);

        // Assert
        processor.Verify(p => p.ProcessAsync(high, It.IsAny<CancellationToken>()), Times.Once);
        processor.Verify(p => p.ProcessAsync(low, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessQueue_ShouldHandleParallelExecution()
    {
        // Arrange
        var processed = new List<Guid>();
        var processor = new Mock<IQueueItemProcessor>();
        processor.Setup(p => p.ProcessAsync(It.IsAny<QueueItem>(), It.IsAny<CancellationToken>()))
            .Returns<QueueItem, CancellationToken>(async (item, _) =>
            {
                processed.Add(item.Id);
                await Task.Delay(50);
                return true;
            });

        var manager = new QueueManager(processor.Object, maxConcurrent: 2);
        var item1 = new QueueItem { Id = Guid.NewGuid() };
        var item2 = new QueueItem { Id = Guid.NewGuid() };

        // Act
        await Task.WhenAll(manager.EnqueueAsync(item1), manager.EnqueueAsync(item2));

        // Assert
        processed.Should().Contain(new[] { item1.Id, item2.Id });
    }

    [Fact]
    public async Task RemoveItem_ShouldUpdateScheduling()
    {
        // Arrange
        var processor = new Mock<IQueueItemProcessor>();
        processor.Setup(p => p.ProcessAsync(It.IsAny<QueueItem>(), It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                await Task.Delay(100);
                return true;
            });

        var manager = new QueueManager(processor.Object, maxConcurrent: 1);
        var item = new QueueItem { Id = Guid.NewGuid() };

        // Act
        var enqueueTask = manager.EnqueueAsync(item);
        await manager.StopAsync();

        // Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() => enqueueTask);
    }
}
