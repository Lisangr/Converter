using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Converter.Application.Abstractions;
using Converter.Models;
using Microsoft.Extensions.Logging;

namespace Converter.Infrastructure.Persistence
{
    public sealed class JsonQueueStore : IQueueStore
    {
        private readonly string _queuePath;
        private readonly ILogger<JsonQueueStore> _logger;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly SemaphoreSlim _fileLock = new(1, 1);

        public JsonQueueStore(ILogger<JsonQueueStore> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appData, "Converter");
            Directory.CreateDirectory(appFolder);
            _queuePath = Path.Combine(appFolder, "queue.json");

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true
            };
        }

        private async Task<List<QueueItem>> ReadAllInternalAsync(CancellationToken cancellationToken)
        {
            if (!File.Exists(_queuePath))
            {
                return new List<QueueItem>();
            }

            await using var fs = File.OpenRead(_queuePath);
            var items = await JsonSerializer.DeserializeAsync<List<QueueItem>>(fs, _jsonOptions, cancellationToken)
                        .ConfigureAwait(false);
            return items ?? new List<QueueItem>();
        }

        private async Task WriteAllInternalAsync(List<QueueItem> items, CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_queuePath)!);
            await using var fs = File.Create(_queuePath);
            await JsonSerializer.SerializeAsync(fs, items, _jsonOptions, cancellationToken)
                .ConfigureAwait(false);
        }

        public async IAsyncEnumerable<QueueItem> GetAllAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await _fileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var items = await ReadAllInternalAsync(cancellationToken).ConfigureAwait(false);
                foreach (var item in items)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return item;
                }
            }
            finally
            {
                _fileLock.Release();
            }
        }

        public async IAsyncEnumerable<QueueItem> GetPendingAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await _fileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var items = await ReadAllInternalAsync(cancellationToken).ConfigureAwait(false);
                foreach (var item in items.Where(x => x.Status == ConversionStatus.Pending))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return item;
                }
            }
            finally
            {
                _fileLock.Release();
            }
        }

        public async Task AddAsync(QueueItem item, CancellationToken cancellationToken = default)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            await _fileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var items = await ReadAllInternalAsync(cancellationToken).ConfigureAwait(false);
                if (items.Any(x => x.Id == item.Id))
                {
                    return;
                }

                items.Add(item);
                await WriteAllInternalAsync(items, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding queue item {ItemId} to JSON store", item.Id);
                throw;
            }
            finally
            {
                _fileLock.Release();
            }
        }

        public async Task UpdateAsync(QueueItem item, CancellationToken cancellationToken = default)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            await _fileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var items = await ReadAllInternalAsync(cancellationToken).ConfigureAwait(false);
                var index = items.FindIndex(x => x.Id == item.Id);
                if (index >= 0)
                {
                    items[index] = item;
                    await WriteAllInternalAsync(items, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating queue item {ItemId} in JSON store", item.Id);
                throw;
            }
            finally
            {
                _fileLock.Release();
            }
        }

        public async Task RemoveAsync(Guid id, CancellationToken cancellationToken = default)
        {
            await _fileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var items = await ReadAllInternalAsync(cancellationToken).ConfigureAwait(false);
                var removed = items.RemoveAll(x => x.Id == id);
                if (removed > 0)
                {
                    await WriteAllInternalAsync(items, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing queue item {ItemId} from JSON store", id);
                throw;
            }
            finally
            {
                _fileLock.Release();
            }
        }

        public async Task<bool> TryReserveAsync(Guid id, CancellationToken cancellationToken = default)
        {
            await _fileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var items = await ReadAllInternalAsync(cancellationToken).ConfigureAwait(false);
                var item = items.FirstOrDefault(x => x.Id == id);
                if (item == null)
                {
                    return false;
                }

                if (item.Status != ConversionStatus.Pending)
                {
                    return false;
                }

                item.Status = ConversionStatus.Processing;
                item.StartedAt = DateTime.UtcNow;

                var index = items.FindIndex(x => x.Id == id);
                if (index >= 0)
                {
                    items[index] = item;
                }

                await WriteAllInternalAsync(items, cancellationToken).ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reserving queue item {ItemId} in JSON store", id);
                throw;
            }
            finally
            {
                _fileLock.Release();
            }
        }

        public async Task CompleteAsync(Guid id, ConversionStatus finalStatus, string? errorMessage = null, long? outputFileSizeBytes = null, DateTime? completedAt = null, CancellationToken cancellationToken = default)
        {
            await _fileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var items = await ReadAllInternalAsync(cancellationToken).ConfigureAwait(false);
                var index = items.FindIndex(x => x.Id == id);
                if (index < 0)
                {
                    return;
                }

                var item = items[index];
                item.Status = finalStatus;
                item.ErrorMessage = errorMessage;
                item.OutputFileSizeBytes = outputFileSizeBytes;
                item.CompletedAt = completedAt ?? DateTime.UtcNow;

                items[index] = item;
                await WriteAllInternalAsync(items, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing queue item {ItemId} in JSON store", id);
                throw;
            }
            finally
            {
                _fileLock.Release();
            }
        }
    }
}
