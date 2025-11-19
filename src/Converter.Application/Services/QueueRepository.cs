/// <summary>
/// Реализация <see cref="IQueueRepository"/> для управления очередью элементов конвертации.
/// Отвечает за:
/// - Хранение и управление очередью элементов
/// - Управление приоритетами элементов
/// - Выполнение базовых CRUD-операций с элементами очереди
/// </summary>
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Converter.Application.Abstractions;
using Converter.Domain.Models;
using Microsoft.Extensions.Logging;

namespace Converter.Application.Services
{
    public class QueueRepository : IQueueRepository
    {
        private readonly ConcurrentDictionary<Guid, QueueItem> _items = new();
        private readonly ILogger<QueueRepository> _logger;
        private readonly IQueueStore _queueStore;
        private readonly SemaphoreSlim _initLock = new(1, 1);
        private bool _initialized;

        public event EventHandler<QueueItem> ItemAdded;
        public event EventHandler<QueueItem> ItemUpdated;
        public event EventHandler<Guid> ItemRemoved;

        public QueueRepository(ILogger<QueueRepository> logger, IQueueStore queueStore)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _queueStore = queueStore ?? throw new ArgumentNullException(nameof(queueStore));
        }

        private async Task EnsureInitializedAsync()
        {
            if (_initialized)
            {
                return;
            }

            await _initLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_initialized)
                {
                    return;
                }

                _logger.LogInformation("Initializing queue repository from persistent store");
                await foreach (var item in _queueStore.GetAllAsync().ConfigureAwait(false))
                {
                    _items[item.Id] = item;
                }

                _initialized = true;
                _logger.LogInformation("Queue repository initialized with {Count} items", _items.Count);
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

            if (_items.TryAdd(item.Id, item))
            {
                _logger.LogInformation("Added item {ItemId} to queue", item.Id);
                ItemAdded?.Invoke(this, item);
            }
        }

        public async Task AddRangeAsync(IEnumerable<QueueItem> items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            await EnsureInitializedAsync().ConfigureAwait(false);
            foreach (var item in items)
            {
                await AddAsync(item).ConfigureAwait(false);
            }
        }

        public async Task UpdateAsync(QueueItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            await EnsureInitializedAsync().ConfigureAwait(false);
            await _queueStore.UpdateAsync(item).ConfigureAwait(false);

            if (_items.TryGetValue(item.Id, out var existingItem))
            {
                _items[item.Id] = item;
                _logger.LogDebug("Updated item {ItemId} in queue", item.Id);
                ItemUpdated?.Invoke(this, item);
            }
            else
            {
                _logger.LogWarning("Attempted to update non-existent item {ItemId}", item.Id);
            }
        }

        public async Task RemoveAsync(Guid id)
        {
            await EnsureInitializedAsync().ConfigureAwait(false);
            await _queueStore.RemoveAsync(id).ConfigureAwait(false);

            if (_items.TryRemove(id, out _))
            {
                _logger.LogInformation("Removed item {ItemId} from queue", id);
                ItemRemoved?.Invoke(this, id);
            }
            else
            {
                _logger.LogWarning("Attempted to remove non-existent item {ItemId}", id);
            }
        }

        public async Task<QueueItem> GetByIdAsync(Guid id)
        {
            await EnsureInitializedAsync().ConfigureAwait(false);
            _items.TryGetValue(id, out var item);
            return item;
        }

        public async Task<IReadOnlyList<QueueItem>> GetAllAsync()
        {
            await EnsureInitializedAsync().ConfigureAwait(false);
            return _items.Values.OrderBy(x => x.AddedAt).ToList();
        }

        public async Task<IReadOnlyList<QueueItem>> GetPendingItemsAsync()
        {
            await EnsureInitializedAsync().ConfigureAwait(false);
            return _items.Values
                .Where(x => x.Status == ConversionStatus.Pending)
                .OrderBy(x => x.AddedAt)
                .ToList();
        }

        public async Task<int> GetPendingCountAsync()
        {
            await EnsureInitializedAsync().ConfigureAwait(false);
            return _items.Values.Count(x => x.Status == ConversionStatus.Pending);
        }
    }
}
