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

        public event EventHandler<QueueItem> ItemAdded;
        public event EventHandler<QueueItem> ItemUpdated;
        public event EventHandler<Guid> ItemRemoved;

        public QueueRepository(ILogger<QueueRepository> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task AddAsync(QueueItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            if (_items.TryAdd(item.Id, item))
            {
                _logger.LogInformation("Added item {ItemId} to queue", item.Id);
                ItemAdded?.Invoke(this, item);
            }

            return Task.CompletedTask;
        }

        public Task AddRangeAsync(IEnumerable<QueueItem> items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            return Task.WhenAll(items.Select(AddAsync));
        }

        public Task UpdateAsync(QueueItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

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

            return Task.CompletedTask;
        }

        public Task RemoveAsync(Guid id)
        {
            if (_items.TryRemove(id, out _))
            {
                _logger.LogInformation("Removed item {ItemId} from queue", id);
                ItemRemoved?.Invoke(this, id);
            }
            else
            {
                _logger.LogWarning("Attempted to remove non-existent item {ItemId}", id);
            }

            return Task.CompletedTask;
        }

        public Task<QueueItem> GetByIdAsync(Guid id)
        {
            _items.TryGetValue(id, out var item);
            return Task.FromResult(item);
        }

        public Task<IReadOnlyList<QueueItem>> GetAllAsync()
        {
            return Task.FromResult<IReadOnlyList<QueueItem>>(
                _items.Values.OrderBy(x => x.AddedAt).ToList());
        }

        public Task<IReadOnlyList<QueueItem>> GetPendingItemsAsync()
        {
            return Task.FromResult<IReadOnlyList<QueueItem>>(
                _items.Values
                    .Where(x => x.Status == ConversionStatus.Pending)
                    .OrderBy(x => x.AddedAt)
                    .ToList());
        }

        public Task<int> GetPendingCountAsync()
        {
            return Task.FromResult(_items.Values.Count(x => x.Status == ConversionStatus.Pending));
        }
    }
}
