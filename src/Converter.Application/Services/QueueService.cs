using System.Collections.Concurrent;
using System.Threading.Channels;
using Converter.Application.Interfaces;
using Converter.Domain.Models;
using Microsoft.Extensions.Logging;

namespace Converter.Application.Services;

public sealed class QueueService : IQueueService
{
    private readonly IConversionOrchestrator _orchestrator;
    private readonly ILogger<QueueService> _logger;
    private readonly Channel<QueueItem> _channel;
    private readonly CancellationTokenSource _shutdown = new();
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _itemCancellation = new();
    private readonly List<QueueItem> _items = new();
    private readonly SemaphoreSlim _itemsGate = new(1, 1);
    private readonly Task _worker;

    public event EventHandler<QueueItem>? ItemQueued;
    public event EventHandler<(QueueItem Item, ConversionProgress Progress)>? ProgressChanged;
    public event EventHandler<(QueueItem Item, ConversionResult Result)>? ItemCompleted;

    public QueueService(IConversionOrchestrator orchestrator, ILogger<QueueService> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
        _channel = Channel.CreateUnbounded<QueueItem>(new UnboundedChannelOptions
        {
            SingleReader = true,
            AllowSynchronousContinuations = false
        });

        _worker = Task.Run(ProcessAsync);
    }

    public async Task EnqueueAsync(ConversionRequest request, CancellationToken cancellationToken)
    {
        var queueItem = new QueueItem(Guid.NewGuid(), request);
        _itemCancellation[queueItem.Id] = CancellationTokenSource.CreateLinkedTokenSource(request.CancellationToken);
        await _itemsGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _items.Add(queueItem);
        }
        finally
        {
            _itemsGate.Release();
        }

        await _channel.Writer.WriteAsync(queueItem, cancellationToken).ConfigureAwait(false);
        ItemQueued?.Invoke(this, queueItem);
    }

    public Task CancelAsync(Guid queueItemId)
    {
        if (_itemCancellation.TryGetValue(queueItemId, out var cts))
        {
            cts.Cancel();
        }

        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<QueueItem>> SnapshotAsync()
    {
        await _itemsGate.WaitAsync().ConfigureAwait(false);
        try
        {
            return _items.ToList();
        }
        finally
        {
            _itemsGate.Release();
        }
    }

    private async Task ProcessAsync()
    {
        await foreach (var item in _channel.Reader.ReadAllAsync(_shutdown.Token))
        {
            if (!_itemCancellation.TryGetValue(item.Id, out var cancellationSource))
            {
                continue;
            }

            using var linked = CancellationTokenSource.CreateLinkedTokenSource(_shutdown.Token, cancellationSource.Token);
            var progress = new Progress<ConversionProgress>(update =>
            {
                ProgressChanged?.Invoke(this, (item, update));
            });

            var result = await _orchestrator.ExecuteAsync(item.Request with { CancellationToken = linked.Token }, progress, linked.Token).ConfigureAwait(false);
            ItemCompleted?.Invoke(this, (item, result));
            _itemCancellation.TryRemove(item.Id, out _);
            await _itemsGate.WaitAsync(linked.Token).ConfigureAwait(false);
            try
            {
                _items.RemoveAll(i => i.Id == item.Id);
            }
            finally
            {
                _itemsGate.Release();
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _shutdown.Cancel();
        _channel.Writer.TryComplete();
        try
        {
            await _worker.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }

        foreach (var kvp in _itemCancellation)
        {
            kvp.Value.Dispose();
        }

        _itemsGate.Dispose();
        _shutdown.Dispose();
    }
}
