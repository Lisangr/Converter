using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Converter.Application.Abstractions;
using Converter.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Converter.Application.Services
{
    public class ChannelQueueProcessor : IHostedService, IQueueProcessor, IAsyncDisposable, IDisposable
    {
        private readonly IQueueRepository _queueRepository;
        private readonly IConversionUseCase _conversionUseCase;
        private readonly ILogger<ChannelQueueProcessor> _logger;
        private readonly Channel<QueueItem> _queueChannel;
        private readonly SemaphoreSlim _pauseLock = new(1, 1);
        private readonly CancellationTokenSource _stoppingCts = new();
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
            
            _queueChannel = Channel.CreateBounded<QueueItem>(new BoundedChannelOptions(1000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            });
        }

        public async Task StartProcessingAsync(CancellationToken cancellationToken = default)
        {
            if (_isProcessing)
                return;

            _logger.LogInformation("Starting queue processor");
            _isProcessing = true;
            _stoppingCts.Token.ThrowIfCancellationRequested();
            
            _processingTask = ProcessQueueAsync(_stoppingCts.Token);
            
            _ = Task.Run(async () => 
            {
                try
                {
                    await FeedQueueItemsAsync(_stoppingCts.Token);
                }
                catch (OperationCanceledException)
                {
                    // Expected during shutdown
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error feeding queue items");
                }
            }, _stoppingCts.Token);
        }

        public async Task StopProcessingAsync()
        {
            if (!_isProcessing)
                return;

            _logger.LogInformation("Stopping queue processor");
            
            _stoppingCts.Cancel();
            
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
            await _pauseLock.WaitAsync();
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

            try
            {
                item.Status = ConversionStatus.Processing;
                item.StartedAt = DateTime.UtcNow;
                await _queueRepository.UpdateAsync(item);
                
                ItemStarted?.Invoke(this, item);
                _logger.LogInformation("Started processing item {ItemId}", item.Id);

                var progress = new Progress<int>(p => 
                    ProgressChanged?.Invoke(this, new QueueProgressEventArgs(item, p)));

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
                throw;
            }
            finally
            {
                await _queueRepository.UpdateAsync(item);
            }
        }

        private async Task ProcessQueueAsync(CancellationToken stoppingToken)
        {
            try
            {
                await foreach (var item in _queueChannel.Reader.ReadAllAsync(stoppingToken))
                {
                    try
                    {
                        await _pauseLock.WaitAsync(stoppingToken);
                        try
                        {
                            if (_isPaused)
                            {
                                await _queueChannel.Writer.WriteAsync(item, stoppingToken);
                                await Task.Delay(1000, stoppingToken);
                                continue;
                            }

                            await ProcessItemAsync(item, stoppingToken);
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
                        _logger.LogError(ex, "Error processing queue item");
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

        private async Task FeedQueueItemsAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var items = await _queueRepository.GetPendingItemsAsync();
                    foreach (var item in items)
                    {
                        if (stoppingToken.IsCancellationRequested)
                            break;

                        await _queueChannel.Writer.WriteAsync(item, stoppingToken);
                    }

                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await StartProcessingAsync(cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await StopProcessingAsync();
            _stoppingCts.Cancel();
        }

        public async ValueTask DisposeAsync()
        {
            _stoppingCts.Cancel();
            
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

            _stoppingCts.Dispose();
            _pauseLock.Dispose();
        }

        public void Dispose()
        {
            DisposeAsync().AsTask().GetAwaiter().GetResult();
            GC.SuppressFinalize(this);
        }
    }
}
