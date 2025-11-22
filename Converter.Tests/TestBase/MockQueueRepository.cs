using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Converter.Application.Abstractions;
using Converter.Domain.Models;

namespace Converter.Tests.TestBase
{
    /// <summary>
    /// Тестовая реализация IQueueRepository с поддержкой событий
    /// Используется для load-тестов вместо Mock объектов
    /// </summary>
    public class MockQueueRepository : IQueueRepository
    {
        private readonly ConcurrentDictionary<Guid, QueueItem> _items = new();
        private readonly object _lockObject = new object();

        // События
        public event EventHandler<QueueItem>? ItemAdded;
        public event EventHandler<QueueItem>? ItemUpdated;
        public event EventHandler<Guid>? ItemRemoved;

        public Task AddAsync(QueueItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            
            lock (_lockObject)
            {
                _items[item.Id] = item;
            }

            // Вызываем событие после добавления
            Task.Run(() => ItemAdded?.Invoke(this, item));
            return Task.CompletedTask;
        }

        public Task AddRangeAsync(IEnumerable<QueueItem> items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));

            var itemsList = items.ToList();
            lock (_lockObject)
            {
                foreach (var item in itemsList)
                {
                    _items[item.Id] = item;
                }
            }

            // Вызываем события для каждого добавленного элемента
            Task.Run(() =>
            {
                foreach (var item in itemsList)
                {
                    ItemAdded?.Invoke(this, item);
                }
            });

            return Task.CompletedTask;
        }

        public Task UpdateAsync(QueueItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            lock (_lockObject)
            {
                if (_items.ContainsKey(item.Id))
                {
                    _items[item.Id] = item;
                }
            }

            // Вызываем событие после обновления
            Task.Run(() => ItemUpdated?.Invoke(this, item));
            return Task.CompletedTask;
        }

        public Task RemoveAsync(Guid id)
        {
            lock (_lockObject)
            {
                _items.TryRemove(id, out _);
            }

            // Вызываем событие после удаления
            Task.Run(() => ItemRemoved?.Invoke(this, id));
            return Task.CompletedTask;
        }

        public Task RemoveRangeAsync(IEnumerable<Guid> ids)
        {
            if (ids == null) throw new ArgumentNullException(nameof(ids));

            var idsList = ids.ToList();
            lock (_lockObject)
            {
                foreach (var id in idsList)
                {
                    _items.TryRemove(id, out _);
                }
            }

            // Вызываем события для каждого удаленного элемента
            Task.Run(() =>
            {
                foreach (var id in idsList)
                {
                    ItemRemoved?.Invoke(this, id);
                }
            });

            return Task.CompletedTask;
        }

        public Task<QueueItem> GetByIdAsync(Guid id)
        {
            lock (_lockObject)
            {
                _items.TryGetValue(id, out var item);
                return Task.FromResult(item ?? throw new KeyNotFoundException($"Item with id {id} not found"));
            }
        }

        public Task<IReadOnlyList<QueueItem>> GetAllAsync()
        {
            lock (_lockObject)
            {
                var items = _items.Values.ToList().AsReadOnly();
                return Task.FromResult<IReadOnlyList<QueueItem>>(items);
            }
        }

        public Task<IReadOnlyList<QueueItem>> GetPendingItemsAsync()
        {
            lock (_lockObject)
            {
                var pendingItems = _items.Values
                    .Where(item => item.Status == ConversionStatus.Pending)
                    .ToList()
                    .AsReadOnly();
                return Task.FromResult<IReadOnlyList<QueueItem>>(pendingItems);
            }
        }

        public Task<int> GetPendingCountAsync()
        {
            lock (_lockObject)
            {
                var count = _items.Values.Count(item => item.Status == ConversionStatus.Pending);
                return Task.FromResult(count);
            }
        }

        /// <summary>
        /// Очищает все элементы (для тестов)
        /// </summary>
        public void Clear()
        {
            lock (_lockObject)
            {
                _items.Clear();
            }
        }

        /// <summary>
        /// Возвращает количество элементов в репозитории
        /// </summary>
        public int Count
        {
            get
            {
                lock (_lockObject)
                {
                    return _items.Count;
                }
            }
        }
    }
}