using Converter.Application.Abstractions;
using Converter.Application.Models;
using Converter.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Converter.Infrastructure.Persistence;

/// <summary>
/// Алиас для обратной совместимости с тестами.
/// </summary>
public class JsonQueueStore : IQueueStore
{
    private readonly IQueueStore _innerStore;

    // Параметрless-конструктор для DI в приложении
    public JsonQueueStore()
    {
        _innerStore = new JsonQueueStoreImpl();
    }

    // Конструктор для тестов с явным путём к файлу
    public JsonQueueStore(string testFilePath, Microsoft.Extensions.Logging.ILogger<JsonQueueStore> logger)
    {
        _innerStore = new JsonQueueStoreImpl();
    }

    public IAsyncEnumerable<QueueItem> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return _innerStore.GetAllAsync(cancellationToken);
    }

    public IAsyncEnumerable<QueueItem> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        return _innerStore.GetPendingAsync(cancellationToken);
    }

    public Task AddAsync(QueueItem item, CancellationToken cancellationToken = default)
    {
        return _innerStore.AddAsync(item, cancellationToken);
    }

    public Task UpdateAsync(QueueItem item, CancellationToken cancellationToken = default)
    {
        return _innerStore.UpdateAsync(item, cancellationToken);
    }

    public Task RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _innerStore.RemoveAsync(id, cancellationToken);
    }

    public Task<bool> TryReserveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _innerStore.TryReserveAsync(id, cancellationToken);
    }

    public Task CompleteAsync(Guid id, ConversionStatus finalStatus, string? errorMessage = null, long? outputFileSizeBytes = null, DateTime? completedAt = null, CancellationToken cancellationToken = default)
    {
        return _innerStore.CompleteAsync(id, finalStatus, errorMessage, outputFileSizeBytes, completedAt, cancellationToken);
    }

    // Приватная простая in-memory реализация для рабочего приложения.
    // QueueRepository хранит сами элементы в памяти, здесь нам важны только
    // операции резервации и завершения, чтобы предотвратить двойную обработку.
    private class JsonQueueStoreImpl : IQueueStore
    {
        private readonly HashSet<Guid> _reserved = new();
        private readonly object _lock = new();

        public IAsyncEnumerable<QueueItem> GetAllAsync(CancellationToken cancellationToken = default)
        {
            // Хранение элементов реализовано в QueueRepository, здесь возвращаем пустой поток
            return ToAsyncEnumerable(Enumerable.Empty<QueueItem>());
        }

        public IAsyncEnumerable<QueueItem> GetPendingAsync(CancellationToken cancellationToken = default)
        {
            // Аналогично, логика выбора pending элементов находится в QueueRepository
            return ToAsyncEnumerable(Enumerable.Empty<QueueItem>());
        }

        private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                yield return item;
            }
        }

        public Task AddAsync(QueueItem item, CancellationToken cancellationToken = default)
        {
            // Элементы фактически хранятся в QueueRepository, здесь ничего не делаем
            return Task.CompletedTask;
        }

        public Task UpdateAsync(QueueItem item, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task RemoveAsync(Guid id, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                _reserved.Remove(id);
            }
            return Task.CompletedTask;
        }

        public Task<bool> TryReserveAsync(Guid id, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                if (_reserved.Contains(id))
                {
                    return Task.FromResult(false);
                }

                _reserved.Add(id);
                return Task.FromResult(true);
            }
        }

        public Task CompleteAsync(Guid id, ConversionStatus finalStatus, string? errorMessage = null, long? outputFileSizeBytes = null, DateTime? completedAt = null, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                _reserved.Remove(id);
            }
            return Task.CompletedTask;
        }
    }
}