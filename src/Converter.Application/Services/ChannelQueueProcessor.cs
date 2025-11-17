using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Converter.Application.Abstractions;
using Converter.Domain.Models;
using Microsoft.Extensions.Logging;

namespace Converter.Application.Services
{
    public enum QueueCommandType
    {
        ProcessItem
    }

    public sealed class QueueCommand
    {
        public QueueCommandType Type { get; }
        public QueueItem Item { get; }

        public QueueCommand(QueueCommandType type, QueueItem item)
        {
            Type = type;
            Item = item ?? throw new ArgumentNullException(nameof(item));
        }
    }

    public class ChannelQueueProcessor : IQueueProcessor, IAsyncDisposable, IDisposable
    {
        private readonly IQueueRepository _queueRepository;
        private readonly IConversionUseCase _conversionUseCase;
        private readonly ILogger<ChannelQueueProcessor> _logger;
        private readonly Channel<QueueCommand> _commandChannel;
        private readonly SemaphoreSlim _pauseLock = new(1, 1);
        private CancellationTokenSource _processingCts;
        private Task _processingTask;
        private bool _isPaused;
        private bool _isProcessing;

        public event EventHandler<QueueItem> ItemStarted;
        public event EventHandler<QueueItem> ItemCompleted;
        public event EventHandler<QueueItem> ItemFailed;
        public event EventHandler<QueueProgressEventArgs> ProgressChanged;
        public event EventHandler QueueCompleted;

        public bool IsRunning => _processingTask != null && !_processingTask.IsCompleted;
        public bool IsPaused => _isPaused;

        public ChannelQueueProcessor(
            IQueueRepository queueRepository,
            IConversionUseCase conversionUseCase,
            ILogger<ChannelQueueProcessor> logger)
        {
            _queueRepository = queueRepository ?? throw new ArgumentNullException(nameof(queueRepository));
            _conversionUseCase = conversionUseCase ?? throw new ArgumentNullException(nameof(conversionUseCase));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _commandChannel = Channel.CreateBounded<QueueCommand>(new BoundedChannelOptions(1000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            });

            _queueRepository.ItemAdded += OnItemAdded;
            _queueRepository.ItemUpdated += OnItemUpdated;
        }

        public async Task StartProcessingAsync(CancellationToken cancellationToken = default)
        {
            if (_isProcessing)
                return;

            _logger.LogInformation("Starting queue processor");
            _isProcessing = true;

            _processingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = _processingCts.Token;

            _processingTask = ProcessQueueAsync(token);

            var pendingItems = await _queueRepository.GetPendingItemsAsync();
            foreach (var item in pendingItems)
            {
                EnqueueItem(item);
            }

            _logger.LogInformation("Queue processor started");
        }

        public async Task StopProcessingAsync()
        {
            if (!_isProcessing)
                return;

            _logger.LogInformation("Stopping queue processor");

            if (_isPaused)
            {
                _logger.LogDebug("Releasing pause lock during stop");
                _isPaused = false;
                _pauseLock.Release();
            }

            _processingCts?.Cancel();

            if (_processingTask != null)
            {
                try
                {
                    await _processingTask;
                }
                catch (OperationCanceledException)
                {
                    // Expected during shutdown
                }
            }

            _isProcessing = false;
            _logger.LogInformation("Queue processor stopped");
        }

        public async Task PauseProcessingAsync()
        {
            if (_isPaused)
                return;

            _logger.LogInformation("Pausing queue processor");
            await _pauseLock.WaitAsync().ConfigureAwait(false);
            _isPaused = true;
            _logger.LogInformation("Queue processor paused");
        }

        public Task ResumeProcessingAsync()
        {
            if (!_isPaused)
                return Task.CompletedTask;

            _logger.LogInformation("Resuming queue processor");
            _isPaused = false;
            _pauseLock.Release();
            _logger.LogInformation("Queue processor resumed");
            return Task.CompletedTask;
        }

        public async Task ProcessItemAsync(QueueItem item, CancellationToken cancellationToken = default)
        {
            if (item == null) 
                throw new ArgumentNullException(nameof(item));

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
                throw;
            }
            finally
            {
                if (shouldSyncFinalState)
                {
                    await _queueRepository.UpdateAsync(item);
                }
            }
        }

        private async Task ProcessQueueAsync(CancellationToken stoppingToken)
        {
            try
            {
                await foreach (var command in _commandChannel.Reader.ReadAllAsync(stoppingToken))
                {
                    try
                    {
                        await _pauseLock.WaitAsync(stoppingToken).ConfigureAwait(false);
                        try
                        {
                            switch (command.Type)
                            {
                                case QueueCommandType.ProcessItem:
                                    await ProcessItemAsync(command.Item, stoppingToken);
                                    break;
                            }
                        }
                        finally
                        {
                            if (!_isPaused)
                            {
                                _pauseLock.Release();
                            }
                        }
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing queue command");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error in queue processing");
                throw;
            }
            finally
            {
                QueueCompleted?.Invoke(this, EventArgs.Empty);
            }
        }

        private void EnqueueItem(QueueItem item)
        {
            if (item == null)
                return;

            var command = new QueueCommand(QueueCommandType.ProcessItem, item);
            if (!_commandChannel.Writer.TryWrite(command))
            {
                _ = WriteWithBackpressureAsync(command);
            }
        }

        private async Task WriteWithBackpressureAsync(QueueCommand command)
        {
            try
            {
                while (await _commandChannel.Writer.WaitToWriteAsync().ConfigureAwait(false))
                {
                    if (_commandChannel.Writer.TryWrite(command))
                    {
                        return;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Ignore cancellation caused by shutting down the processor.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enqueue command for item {ItemId}", command.Item.Id);
            }

            _logger.LogWarning("Failed to enqueue item {ItemId}: command channel is closed", command.Item.Id);
        }

        private void OnItemAdded(object sender, QueueItem item)
        {
            if (item.Status == ConversionStatus.Pending)
            {
                EnqueueItem(item);
            }
        }

        private void OnItemUpdated(object sender, QueueItem item)
        {
            if (item.Status == ConversionStatus.Pending)
            {
                EnqueueItem(item);
            }
        }

        public async ValueTask DisposeAsync()
        {
            _queueRepository.ItemAdded -= OnItemAdded;
            _queueRepository.ItemUpdated -= OnItemUpdated;

            if (_isPaused)
            {
                _isPaused = false;
                _pauseLock.Release();
            }

            _processingCts?.Cancel();

            if (_processingTask != null)
            {
                try
                {
                    await _processingTask;
                }
                catch (OperationCanceledException)
                {
                    // Expected during shutdown
                }
            }

            _processingCts?.Dispose();
            _pauseLock.Dispose();
        }

        public void Dispose()
        {
            DisposeAsync().AsTask().GetAwaiter().GetResult();
            GC.SuppressFinalize(this);
        }
    }
}
