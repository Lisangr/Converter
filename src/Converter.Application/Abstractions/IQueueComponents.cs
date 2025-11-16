using System;
using System.Collections.Generic;

namespace Converter.Application.Abstractions;

public interface IConversionQueue
{
    event EventHandler<QueueItemSnapshot> ItemChanged;
    void Enqueue(QueueItemModel item);
    void EnqueueMany(IEnumerable<QueueItemModel> items);
    QueueItemSnapshot? TryMarkNextProcessing(ISchedulingStrategy schedulingStrategy);
    QueueItemSnapshot Update(Guid id, Action<QueueItemModel> mutate);
    IReadOnlyList<QueueItemSnapshot> Snapshot();
}

public interface ISchedulingStrategy
{
    QueueItemSnapshot? SelectNext(IReadOnlyCollection<QueueItemSnapshot> pendingItems);
}

public interface IProgressReporter
{
    IProgress<int> Create(Guid itemId, Action<Guid, int> progressHandler);
}
