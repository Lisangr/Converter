using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Converter.Domain.Models;

namespace Converter.Application.Abstractions
{
    public interface IQueueStore
    {
        IAsyncEnumerable<QueueItem> GetAllAsync(CancellationToken cancellationToken = default);
        IAsyncEnumerable<QueueItem> GetPendingAsync(CancellationToken cancellationToken = default);
        Task AddAsync(QueueItem item, CancellationToken cancellationToken = default);
        Task UpdateAsync(QueueItem item, CancellationToken cancellationToken = default);
        Task RemoveAsync(Guid id, CancellationToken cancellationToken = default);
        Task<bool> TryReserveAsync(Guid id, CancellationToken cancellationToken = default);
        Task CompleteAsync(Guid id, ConversionStatus finalStatus, string? errorMessage = null, long? outputFileSizeBytes = null, DateTime? completedAt = null, CancellationToken cancellationToken = default);
    }
}
