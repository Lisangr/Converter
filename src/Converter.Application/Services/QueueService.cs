using Converter.Application.Abstractions;
using Converter.Domain.Models;
using Microsoft.Extensions.Logging;

namespace Converter.Application.Services;

public sealed class QueueService : IQueueService
{
    private readonly IConversionOrchestrator _orchestrator;
    private readonly ILogger<QueueService> _logger;
    private readonly Queue<QueueItem> _queue = new();
    private readonly List<QueueItem> _history = new();
    private readonly SemaphoreSlim _gate = new(1, 1);
    private CancellationTokenSource? _internalCts;

    public event EventHandler<IReadOnlyCollection<QueueItem>>? QueueChanged;
    public event EventHandler<(QueueItem Item, QueueItemStatus Status)>? ItemStatusChanged;
    public event EventHandler<ConversionProgress>? ProgressChanged;

    public QueueService(IConversionOrchestrator orchestrator, ILogger<QueueService> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    public async Task EnqueueAsync(IEnumerable<ConversionRequest> requests, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(requests);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            foreach (var request in requests)
            {
                var item = QueueItem.Create(request);
                _queue.Enqueue(item);
                _history.Add(item);
            }
        }
        finally
        {
            _gate.Release();
        }

        QueueChanged?.Invoke(this, _history.AsReadOnly());
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _internalCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        while (true)
        {
            QueueItem? item = null;
            await _gate.WaitAsync(_internalCts.Token).ConfigureAwait(false);
            try
            {
                if (_queue.Count == 0)
                {
                    break;
                }

                item = _queue.Dequeue();
            }
            finally
            {
                _gate.Release();
            }

            QueueChanged?.Invoke(this, _history.AsReadOnly());

            if (item is null)
            {
                break;
            }

            ItemStatusChanged?.Invoke(this, (item, QueueItemStatus.Running));
            var progress = new Progress<ConversionProgress>(p => ProgressChanged?.Invoke(this, p));
            try
            {
                var result = await _orchestrator.ExecuteAsync(item.Request, progress, _internalCts.Token).ConfigureAwait(false);
                if (result is ConversionResult.Success)
                {
                    ItemStatusChanged?.Invoke(this, (item, QueueItemStatus.Completed));
                }
                else
                {
                    ItemStatusChanged?.Invoke(this, (item, QueueItemStatus.Failed));
                }
            }
            catch (OperationCanceledException)
            {
                ItemStatusChanged?.Invoke(this, (item, QueueItemStatus.Cancelled));
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Conversion failed for {Input}", item.Request.InputPath);
                ItemStatusChanged?.Invoke(this, (item, QueueItemStatus.Failed));
            }
        }

        _internalCts.Dispose();
        _internalCts = null;
    }

    public Task CancelAsync()
    {
        _internalCts?.Cancel();
        return Task.CompletedTask;
    }

    public IReadOnlyCollection<QueueItem> Snapshot()
        => _history.AsReadOnly();

    public ValueTask DisposeAsync()
    {
        _internalCts?.Dispose();
        _gate.Dispose();
        return ValueTask.CompletedTask;
    }
}
