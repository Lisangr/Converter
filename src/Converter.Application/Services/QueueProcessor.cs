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
        private readonly IConversionUseCase _conversionUseCase;
        private readonly ILogger<QueueProcessor> _logger;
        private readonly SemaphoreSlim _processingLock = new(1, 1);
        private CancellationTokenSource _processingCts;
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
            IConversionUseCase conversionUseCase,
            ILogger<QueueProcessor> logger)
        {
            _queueRepository = queueRepository ?? throw new ArgumentNullException(nameof(queueRepository));
            _conversionUseCase = conversionUseCase ?? throw new ArgumentNullException(nameof(conversionUseCase));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task StartProcessingAsync(CancellationToken cancellationToken = default)
        {
            if (_isRunning) return;

            _logger.LogInformation("Starting queue processor");
            _processingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _isRunning = true;
            _isPaused = false;
            
            await base.StartAsync(cancellationToken);
            _logger.LogInformation("Queue processor started");
        }

        public new async Task StopProcessingAsync()
        {
            if (!_isRunning) return;

            _logger.LogInformation("Stopping queue processor");
            _processingCts?.Cancel();
            await base.StopAsync(default);
            _isRunning = false;
            _logger.LogInformation("Queue processor stopped");
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
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _processingLock.WaitAsync(stoppingToken);

                    if (_isPaused)
                    {
                        await Task.Delay(1000, stoppingToken);
                        continue;
                    }

                    var nextItem = (await _queueRepository.GetPendingItemsAsync()).FirstOrDefault();
                    if (nextItem == null)
                    {
                        await Task.Delay(1000, stoppingToken);
                        continue;
                    }

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
                    if (!_isPaused)
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

            var shouldSyncFinalState = true;

            try
            {
                if (!await _queueRepository.TryReserveAsync(item, cancellationToken).ConfigureAwait(false))
                {
                    shouldSyncFinalState = false;
                    _logger.LogDebug("Item {ItemId} was already reserved or processed, skipping", item.Id);
                    return;
                }

                ItemStarted?.Invoke(this, item);
                _logger.LogInformation("Started processing item {ItemId}", item.Id);

                var progress = new Progress<int>(p =>
                    ProgressChanged?.Invoke(this, new QueueProgressEventArgs(item, p)));

                var result = await _conversionUseCase.ExecuteAsync(item, progress, cancellationToken);

                if (result.Success)
                {
                    await _queueRepository.CompleteAsync(
                        item,
                        ConversionStatus.Completed,
                        null,
                        result.OutputFileSize,
                        DateTime.UtcNow,
                        cancellationToken).ConfigureAwait(false);

                    _logger.LogInformation("Successfully processed item {ItemId}", item.Id);
                    ItemCompleted?.Invoke(this, item);
                    shouldSyncFinalState = false;
                }
                else
                {
                    await _queueRepository.CompleteAsync(
                        item,
                        ConversionStatus.Failed,
                        result.ErrorMessage,
                        null,
                        DateTime.UtcNow,
                        cancellationToken).ConfigureAwait(false);

                    _logger.LogError("Failed to process item {ItemId}: {Error}", item.Id, result.ErrorMessage);
                    ItemFailed?.Invoke(this, item);
                    shouldSyncFinalState = false;
                }
            }
            catch (OperationCanceledException)
            {
                item.Status = ConversionStatus.Pending;
                _logger.LogInformation("Processing of item {ItemId} was cancelled", item.Id);
                throw;
            }
            catch (Exception ex)
            {
                await _queueRepository.CompleteAsync(
                    item,
                    ConversionStatus.Failed,
                    ex.Message,
                    null,
                    DateTime.UtcNow,
                    cancellationToken).ConfigureAwait(false);

                _logger.LogError(ex, "Error processing item {ItemId}", item.Id);
                ItemFailed?.Invoke(this, item);
                shouldSyncFinalState = false;
            }
            finally
            {
                if (shouldSyncFinalState)
                {
                    await _queueRepository.UpdateAsync(item).ConfigureAwait(false);
                }
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