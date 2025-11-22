using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Converter.Application.Abstractions;
using Converter.Application.Services;
using Converter.Domain.Models;
using Microsoft.Extensions.Logging;

namespace Converter.Infrastructure
{
    public class ChannelQueueProcessor : IQueueProcessor, IAsyncDisposable, IDisposable
    {
        private readonly IQueueStore _queueStore;
        private readonly IConversionUseCase _conversionUseCase;
        private readonly ILogger<ChannelQueueProcessor> _logger;
        private readonly Channel<QueueItem> _itemChannel;
        private readonly AsyncAutoResetEvent _pauseEvent = new();
        private readonly IQueueRepository _queueRepository;
        private readonly IUiDispatcher _uiDispatcher;
        private CancellationTokenSource? _processingCts;
        private Task? _processingTask;
        private bool _isProcessing;
        private bool _disposed;

        public event EventHandler<QueueItem>? ItemStarted;
        public event EventHandler<QueueItem>? ItemCompleted;
        public event EventHandler<QueueItem>? ItemFailed;
        public event EventHandler<QueueProgressEventArgs>? ProgressChanged;
        public event EventHandler? QueueCompleted;
        public event EventHandler<QueueItem>? ItemUpdated;

        public async Task RemoveItemAsync(QueueItem item, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                return;

            try
            {
                // Remove from persistent storage
                await _queueRepository.RemoveAsync(item.Id).ConfigureAwait(false);
                _logger.LogInformation("Item {ItemId} removed from repository.", item.Id);

                // Signal UI for removal (or cancellation if it was in progress)
                // We can't directly remove from the channel, so we'll mark it and let the processor skip it if running.
                item.Status = ConversionStatus.Cancelled; // Mark as cancelled/removed
                item.ErrorMessage = "Removed by user";
                _ = _uiDispatcher.InvokeAsync(async () => ItemUpdated?.Invoke(this, item));
                _logger.LogInformation("Signaled UI to remove item {ItemId}", item.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing item {ItemId} from queue processor", item.Id);
                throw; // Re-throw to indicate failure
            }
        }

        public bool IsRunning => _isProcessing && !_itemChannel.Reader.Completion.IsCompleted;
        public bool IsPaused => _pauseEvent.IsPaused;

        public ChannelQueueProcessor(
            IQueueRepository queueRepository,
            IQueueStore queueStore,
            IConversionUseCase conversionUseCase,
            ILogger<ChannelQueueProcessor> logger,
            IUiDispatcher uiDispatcher)
        {
            _queueRepository = queueRepository ?? throw new ArgumentNullException(nameof(queueRepository));
            _queueStore = queueStore ?? throw new ArgumentNullException(nameof(queueStore));
            _conversionUseCase = conversionUseCase ?? throw new ArgumentNullException(nameof(conversionUseCase));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _uiDispatcher = uiDispatcher ?? throw new ArgumentNullException(nameof(uiDispatcher));

            _itemChannel = Channel.CreateBounded<QueueItem>(new BoundedChannelOptions(1000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            });

            _queueRepository.ItemAdded += OnItemAdded;
        }

        public async Task StartProcessingAsync(CancellationToken cancellationToken = default)
        {
            if (_isProcessing)
                return;

            _logger.LogInformation("Starting queue processor");
            _isProcessing = true;

            try
            {
                _processingCts?.Dispose();
                _processingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var token = _processingCts.Token;

                var pendingItems = await _queueRepository.GetPendingItemsAsync().ConfigureAwait(false);
                foreach (var item in pendingItems)
                {
                    token.ThrowIfCancellationRequested();
                    await WriteItemToChannelAsync(item, token).ConfigureAwait(false);
                }

                _processingTask = Task.Run(async () =>
                {
                    await foreach (var item in _itemChannel.Reader.ReadAllAsync(token).ConfigureAwait(false))
                    {
                        if (token.IsCancellationRequested) break;
                        await _pauseEvent.WaitAsync(token).ConfigureAwait(false);
                        if (token.IsCancellationRequested) break;

                        await ProcessItemAsync(item, token).ConfigureAwait(false);
                    }
                    _logger.LogInformation("Queue processing loop finished.");
                    QueueCompleted?.Invoke(this, EventArgs.Empty);
                }, CancellationToken.None);

                _logger.LogInformation("Queue processor started");
            }
            catch (OperationCanceledException)
            {
                await StopProcessingAsync().ConfigureAwait(false);
                throw;
            }
        }

        public async Task StopProcessingAsync()
        {
            if (_disposed) return;

            try
            {
                _logger.LogInformation("Stopping queue processor...");

                _processingCts?.Cancel();

                if (_processingTask != null && !_processingTask.IsCompleted)
                {
                    try
                    {
                        await _processingTask.ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during queue processing task completion");
                    }
                }

                while (_itemChannel.Reader.TryRead(out var item))
                {
                    try
                    {
                        if (item.Status != ConversionStatus.Completed && item.Status != ConversionStatus.Failed)
                        {
                            item.Status = ConversionStatus.Cancelled;
                            _ = _uiDispatcher.InvokeAsync(async () => ItemUpdated?.Invoke(this, item));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error cleaning up queue item from channel");
                    }
                }

                _isProcessing = false;
                _logger.LogInformation("Queue processor stopped");
                QueueCompleted?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping queue processor");
                throw;
            }
        }

        public Task PauseProcessingAsync()
        {
            if (!IsRunning || IsPaused)
                return Task.CompletedTask;

            _logger.LogInformation("Pausing queue processor");
            _pauseEvent.Reset();
            _logger.LogInformation("Queue processor paused");
            return Task.CompletedTask;
        }

        public Task ResumeProcessingAsync()
        {
            if (!IsRunning || !IsPaused)
                return Task.CompletedTask;

            _logger.LogInformation("Resuming queue processor");
            _pauseEvent.Set();
            _logger.LogInformation("Queue processor resumed");
            return Task.CompletedTask;
        }

        public async Task ProcessItemAsync(QueueItem item, CancellationToken cancellationToken = default)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            try
            {
                if (!await _queueStore.TryReserveAsync(item.Id, cancellationToken).ConfigureAwait(false))
                {
                    _logger.LogDebug("Item {ItemId} was already reserved or processed, skipping", item.Id);
                    return;
                }

                item.Status = ConversionStatus.Processing;
                item.StartedAt = DateTime.UtcNow;
                await _queueRepository.UpdateAsync(item).ConfigureAwait(false);

                _ = _uiDispatcher.InvokeAsync(async () => ItemStarted?.Invoke(this, item));
                _logger.LogInformation("Started processing item {ItemId}", item.Id);

                var progress = new Progress<int>(p =>
                {
                    item.Progress = p;
                    _ = _queueRepository.UpdateAsync(item).ContinueWith(t =>
                    {
                        if (t.IsFaulted) _logger.LogError(t.Exception, "Error updating queue item progress in repository");
                    }, TaskScheduler.Default);

                    _ = _uiDispatcher.InvokeAsync(async () => ProgressChanged?.Invoke(this, new QueueProgressEventArgs(item, p)));
                });

                var result = await _conversionUseCase.ExecuteAsync(item, progress, cancellationToken).ConfigureAwait(false);

                if (result.Success)
                {
                    await _queueStore
                        .CompleteAsync(item.Id, ConversionStatus.Completed, null, result.OutputFileSize, DateTime.UtcNow, cancellationToken)
                        .ConfigureAwait(false);

                    item.Status = ConversionStatus.Completed;
                    item.CompletedAt = DateTime.UtcNow;
                    item.OutputFileSizeBytes = result.OutputFileSize;
                    await _queueRepository.UpdateAsync(item).ConfigureAwait(false);

                    _logger.LogInformation("Successfully processed item {ItemId}", item.Id);
                    _ = _uiDispatcher.InvokeAsync(async () => ItemCompleted?.Invoke(this, item));
                }
                else
                {
                    await _queueStore
                        .CompleteAsync(item.Id, ConversionStatus.Failed, result.ErrorMessage, null, DateTime.UtcNow, cancellationToken)
                        .ConfigureAwait(false);

                    item.Status = ConversionStatus.Failed;
                    item.ErrorMessage = result.ErrorMessage;
                    await _queueRepository.UpdateAsync(item).ConfigureAwait(false);

                    _logger.LogError("Failed to process item {ItemId}: {Error}", item.Id, result.ErrorMessage);
                    _ = _uiDispatcher.InvokeAsync(async () => ItemFailed?.Invoke(this, item));
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Processing of item {ItemId} was cancelled", item.Id);
                throw;
            }
            catch (Exception ex)
            {
                await _queueStore
                    .CompleteAsync(item.Id, ConversionStatus.Failed, ex.Message, null, DateTime.UtcNow, cancellationToken)
                    .ConfigureAwait(false);

                item.Status = ConversionStatus.Failed;
                item.ErrorMessage = ex.Message;
                await _queueRepository.UpdateAsync(item).ConfigureAwait(false);

                _logger.LogError(ex, "Error processing item {ItemId}", item.Id);
                _ = _uiDispatcher.InvokeAsync(async () => ItemFailed?.Invoke(this, item));
                throw;
            }
        }

        public async Task EnqueueAsync(QueueItem item, CancellationToken cancellationToken = default)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            await WriteItemToChannelAsync(item, cancellationToken).ConfigureAwait(false);
        }

        public IAsyncEnumerable<QueueItem> GetItemsAsync(CancellationToken cancellationToken = default)
        {
            return _itemChannel.Reader.ReadAllAsync(cancellationToken);
        }

        private async Task WriteItemToChannelAsync(QueueItem item, CancellationToken cancellationToken = default)
        {
            if (item == null || _disposed)
                return;

            try
            {
                while (await _itemChannel.Writer.WaitToWriteAsync(cancellationToken).ConfigureAwait(false))
                {
                    try
                    {
                        await _itemChannel.Writer.WriteAsync(item, cancellationToken).ConfigureAwait(false);
                        return;
                    }
                    catch (ChannelClosedException)
                    {
                        _logger.LogWarning("Channel was closed while trying to write item {ItemId}", item.Id);
                        return;
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogInformation("Writing item {ItemId} was cancelled", item.Id);
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error writing item {ItemId} to channel", item.Id);
                        await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                    }
                }

                _logger.LogWarning("Failed to enqueue item {ItemId}: channel is closed", item.Id);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while writing item {ItemId} to channel", item.Id);
            }
        }

        private void OnItemAdded(object? sender, QueueItem item)
        {
            if (item == null || _disposed) return;

            if (_isProcessing && item.Status == ConversionStatus.Pending)
            {
                _ = WriteItemToChannelAsync(item, _processingCts?.Token ?? CancellationToken.None);
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            _queueRepository.ItemAdded -= OnItemAdded;
            await StopProcessingAsync().ConfigureAwait(false);

            _processingCts?.Dispose();
            _pauseEvent?.Dispose();
        }

        public void Dispose()
        {
            DisposeAsync().AsTask().GetAwaiter().GetResult();
            GC.SuppressFinalize(this);
        }
    }

    
}
