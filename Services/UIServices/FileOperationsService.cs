using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Converter.Application.Abstractions;
using Converter.Models;
using Converter.Domain.Models;
using Microsoft.Extensions.Logging;

namespace Converter.Services.UIServices
{
    public class FileOperationsService : IFileOperationsService, IDisposable
    {
        private readonly IQueueRepository _queueRepository;
        private readonly IQueueProcessor _queueProcessor;
        private readonly ILogger<FileOperationsService> _logger;
        private bool _disposed;

        public FileOperationsService(
            IQueueRepository queueRepository,
            IQueueProcessor queueProcessor,
            ILogger<FileOperationsService> logger)
        {
            _queueRepository = queueRepository ?? throw new ArgumentNullException(nameof(queueRepository));
            _queueProcessor = queueProcessor ?? throw new ArgumentNullException(nameof(queueProcessor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _disposed = false;
            
            // Подписываемся на события репозитория
            SubscribeToRepositoryEvents();
        }

        public event EventHandler<QueueItem>? QueueItemAdded;
        public event EventHandler<QueueUpdatedEventArgs>? QueueUpdated;

        public async Task AddFilesAsync(IEnumerable<string> filePaths)
        {
            if (filePaths == null) throw new ArgumentNullException(nameof(filePaths));

            _logger.LogInformation("Начало добавления файлов: {FileCount} файлов", filePaths.Count());
            
            try
            {
                foreach (var filePath in filePaths)
                {
                    if (!System.IO.File.Exists(filePath))
                    {
                        _logger.LogWarning("Файл не найден: {FilePath}", filePath);
                        continue;
                    }

                    var queueItem = new QueueItem
                    {
                        Id = Guid.NewGuid(),
                        FilePath = filePath,
                        Status = ConversionStatus.Pending,
                        AddedAt = DateTime.UtcNow
                    };

                    await _queueRepository.AddAsync(queueItem);
                    _logger.LogInformation("Файл добавлен в очередь: {FilePath}", filePath);
                }
                
                _logger.LogInformation("Успешно добавлено {FileCount} файлов в очередь", filePaths.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при добавлении файлов в очередь");
                throw;
            }
        }

        public async Task RemoveSelectedFilesAsync(IEnumerable<QueueItem> selectedItems)
        {
            if (selectedItems == null) throw new ArgumentNullException(nameof(selectedItems));

            try
            {
                var removeTasks = selectedItems
                    .Where(item => item != null)
                    .Select(item => _queueRepository.RemoveAsync(item.Id));

                await Task.WhenAll(removeTasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении выбранных файлов из очереди");
                throw;
            }
        }

        public async Task ClearAllFilesAsync()
        {
            try
            {
                var items = await _queueRepository.GetAllAsync();
                var removeTasks = items.Select(item => _queueRepository.RemoveAsync(item.Id));
                await Task.WhenAll(removeTasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при очистке очереди");
                throw;
            }
        }

        public IReadOnlyList<QueueItem> GetQueueItems()
        {
            // В реальном приложении это может быть асинхронным вызовом
            return _queueRepository.GetAllAsync().GetAwaiter().GetResult().ToList();
        }
        public async Task UpdateQueueItem(QueueItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            try
            {
                await _queueRepository.UpdateAsync(item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении элемента очереди");
                throw;
            }
        }
        private async Task OnQueueItemAdded(object? sender, QueueItem item)
        {
            if (item == null) return;
            
            try
            {
                _logger?.LogInformation("Добавление элемента в очередь: {FileName}", item.FilePath);

                // Получаем актуальный список элементов очереди
                var items = await _queueRepository.GetAllAsync();

                // Вызываем события в потоке UI
                await InvokeOnUIThreadAsync(() =>
                {
                    // Уведомляем о конкретно добавленном элементе (для точечных реакций UI)
                    QueueItemAdded?.Invoke(this, item);

                    // И полное обновление очереди для гридов/биндингов
                    QueueUpdated?.Invoke(this, new QueueUpdatedEventArgs(items.ToList()));
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Ошибка при обработке добавления элемента в очередь");
                throw;
            }
        }

        private async Task OnQueueItemUpdated(object? sender, QueueItem item)
        {
            try
            {
                // Get the updated queue items
                var items = await _queueRepository.GetAllAsync();

                // Raise the event on the UI thread
                await InvokeOnUIThreadAsync(() => 
                {
                    QueueUpdated?.Invoke(this, new QueueUpdatedEventArgs(items.ToList()));
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnQueueItemUpdated");
                throw;
            }
        }

        private async Task OnQueueItemRemoved(object? sender, Guid itemId)
        {
            try
            {
                // Get the updated queue items
                var items = await _queueRepository.GetAllAsync();

                // Raise the event on the UI thread
                await InvokeOnUIThreadAsync(() => 
                {
                    QueueUpdated?.Invoke(this, new QueueUpdatedEventArgs(items.ToList()));
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnQueueItemRemoved");
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                // Отписываемся от событий
                if (_itemAddedHandler != null)
                    _queueRepository.ItemAdded -= _itemAddedHandler;
                if (_itemUpdatedHandler != null)
                    _queueRepository.ItemUpdated -= _itemUpdatedHandler;
                if (_itemRemovedHandler != null)
                    _queueRepository.ItemRemoved -= _itemRemovedHandler;
                
                _disposed = true;
            }
        }

        ~FileOperationsService()
        {
            Dispose(disposing: false);
        }

        // Сохраняем ссылки на обработчики для корректной отписки
        private EventHandler<QueueItem>? _itemAddedHandler;
        private EventHandler<QueueItem>? _itemUpdatedHandler;
        private EventHandler<Guid>? _itemRemovedHandler;

        private Task InvokeOnUIThreadAsync(Action action)
        {
            // Получаем SynchronizationContext из UI потока
            var syncContext = SynchronizationContext.Current;
            
            if (syncContext == null)
            {
                // Если контекста синхронизации нет, выполняем напрямую
                action();
                return Task.CompletedTask;
            }
            
            // Иначе используем контекст синхронизации для выполнения в UI потоке
            var tcs = new TaskCompletionSource<bool>();
            syncContext.Post(_ => 
            {
                try
                {
                    action();
                    tcs.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            }, null);
            
            return tcs.Task;
        }

        private void SubscribeToRepositoryEvents()
        {
            // Создаем обработчики событий с сохранением ссылок
            _itemAddedHandler = async (sender, item) =>
            {
                try
                {
                    await OnQueueItemAdded(sender, item);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Ошибка при обработке добавления элемента");
                }
            };

            _itemUpdatedHandler = async (sender, item) =>
            {
                try
                {
                    await OnQueueItemUpdated(sender, item);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Ошибка при обновлении элемента");
                }
            };

            _itemRemovedHandler = async (sender, itemId) =>
            {
                try
                {
                    await OnQueueItemRemoved(sender, itemId);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Ошибка при удалении элемента");
                }
            };

            // Подписываемся на события
            _queueRepository.ItemAdded += _itemAddedHandler;
            _queueRepository.ItemUpdated += _itemUpdatedHandler;
            _queueRepository.ItemRemoved += _itemRemovedHandler;
        }
    }
}