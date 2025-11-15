using Converter.Domain.Models;

namespace Converter.Application.Interfaces;

public interface IQueueService : IAsyncDisposable
{
    event EventHandler<QueueItem>? ItemQueued;
    event EventHandler<(QueueItem Item, ConversionProgress Progress)>? ProgressChanged;
    event EventHandler<(QueueItem Item, ConversionResult Result)>? ItemCompleted;

    Task EnqueueAsync(ConversionRequest request, CancellationToken cancellationToken);
    Task CancelAsync(Guid queueItemId);
    Task<IReadOnlyList<QueueItem>> SnapshotAsync();
}
