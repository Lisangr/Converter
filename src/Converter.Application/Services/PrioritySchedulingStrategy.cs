using System;
using System.Collections.Generic;
using System.Linq;
using Converter.Application.Abstractions;

namespace Converter.Application.Services;

public sealed class PrioritySchedulingStrategy : ISchedulingStrategy
{
    public QueueItemSnapshot? SelectNext(IReadOnlyCollection<QueueItemSnapshot> pendingItems)
    {
        if (pendingItems is null) throw new ArgumentNullException(nameof(pendingItems));
        if (pendingItems.Count == 0)
        {
            return null;
        }

        return pendingItems
            .OrderByDescending(i => i.Priority)
            .ThenBy(i => i.EnqueuedAtUtc)
            .FirstOrDefault();
    }
}
