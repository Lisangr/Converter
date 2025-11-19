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

            try
            {
                item.Status = ConversionStatus.Processing;
                item.StartedAt = DateTime.UtcNow;
                await _queueRepository.UpdateAsync(item);
                
                ItemStarted?.Invoke(this, item);
                _logger.LogInformation("Started processing item {ItemId}", item.Id);

                var progress = new Progress<int>(p => 
                {
                    // Update the item progress in repository
                    item.Progress = p;
                    _ = Task.Run(async () => await _queueRepository.UpdateAsync(item));
                    
                    // Notify UI
                    ProgressChanged?.Invoke(this, new QueueProgressEventArgs(item, p));
                });

                var result = await _conversionUseCase.ExecuteAsync(item, progress, cancellationToken);

                if (result.Success)
                {
                    item.Status = ConversionStatus.Completed;
                    item.CompletedAt = DateTime.UtcNow;
                    item.OutputFileSizeBytes = result.OutputFileSize;

                    _logger.LogInformation("Successfully processed item {ItemId}", item.Id);
                    ItemCompleted?.Invoke(this, item);
                }
                else
                {
                    item.Status = ConversionStatus.Failed;
                    item.ErrorMessage = result.ErrorMessage;
                    _logger.LogError("Failed to process item {ItemId}: {Error}", item.Id, result.ErrorMessage);
                    ItemFailed?.Invoke(this, item);
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
                item.Status = ConversionStatus.Failed;
                item.ErrorMessage = ex.Message;
                _logger.LogError(ex, "Error processing item {ItemId}", item.Id);
                ItemFailed?.Invoke(this, item);
            }
            finally
            {
                await _queueRepository.UpdateAsync(item);
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