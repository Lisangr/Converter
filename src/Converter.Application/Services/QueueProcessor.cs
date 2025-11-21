/// <summary>
/// Фоновый сервис для обработки элементов очереди конвертации.
/// Особенности:
/// - Наследуется от <see cref="BackgroundService"/> для работы в фоне
/// - Асинхронно обрабатывает элементы из очереди
/// - Использует <see cref="Channel{QueueItem}"/> для потокобезопасной асинхронной обработки
/// </summary>
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Converter.Application.Abstractions;
using Converter.Domain.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Converter.Application.Services
{
    public class QueueProcessor : BackgroundService, IQueueProcessor
    {
        private readonly IQueueRepository _queueRepository;
        private readonly IQueueStore _queueStore;
        private readonly IConversionUseCase _conversionUseCase;
        private readonly ILogger<QueueProcessor> _logger;
        private readonly SemaphoreSlim _processingLock = new(1, 1);
        private CancellationTokenSource _processingCts;
        private Task? _processingTask;
        private bool _isPaused;
        private bool _isRunning;

        public event EventHandler<QueueItem> ItemStarted;
        public event EventHandler<QueueItem> ItemCompleted;
        public event EventHandler<QueueItem> ItemFailed;
        public event EventHandler<QueueProgressEventArgs> ProgressChanged;
        public event EventHandler QueueCompleted;

        public bool IsRunning => _isRunning;
        public bool IsPaused => _isPaused;

        public QueueProcessor(
            IQueueRepository queueRepository,
            IQueueStore queueStore,
            IConversionUseCase conversionUseCase,
            ILogger<QueueProcessor> logger)
        {
            _queueRepository = queueRepository ?? throw new ArgumentNullException(nameof(queueRepository));
            _queueStore = queueStore ?? throw new ArgumentNullException(nameof(queueStore));
            _conversionUseCase = conversionUseCase ?? throw new ArgumentNullException(nameof(conversionUseCase));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Инициализация состояния - изначально на паузе до активации
            _isRunning = false;
            _isPaused = true;
        }

        public async Task StartProcessingAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Активация обработки очереди...");
            
            // Сбрасываем состояние паузы и активируем обработку
            _isPaused = false;
            _isRunning = true;
            
            _processingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _logger.LogInformation("Queue processor активирован для обработки. IsPaused={IsPaused}, IsRunning={IsRunning}", _isPaused, _isRunning);
            
            // Попробуем получить элементы из очереди для проверки
            try
            {
                var pendingItems = await _queueRepository.GetPendingItemsAsync();
                _logger.LogInformation("В StartProcessingAsync найдено элементов в очереди: {Count}", pendingItems.Count);
                
                foreach (var item in pendingItems)
                {
                    _logger.LogInformation("Элемент в очереди: {ItemId} - {FileName} - {Status}", 
                        item.Id, item.FileName, item.Status);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении элементов очереди в StartProcessingAsync");
            }
        }

        public async Task StopProcessingAsync()
        {
            if (!_isRunning) return;

            _logger.LogInformation("Остановка обработки очереди...");
            _isRunning = false;
            _isPaused = true; // Ставим на паузу вместо полной остановки
            
            try
            {
                _processingCts?.Cancel();
                _logger.LogInformation("Queue processor остановлен");
            }
            catch (OperationCanceledException)
            {
                // Игнорируем отмену
            }
        }

        public async Task PauseProcessingAsync()
        {
            if (_isPaused) return;
            
            _logger.LogInformation("Pausing queue processor");
            await _processingLock.WaitAsync();
            _isPaused = true;
            _logger.LogInformation("Queue processor paused");
        }

        public Task ResumeProcessingAsync()
        {
            if (!_isPaused) return Task.CompletedTask;
            
            _logger.LogInformation("Resuming queue processor");
            _isPaused = false;
            _processingLock.Release();
            _logger.LogInformation("Queue processor resumed");
            return Task.CompletedTask;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Queue processor ExecuteAsync запущен. IsPaused={IsPaused}, IsRunning={IsRunning}", _isPaused, _isRunning);
            
            while (!stoppingToken.IsCancellationRequested)
            {
                var lockTaken = false;
                try
                {
                    await _processingLock.WaitAsync(stoppingToken);
                    lockTaken = true;

                    if (_isPaused)
                    {
                        _logger.LogInformation("Queue processor на паузе, ожидание... IsPaused={IsPaused}, IsRunning={IsRunning}", _isPaused, _isRunning);
                        // Освобождаем семафор перед ожиданием, чтобы после снятия паузы цикл мог продолжиться
                        lockTaken = false;
                        _processingLock.Release();
                        await Task.Delay(1000, stoppingToken);
                        continue;
                    }

                    var pendingItems = await _queueRepository.GetPendingItemsAsync();
                    _logger.LogInformation("Найдено элементов в очереди: {Count}", pendingItems.Count);
                    
                    foreach (var item in pendingItems)
                    {
                        _logger.LogInformation("Элемент в очереди: {ItemId} - {FileName} - {Status}", 
                            item.Id, item.FileName, item.Status);
                    }
                    
                    var nextItem = pendingItems.FirstOrDefault();
                    if (nextItem == null)
                    {
                        _logger.LogDebug("Нет элементов для обработки, ожидание...");
                        await Task.Delay(1000, stoppingToken);
                        continue;
                    }

                    _logger.LogInformation("Найден элемент для обработки: {ItemId} - {FileName}", nextItem.Id, nextItem.FileName);
                    await ProcessItemAsync(nextItem, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Queue processing was cancelled");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in queue processing loop");
                    await Task.Delay(5000, stoppingToken);
                }
                finally
                {
                    if (lockTaken)
                    {
                        _processingLock.Release();
                    }
                }
            }

            QueueCompleted?.Invoke(this, EventArgs.Empty);
        }

        public async Task ProcessItemAsync(QueueItem item, CancellationToken cancellationToken = default)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            try
            {
                // Атомарная операция: пытаемся зарезервировать элемент для обработки
                if (!await _queueStore.TryReserveAsync(item.Id, cancellationToken).ConfigureAwait(false))
                {
                    _logger.LogDebug("Item {ItemId} was already reserved or processed, skipping", item.Id);
                    return;
                }

                // Обновляем кэш через IQueueRepository для генерации событий
                item.Status = ConversionStatus.Processing;
                item.StartedAt = DateTime.UtcNow;
                await _queueRepository.UpdateAsync(item);
                
                ItemStarted?.Invoke(this, item);
                _logger.LogInformation("Started processing item {ItemId}", item.Id);

                var progress = new Progress<int>(p => 
                {
                    // Обновляем прогресс в кэше для UI
                    item.Progress = p;
                    _ = Task.Run(async () => await _queueRepository.UpdateAsync(item));
                    
                    // Уведомляем UI
                    ProgressChanged?.Invoke(this, new QueueProgressEventArgs(item, p));
                });

                var result = await _conversionUseCase.ExecuteAsync(item, progress, cancellationToken);

                if (result.Success)
                {
                    // Атомарно завершаем элемент в IQueueStore
                    await _queueStore.CompleteAsync(item.Id, ConversionStatus.Completed, null, result.OutputFileSize, DateTime.UtcNow, cancellationToken)
                        .ConfigureAwait(false);

                    // Обновляем кэш и генерируем событие
                    item.Status = ConversionStatus.Completed;
                    item.CompletedAt = DateTime.UtcNow;
                    item.OutputFileSizeBytes = result.OutputFileSize;
                    await _queueRepository.UpdateAsync(item);

                    _logger.LogInformation("Successfully processed item {ItemId}", item.Id);
                    ItemCompleted?.Invoke(this, item);
                }
                else
                {
                    // Атомарно завершаем элемент с ошибкой
                    await _queueStore.CompleteAsync(item.Id, ConversionStatus.Failed, result.ErrorMessage, null, DateTime.UtcNow, cancellationToken)
                        .ConfigureAwait(false);

                    // Обновляем кэш и генерируем событие
                    item.Status = ConversionStatus.Failed;
                    item.ErrorMessage = result.ErrorMessage;
                    await _queueRepository.UpdateAsync(item);

                    _logger.LogError("Failed to process item {ItemId}: {Error}", item.Id, result.ErrorMessage);
                    ItemFailed?.Invoke(this, item);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Processing of item {ItemId} was cancelled", item.Id);
                throw;
            }
            catch (Exception ex)
            {
                // Завершаем элемент с ошибкой в IQueueStore
                await _queueStore.CompleteAsync(item.Id, ConversionStatus.Failed, ex.Message, null, DateTime.UtcNow, cancellationToken)
                    .ConfigureAwait(false);

                // Обновляем кэш
                item.Status = ConversionStatus.Failed;
                item.ErrorMessage = ex.Message;
                await _queueRepository.UpdateAsync(item);

                _logger.LogError(ex, "Error processing item {ItemId}", item.Id);
                ItemFailed?.Invoke(this, item);
                throw;
            }
        }

        public override void Dispose()
        {
            _processingCts?.Dispose();
            _processingLock?.Dispose();
            base.Dispose();
        }
    }
}