using Converter.Domain.Models;

namespace Converter.Application.Abstractions;

public interface IQueueService : IAsyncDisposable
{
    event EventHandler<IReadOnlyCollection<QueueItem>>? QueueChanged;
    event EventHandler<(QueueItem Item, QueueItemStatus Status)>? ItemStatusChanged;
    event EventHandler<ConversionProgress>? ProgressChanged;

    Task EnqueueAsync(IEnumerable<ConversionRequest> requests, CancellationToken cancellationToken);
    Task StartAsync(CancellationToken cancellationToken);
    Task CancelAsync();
    IReadOnlyCollection<QueueItem> Snapshot();
}
