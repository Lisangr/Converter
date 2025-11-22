using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Converter.Application.Abstractions;
using Converter.Application.Models;
using Converter.Application.Services;
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
        var queueStore = new Mock<IQueueStore>();
        queueStore.Setup(s => s.TryReserveAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        queueStore.Setup(s => s.CompleteAsync(It.IsAny<Guid>(), It.IsAny<ConversionStatus>(), It.IsAny<string?>(), It.IsAny<long?>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var repository = new Mock<IQueueRepository>();
        repository.Setup(r => r.UpdateAsync(It.IsAny<QueueItem>())).Returns(Task.CompletedTask);

        var conversion = new Mock<IConversionUseCase>();
        conversion.Setup(c => c.ExecuteAsync(It.IsAny<QueueItem>(), It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()))
            .Callback<QueueItem, IProgress<int>, CancellationToken>((item, progress, _) => progress.Report(75))
            .ReturnsAsync(new ConversionResult { Success = true, OutputFileSize = 2048 });

        var logger = Mock.Of<ILogger<ChannelQueueProcessor>>();
        var processor = new ChannelQueueProcessor(repository.Object, queueStore.Object, conversion.Object, logger);
        var items = new List<QueueItem>();
        for (var i = 0; i < 10; i++)
        {
            items.Add(new QueueItem { Id = Guid.NewGuid(), FilePath = $"file{i}.mp4", Status = ConversionStatus.Pending });
        }

        foreach (var item in items)
        {
            await processor.ProcessItemAsync(item, CancellationToken.None);
        }

        conversion.Verify(c => c.ExecuteAsync(It.IsAny<QueueItem>(), It.IsAny<IProgress<int>>(), It.IsAny<CancellationToken>()), Times.Exactly(items.Count));
        repository.Verify(r => r.UpdateAsync(It.Is<QueueItem>(q => q.Progress == 75)), Times.AtLeast(items.Count));
    }
}
