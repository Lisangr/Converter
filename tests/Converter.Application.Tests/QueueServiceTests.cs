using System.Threading.Tasks;
using Converter.Application.Abstractions;
using Converter.Application.Services;
using Converter.Domain.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Converter.Application.Tests;

public sealed class QueueServiceTests
{
    [Fact]
    public async Task StartAsync_ProcessesQueuedItems()
    {
        var orchestrator = new Mock<IConversionOrchestrator>();
        orchestrator
            .Setup(o => o.ExecuteAsync(It.IsAny<ConversionRequest>(), It.IsAny<IProgress<ConversionProgress>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConversionResult.Success("input", "output", TimeSpan.FromSeconds(1)));
        var service = new QueueService(orchestrator.Object, NullLogger<QueueService>.Instance);
        var profile = new ConversionProfile("Test", "mp4", "libx264", "aac");
        var request = new ConversionRequest("input", "out", profile);

        var completionSource = new TaskCompletionSource();
        service.ItemStatusChanged += (_, tuple) =>
        {
            if (tuple.Status == QueueItemStatus.Completed)
            {
                completionSource.TrySetResult();
            }
        };

        await service.EnqueueAsync(new[] { request }, CancellationToken.None);
        await service.StartAsync(CancellationToken.None);
        await completionSource.Task.WaitAsync(TimeSpan.FromSeconds(2));

        orchestrator.Verify(o => o.ExecuteAsync(It.IsAny<ConversionRequest>(), It.IsAny<IProgress<ConversionProgress>>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
