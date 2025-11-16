using System;
using System.Collections.Generic;
using System.Linq;
using Converter.Application.Abstractions;

namespace Converter.Application.Services;

public sealed class ConversionQueueManager : IConversionQueue
{
    private readonly List<QueueItemModel> _items = new();
    private readonly object _sync = new();

    public event EventHandler<QueueItemSnapshot>? ItemChanged;

    public void Enqueue(QueueItemModel item)
    {
        if (item is null) throw new ArgumentNullException(nameof(item));
        QueueItemSnapshot snapshot;
        lock (_sync)
        {
            if (item.Id == Guid.Empty)
            {
                item.Id = Guid.NewGuid();
            }

            if (item.EnqueuedAtUtc == default)
            {
                item.EnqueuedAtUtc = DateTime.UtcNow;
            }

            item.Status = QueueItemStatuses.Pending;
            item.Progress = 0;
            _items.Add(item);
            snapshot = item.ToSnapshot();
        }

        ItemChanged?.Invoke(this, snapshot);
    }

    public void EnqueueMany(IEnumerable<QueueItemModel> items)
    {
        if (items is null) throw new ArgumentNullException(nameof(items));
        foreach (var item in items)
        {
            Enqueue(item);
        }
    }

    public QueueItemSnapshot? TryMarkNextProcessing(ISchedulingStrategy schedulingStrategy)
    {
        if (schedulingStrategy is null) throw new ArgumentNullException(nameof(schedulingStrategy));
        QueueItemSnapshot? snapshot = null;
        lock (_sync)
        {
            var pending = _items
                .Where(i => string.Equals(i.Status, QueueItemStatuses.Pending, StringComparison.OrdinalIgnoreCase))
                .Select(i => i.ToSnapshot())
                .ToList();

            if (pending.Count == 0)
            {
                return null;
            }

            var next = schedulingStrategy.SelectNext(pending);
            if (next == null)
            {
                return null;
            }

            var state = _items.FirstOrDefault(i => i.Id == next.Id);
            if (state == null)
            {
                return null;
            }

            state.Status = QueueItemStatuses.Processing;
            state.Progress = 0;
            state.ErrorMessage = null;
            snapshot = state.ToSnapshot();
        }

        ItemChanged?.Invoke(this, snapshot);
        return snapshot;
    }

    public QueueItemSnapshot Update(Guid id, Action<QueueItemModel> mutate)
    {
        if (mutate is null) throw new ArgumentNullException(nameof(mutate));
        QueueItemSnapshot snapshot;
        lock (_sync)
        {
            var item = _items.FirstOrDefault(i => i.Id == id)
                       ?? throw new InvalidOperationException($"Queue item {id} not found");
            mutate(item);
            snapshot = item.ToSnapshot();
        }

        ItemChanged?.Invoke(this, snapshot);
        return snapshot;
    }

    public IReadOnlyList<QueueItemSnapshot> Snapshot()
    {
        lock (_sync)
        {
            return _items.Select(i => i.ToSnapshot()).ToList();
        }
    }
}
