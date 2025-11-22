using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Converter.Application.Abstractions;
using Converter.Domain.Models;
using Microsoft.Extensions.Logging;

namespace Converter.Infrastructure
{
    /// <summary>
    /// Реализация <see cref="IQueueRepository"/> как кэш/наблюдатель для очереди элементов конвертации.
    /// Особенности:
    /// - Простой кэш в памяти, синхронизированный с IQueueStore
    /// - Делегирует все операции в IQueueStore (единственный источник правды)
    /// - Генерирует события для UI об изменениях в очереди
    /// - Не содержит бизнес-логики, только кэширование и делегирование
    /// </summary>
    public class QueueRepository : IQueueRepository
    {
        private readonly ConcurrentDictionary<Guid, QueueItem> _cache = new();
        private readonly ILogger<QueueRepository> _logger;
        private readonly IQueueStore _queueStore;
        private readonly SemaphoreSlim _initLock = new(1, 1);
        private bool _initialized;

        public event EventHandler<QueueItem>? ItemAdded;
        public event EventHandler<QueueItem>? ItemUpdated;
        public event EventHandler<Guid>? ItemRemoved;

        public QueueRepository(ILogger<QueueRepository> logger, IQueueStore queueStore)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _queueStore = queueStore ?? throw new ArgumentNullException(nameof(queueStore));
        }

        private async Task EnsureInitializedAsync()
        {
            if (_initialized)
                return;

            await _initLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_initialized)
                    return;

                _logger.LogInformation("Initializing queue repository cache from persistent store");

                await foreach (var item in _queueStore.GetAllAsync().ConfigureAwait(false))
                {
                    _cache[item.Id] = item;
                }

                _initialized = true;
                _logger.LogInformation("Queue repository cache initialized with {Count} items", _cache.Count);
            }
            finally
            {
                _initLock.Release();
            }
        }

        public async Task AddAsync(QueueItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            await EnsureInitializedAsync().ConfigureAwait(false);

            await _queueStore.AddAsync(item).ConfigureAwait(false);
            _cache[item.Id] = item;

            _logger.LogDebug("Added item {ItemId} to queue", item.Id);
            ItemAdded?.Invoke(this, item);
        }

        public async Task AddRangeAsync(IEnumerable<QueueItem> items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            await EnsureInitializedAsync().ConfigureAwait(false);

            var itemList = items.ToList();
            foreach (var item in itemList)
            {
                await AddAsync(item).ConfigureAwait(false);
            }
        }

        public async Task UpdateAsync(QueueItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            await EnsureInitializedAsync().ConfigureAwait(false);

            await _queueStore.UpdateAsync(item).ConfigureAwait(false);
            _cache[item.Id] = item;

            _logger.LogDebug("Updated item {ItemId} in cache", item.Id);
            ItemUpdated?.Invoke(this, item);
        }

        public async Task RemoveAsync(Guid id)
        {
            await EnsureInitializedAsync().ConfigureAwait(false);

            await _queueStore.RemoveAsync(id).ConfigureAwait(false);
            _cache.TryRemove(id, out _);

            _logger.LogDebug("Removed item {ItemId} from queue", id);
            ItemRemoved?.Invoke(this, id);
        }

        public async Task<QueueItem?> GetByIdAsync(Guid id)
        {
            await EnsureInitializedAsync().ConfigureAwait(false);
            _cache.TryGetValue(id, out var item);
            return item;
        }

        public async Task<IReadOnlyList<QueueItem>> GetAllAsync()
        {
            await EnsureInitializedAsync().ConfigureAwait(false);
            return _cache.Values.OrderBy(x => x.AddedAt).ToList();
        }

        public async Task<IReadOnlyList<QueueItem>> GetPendingItemsAsync()
        {
            await EnsureInitializedAsync().ConfigureAwait(false);
            return _cache.Values
                .Where(x => x.Status == ConversionStatus.Pending)
                .OrderBy(x => x.AddedAt)
                .ToList();
        }

        public async Task<int> GetPendingCountAsync()
        {
            await EnsureInitializedAsync().ConfigureAwait(false);
            return _cache.Values.Count(x => x.Status == ConversionStatus.Pending);
        }

        public async Task RemoveRangeAsync(IEnumerable<Guid> ids)
        {
            if (ids == null) throw new ArgumentNullException(nameof(ids));

            var idList = ids.ToList();
            if (idList.Count == 0) return;

            await EnsureInitializedAsync().ConfigureAwait(false);

            foreach (var id in idList)
            {
                await _queueStore.RemoveAsync(id).ConfigureAwait(false);
                _cache.TryRemove(id, out _);
                ItemRemoved?.Invoke(this, id);
            }

            _logger.LogDebug("Removed {Count} items from queue", idList.Count);
        }
    }
}
