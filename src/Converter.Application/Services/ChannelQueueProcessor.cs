using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Converter.Application.Abstractions;
using Converter.Domain.Models;
using Microsoft.Extensions.Logging;

namespace Converter.Application.Services
{
    public class ChannelQueueProcessor : IQueueProcessor, IAsyncDisposable, IDisposable
    {
        private readonly IQueueStore _queueStore;
        private readonly IConversionUseCase _conversionUseCase;
        private readonly ILogger<ChannelQueueProcessor> _logger;
        private readonly Channel<QueueItem> _itemChannel;
        private readonly AsyncAutoResetEvent _pauseEvent = new();
        private readonly IQueueRepository _queueRepository;
        private CancellationTokenSource? _processingCts;
        private bool _isProcessing;
        private bool _disposed;

        public event EventHandler<QueueItem> ItemStarted;
        public event EventHandler<QueueItem> ItemCompleted;
        public event EventHandler<QueueItem> ItemFailed;
        public event EventHandler<QueueProgressEventArgs> ProgressChanged;
        public event EventHandler QueueCompleted;

        public bool IsRunning => _isProcessing && !_itemChannel.Reader.Completion.IsCompleted;
        public bool IsPaused => _pauseEvent.IsPaused;

        public ChannelQueueProcessor(
            IQueueRepository queueRepository,
            IQueueStore queueStore,
            IConversionUseCase conversionUseCase,
            ILogger<ChannelQueueProcessor> logger)
        {
            _queueRepository = queueRepository ?? throw new ArgumentNullException(nameof(queueRepository));
            _queueStore = queueStore ?? throw new ArgumentNullException(nameof(queueStore));
            _conversionUseCase = conversionUseCase ?? throw new ArgumentNullException(nameof(conversionUseCase));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _itemChannel = Channel.CreateBounded<QueueItem>(new BoundedChannelOptions(1000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            });

            // Подписываемся на события только для получения уведомлений о новых элементах
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
                // Load existing items from the queue
                _processingCts?.Dispose();
                _processingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var token = _processingCts.Token;

                var pendingItems = await _queueRepository.GetPendingItemsAsync().ConfigureAwait(false);
                foreach (var item in pendingItems)
                {
                    token.ThrowIfCancellationRequested();
                    await WriteItemToChannelAsync(item, token).ConfigureAwait(false);
                }

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
            if (!_isProcessing)
                return;

            _logger.LogInformation("Stopping queue processor");

            // Cancel the current processing и закрыть канал
            _processingCts?.Cancel();
            _itemChannel.Writer.TryComplete();

            _isProcessing = false;
            _logger.LogInformation("Queue processor stopped");
        }

        public async Task PauseProcessingAsync()
        {
            if (!IsRunning || IsPaused)
                return;

            _logger.LogInformation("Pausing queue processor");
            _pauseEvent.Reset();
            _logger.LogInformation("Queue processor paused");
        }

        public async Task ResumeProcessingAsync()
        {
            if (!IsRunning || !IsPaused)
                return;

            _logger.LogInformation("Resuming queue processor");
            _pauseEvent.Set();
            _logger.LogInformation("Queue processor resumed");
            await Task.CompletedTask;
        }

        public async Task ProcessItemAsync(QueueItem item, CancellationToken cancellationToken = default)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

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
                await _queueRepository.UpdateAsync(item).ConfigureAwait(false);

                ItemStarted?.Invoke(this, item);
                _logger.LogInformation("Started processing item {ItemId}", item.Id);

                var progress = new Progress<int>(p =>
                {
                    // Обновляем прогресс в кэше для UI
                    item.Progress = p;
                    _ = Task.Run(async () => await _queueRepository.UpdateAsync(item).ConfigureAwait(false));

                    // Уведомляем UI
                    ProgressChanged?.Invoke(this, new QueueProgressEventArgs(item, p));
                });

                var result = await _conversionUseCase.ExecuteAsync(item, progress, cancellationToken).ConfigureAwait(false);

                if (result.Success)
                {
                    // Атомарно завершаем элемент в IQueueStore
                    await _queueStore
                        .CompleteAsync(item.Id, ConversionStatus.Completed, null, result.OutputFileSize, DateTime.UtcNow, cancellationToken)
                        .ConfigureAwait(false);

                    // Обновляем кэш и генерируем событие
                    item.Status = ConversionStatus.Completed;
                    item.CompletedAt = DateTime.UtcNow;
                    item.OutputFileSizeBytes = result.OutputFileSize;
                    await _queueRepository.UpdateAsync(item).ConfigureAwait(false);

                    _logger.LogInformation("Successfully processed item {ItemId}", item.Id);
                    ItemCompleted?.Invoke(this, item);
                }
                else
                {
                    // Атомарно завершаем элемент с ошибкой
                    await _queueStore
                        .CompleteAsync(item.Id, ConversionStatus.Failed, result.ErrorMessage, null, DateTime.UtcNow, cancellationToken)
                        .ConfigureAwait(false);

                    // Обновляем кэш и генерируем событие
                    item.Status = ConversionStatus.Failed;
                    item.ErrorMessage = result.ErrorMessage;
                    await _queueRepository.UpdateAsync(item).ConfigureAwait(false);

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
                await _queueStore
                    .CompleteAsync(item.Id, ConversionStatus.Failed, ex.Message, null, DateTime.UtcNow, cancellationToken)
                    .ConfigureAwait(false);

                // Обновляем кэш
                item.Status = ConversionStatus.Failed;
                item.ErrorMessage = ex.Message;
                await _queueRepository.UpdateAsync(item).ConfigureAwait(false);

                _logger.LogError(ex, "Error processing item {ItemId}", item.Id);
                ItemFailed?.Invoke(this, item);
                throw;
            }
        }

        public async Task EnqueueAsync(QueueItem item, CancellationToken cancellationToken = default)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            await WriteItemToChannelAsync(item, cancellationToken).ConfigureAwait(false);
        }

        public async IAsyncEnumerable<QueueItem> GetItemsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await foreach (var item in _itemChannel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
                {
                    // Учитываем паузу на стороне процессора
                    while (IsPaused)
                    {
                        await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                    }

                    yield return item;
                }
            }
            finally
            {
                QueueCompleted?.Invoke(this, EventArgs.Empty);
            }
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
                        // Use WriteAsync instead of TryWrite for better backpressure handling
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
                        // Add a small delay before retry to prevent tight loop on error
                        await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                    }
                }

                _logger.LogWarning("Failed to enqueue item {ItemId}: channel is closed", item.Id);
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while writing item {ItemId} to channel", item.Id);
            }
        }

        private void OnItemAdded(object? sender, QueueItem item)
        {
            if (item?.Status == ConversionStatus.Pending)
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

            _processingCts?.Cancel();
            _processingCts?.Dispose();

            _itemChannel.Writer.TryComplete();
            _pauseEvent?.Dispose();
        }

        public void Dispose()
        {
            DisposeAsync().AsTask().GetAwaiter().GetResult();
            GC.SuppressFinalize(this);
        }
    }
}