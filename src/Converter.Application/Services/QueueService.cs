using System.Collections.Concurrent;
using System.Collections.Immutable;
using Converter.Domain.Models;
using Microsoft.Extensions.Logging;
using Converter.Application.Abstractions;
using Converter.Application.DTOs;

namespace Converter.Application.Services;

public class QueueService : IQueueService, IDisposable
{
    private readonly ConcurrentDictionary<Guid, QueueItem> _queue = new();
    private readonly ILogger<QueueService> _logger;
    private readonly object _lock = new();
    private bool _isRunning;
    private bool _isPaused;
    private CancellationTokenSource? _cts;
    private bool _disposed;

    public event Action<QueueItem>? ItemChanged;
    public event EventHandler? QueueCompleted;

    public QueueService(ILogger<QueueService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Enqueue(QueueItem item)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));

        if (_queue.TryAdd(item.Id, item))
        {
            _logger.LogInformation("Added item to queue: {ItemId} - {FilePath}", item.Id, item.FilePath);
            OnItemChanged(item);
        }
    }

    public void EnqueueMany(IEnumerable<QueueItem> items)
    {
        if (items == null) throw new ArgumentNullException(nameof(items));

        foreach (var item in items)
        {
            Enqueue(item);
        }
    }

    public async Task StartAsync(CancellationToken ct)
    {
        if (_isRunning) return;

        _logger.LogInformation("Starting queue service");
        _isRunning = true;
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        while (_isRunning && !_cts.Token.IsCancellationRequested)
        {
            try
            {
                if (_isPaused)
                {
                    await Task.Delay(100, _cts.Token);
                    continue;
                }

                var item = GetNextItem();
                if (item == null)
                {
                    await Task.Delay(100, _cts.Token);
                    continue;
                }

                // Mark as processing
                item.Status = ConversionStatus.Processing;
                OnItemChanged(item);

                // Simulate processing
                for (int i = 0; i <= 100; i += 10)
                {
                    if (_cts.Token.IsCancellationRequested)
                        break;

                    item.Progress = i;
                    OnItemChanged(item);
                    await Task.Delay(500, _cts.Token);
                }

                // Mark as completed
                if (!_cts.Token.IsCancellationRequested)
                {
                    item.Status = ConversionStatus.Completed;
                    item.Progress = 100;
                    OnItemChanged(item);
                    _logger.LogInformation("Completed processing item: {ItemId}", item.Id);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Queue processing was cancelled");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing queue item");
            }
        }

        _isRunning = false;
        QueueCompleted?.Invoke(this, EventArgs.Empty);
    }

    public void Pause()
    {
        _isPaused = true;
        _logger.LogInformation("Queue paused");
    }

    public void Resume()
    {
        _isPaused = false;
        _logger.LogInformation("Queue resumed");
    }

    public void Stop()
    {
        _isRunning = false;
        _cts?.Cancel();
        _logger.LogInformation("Queue stopped");
    }

    public IReadOnlyList<QueueItem> GetQueueItems()
    {
        return _queue.Values.OrderBy(x => x.Priority)
                          .ThenBy(x => x.AddedAt)
                          .ToImmutableList();
    }

    public IReadOnlyList<QueueItemDto> GetQueueItemDtos()
    {
        return GetQueueItems().Select(item => new QueueItemDto
        {
            Id = item.Id,
            FilePath = item.FilePath,
            FileSizeBytes = item.FileSizeBytes,
            Duration = item.Duration,
            Progress = item.Progress,
            Status = item.Status.ToString(),
            IsStarred = item.IsStarred,
            Priority = item.Priority,
            OutputPath = item.OutputPath,
            ErrorMessage = item.ErrorMessage,
            AddedAt = item.AddedAt
        }).ToImmutableList();
    }

    private QueueItem? GetNextItem()
    {
        return _queue.Values
            .Where(x => x.Status == ConversionStatus.Pending)
            .OrderBy(x => x.Priority)
            .ThenBy(x => x.AddedAt)
            .FirstOrDefault();
    }

    private void OnItemChanged(QueueItem item)
    {
        ItemChanged?.Invoke(item);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _cts?.Dispose();
            _cts = null;
        }
    }
}