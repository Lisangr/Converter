using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Converter.Domain.Models;

namespace Converter.Application.Abstractions
{
    public interface IQueueRepository
    {
        event EventHandler<QueueItem> ItemAdded;
        event EventHandler<QueueItem> ItemUpdated;
        event EventHandler<Guid> ItemRemoved;

        Task AddAsync(QueueItem item);
        Task AddRangeAsync(IEnumerable<QueueItem> items);
        Task UpdateAsync(QueueItem item);
        Task RemoveAsync(Guid id);
        Task<QueueItem> GetByIdAsync(Guid id);
        Task<IReadOnlyList<QueueItem>> GetAllAsync();
        Task<IReadOnlyList<QueueItem>> GetPendingItemsAsync();
        Task<int> GetPendingCountAsync();
        Task<bool> TryReserveAsync(QueueItem item, CancellationToken cancellationToken = default);
        Task CompleteAsync(
            QueueItem item,
            ConversionStatus finalStatus,
            string? errorMessage = null,
            long? outputFileSizeBytes = null,
            DateTime? completedAt = null,
            CancellationToken cancellationToken = default);
    }
}
