using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Converter.Domain.Models;
using Converter.Application.Models;

namespace Converter.Services;

public class QueueManager
{
    private readonly ObservableCollection<QueueItem> _queue;
    private readonly object _syncRoot = new();
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _itemTokens = new();
    private readonly Func<QueueItem, IProgress<int>, CancellationToken, Task<ConversionResult>> _conversionHandler;
    private readonly SynchronizationContext? _syncContext;
    private SemaphoreSlim _conversionSemaphore;
    private CancellationTokenSource _cancellationTokenSource;
    private bool _isPaused;
    private bool _isProcessing;
    private bool _stopRequested;
    private int _maxConcurrentConversions = 2;

    public event EventHandler<QueueItem>? ItemAdded;
    public event EventHandler<QueueItem>? ItemRemoved;
    public event EventHandler<QueueItem>? ItemStatusChanged;
    public event EventHandler<QueueItem>? ItemProgressChanged;
    public event EventHandler? QueueCompleted;
    public event EventHandler<string>? ErrorOccurred;

    public int MaxConcurrentConversions
    {
        get => _maxConcurrentConversions;
        set
        {
            var clamped = Math.Max(1, Math.Min(8, value));
            if (_maxConcurrentConversions != clamped)
            {
                _maxConcurrentConversions = clamped;
                if (!_isProcessing)
                {
                    _conversionSemaphore?.Dispose();
                    _conversionSemaphore = new SemaphoreSlim(_maxConcurrentConversions, _maxConcurrentConversions);
                }
            }
        }
    }

    public bool AutoStartNextItem { get; set; } = false;
    public bool StopOnError { get; set; }

    public QueueManager(Func<QueueItem, IProgress<int>, CancellationToken, Task<ConversionResult>>? conversionHandler = null)
    {
        _queue = new ObservableCollection<QueueItem>();
        _conversionSemaphore = new SemaphoreSlim(_maxConcurrentConversions, _maxConcurrentConversions);
        _cancellationTokenSource = new CancellationTokenSource();
        _conversionHandler = conversionHandler ?? SimulateConversionAsync;
        _syncContext = SynchronizationContext.Current;
    }

    private void Post(Action action)
    {
        var ctx = _syncContext;
        if (ctx != null)
        {
            ctx.Post(static s => ((Action)s!).Invoke(), action);
        }
        else
        {
            action();
        }
    }

    public void AddItem(QueueItem item)
    {
        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        lock (_syncRoot)
        {
            if (item.Id == Guid.Empty)
            {
                item.Id = Guid.NewGuid();
            }
            item.AddedAt = DateTime.Now;
            item.Status = ConversionStatus.Pending;
            _queue.Add(item);
        }

        Post(() => ItemAdded?.Invoke(this, item));

        if (AutoStartNextItem && !_isProcessing)
        {
            _ = StartQueueAsync();
        }
    }

    public void AddItems(IEnumerable<QueueItem> items)
    {
        foreach (var item in items)
        {
            AddItem(item);
        }
    }

    public void RemoveItem(Guid itemId)
    {
        QueueItem? removed = null;
        lock (_syncRoot)
        {
            var item = _queue.FirstOrDefault(x => x.Id == itemId);
            if (item != null)
            {
                _queue.Remove(item);
                removed = item;
            }
        }

        if (removed != null)
        {
            if (_itemTokens.TryRemove(removed.Id, out var token))
            {
                token.Cancel();
                token.Dispose();
            }
            Post(() => ItemRemoved?.Invoke(this, removed));
        }
    }

    public void ClearQueue()
    {
        StopQueue();
        List<QueueItem> cleared;
        lock (_syncRoot)
        {
            cleared = _queue.ToList();
            _queue.Clear();
        }

        foreach (var item in cleared)
        {
            Post(() => ItemRemoved?.Invoke(this, item));
        }
    }

    public void ClearCompleted()
    {
        List<QueueItem> completed;
        lock (_syncRoot)
        {
            completed = _queue.Where(x => x.Status == ConversionStatus.Completed).ToList();
            foreach (var item in completed)
            {
                _queue.Remove(item);
            }
        }

        foreach (var item in completed)
        {
            Post(() => ItemRemoved?.Invoke(this, item));
        }
    }

    public void MoveItemUp(Guid itemId)
    {
        lock (_syncRoot)
        {
            var item = _queue.FirstOrDefault(x => x.Id == itemId);
            if (item == null)
            {
                return;
            }

            var index = _queue.IndexOf(item);
            if (index > 0)
            {
                _queue.Move(index, index - 1);
                Post(() => ItemStatusChanged?.Invoke(this, item));
            }
        }
    }

    public void MoveItemDown(Guid itemId)
    {
        lock (_syncRoot)
        {
            var item = _queue.FirstOrDefault(x => x.Id == itemId);
            if (item == null)
            {
                return;
            }

            var index = _queue.IndexOf(item);
            if (index < _queue.Count - 1)
            {
                _queue.Move(index, index + 1);
                Post(() => ItemStatusChanged?.Invoke(this, item));
            }
        }
    }

    public void MoveItemToTop(Guid itemId)
    {
        lock (_syncRoot)
        {
            var item = _queue.FirstOrDefault(x => x.Id == itemId);
            if (item == null)
            {
                return;
            }

            var index = _queue.IndexOf(item);
            if (index > 0)
            {
                _queue.Move(index, 0);
                Post(() => ItemStatusChanged?.Invoke(this, item));
            }
        }
    }

    public void MoveItemToBottom(Guid itemId)
    {
        lock (_syncRoot)
        {
            var item = _queue.FirstOrDefault(x => x.Id == itemId);
            if (item == null)
            {
                return;
            }

            var index = _queue.IndexOf(item);
            if (index < _queue.Count - 1)
            {
                _queue.Move(index, _queue.Count - 1);
                Post(() => ItemStatusChanged?.Invoke(this, item));
            }
        }
    }

    public void SetItemPriority(Guid itemId, int priority)
    {
        QueueItem? target = null;
        lock (_syncRoot)
        {
            target = _queue.FirstOrDefault(x => x.Id == itemId);
            if (target == null)
            {
                return;
            }

            target.Priority = Math.Clamp(priority, 1, 5);
        }

        if (target != null)
        {
            Post(() => ItemStatusChanged?.Invoke(this, target));
            if (AutoStartNextItem)
            {
                SortByPriority();
            }
        }
    }

    public void ToggleStarred(Guid itemId)
    {
        QueueItem? target = null;
        lock (_syncRoot)
        {
            target = _queue.FirstOrDefault(x => x.Id == itemId);
            if (target == null)
            {
                return;
            }

            target.IsStarred = !target.IsStarred;
            if (target.IsStarred)
            {
                var index = _queue.IndexOf(target);
                _queue.Move(index, 0);
            }
        }

        if (target != null)
        {
            Post(() => ItemStatusChanged?.Invoke(this, target));
            SortByPriority();
        }
    }

    public async Task StartQueueAsync()
    {
        if (_isProcessing)
        {
            return;
        }

        _isPaused = false;
        _stopRequested = false;
        _isProcessing = true;
        _cancellationTokenSource = new CancellationTokenSource();
        _conversionSemaphore?.Dispose();
        _conversionSemaphore = new SemaphoreSlim(_maxConcurrentConversions, _maxConcurrentConversions);

        List<QueueItem> pending;
        lock (_syncRoot)
        {
            pending = _queue
                .Where(x => x.Status == ConversionStatus.Pending)
                .OrderBy(x => x.IsStarred ? 0 : 1)
                .ThenBy(x => x.Priority)
                .ThenBy(x => x.AddedAt)
                .ToList();
        }

        if (pending.Count == 0)
        {
            _isProcessing = false;
            return;
        }

        var tasks = pending.Select(item => ProcessItemAsync(item, _cancellationTokenSource.Token)).ToList();

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException)
        {
            // Swallow cancellation to keep UI responsive
        }
        finally
        {
            _isProcessing = false;

            bool hasPending;
            lock (_syncRoot)
            {
                hasPending = _queue.Any(x => x.Status == ConversionStatus.Pending);
            }

            if (!hasPending && !_stopRequested)
            {
                Post(() => QueueCompleted?.Invoke(this, EventArgs.Empty));
            }

            if (AutoStartNextItem && !_stopRequested && hasPending)
            {
                _ = StartQueueAsync();
            }
        }
    }

    private async Task ProcessItemAsync(QueueItem item, CancellationToken ct)
    {
        await _conversionSemaphore.WaitAsync(ct).ConfigureAwait(false);
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _itemTokens[item.Id] = linkedCts;
        try
        {
            while (_isPaused && !linkedCts.IsCancellationRequested)
            {
                await Task.Delay(100, linkedCts.Token).ConfigureAwait(false);
            }

            lock (_syncRoot)
            {
                item.Status = ConversionStatus.Processing;
                item.StartedAt = DateTime.Now;
            }

            ItemStatusChanged?.Invoke(this, item);

            var progress = new Progress<int>(value =>
            {
                lock (_syncRoot)
                {
                    item.Progress = Math.Clamp(value, 0, 100);
                }
                Post(() => ItemProgressChanged?.Invoke(this, item));
            });

        var result = await ConvertVideoAsync(item, progress, linkedCts.Token).ConfigureAwait(false);

        lock (_syncRoot)
        {
            if (result.Success)
            {
                item.Status = ConversionStatus.Completed;
                item.Progress = 100;
                item.OutputFileSizeBytes = result.OutputFileSize;
            }
            else
            {
                item.Status = ConversionStatus.Failed;
                item.ErrorMessage = result.ErrorMessage;
                if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
                {
                    Post(() => ErrorOccurred?.Invoke(this, result.ErrorMessage!));
                }
                if (StopOnError)
                {
                    _stopRequested = true;
                    _cancellationTokenSource.Cancel();
                }
            }

                item.CompletedAt = DateTime.Now;
                // ConversionDuration вычисляется автоматически из StartedAt и CompletedAt
            }

            Post(() => ItemStatusChanged?.Invoke(this, item));
        }
        catch (OperationCanceledException)
        {
            lock (_syncRoot)
            {
                item.Status = ConversionStatus.Cancelled;
            }

            Post(() => ItemStatusChanged?.Invoke(this, item));
        }
        catch (Exception ex)
        {
            lock (_syncRoot)
            {
                item.Status = ConversionStatus.Failed;
                item.ErrorMessage = ex.Message;
            }

            Post(() => ItemStatusChanged?.Invoke(this, item));
            Post(() => ErrorOccurred?.Invoke(this, ex.Message));

            if (StopOnError)
            {
                _stopRequested = true;
                _cancellationTokenSource.Cancel();
            }
        }
        finally
        {
            _itemTokens.TryRemove(item.Id, out _);
            linkedCts.Dispose();
            _conversionSemaphore.Release();
        }
    }

    public void PauseQueue()
    {
        _isPaused = true;

        List<QueueItem> processing;
        lock (_syncRoot)
        {
            processing = _queue.Where(x => x.Status == ConversionStatus.Processing).ToList();
            foreach (var item in processing)
            {
                item.Status = ConversionStatus.Paused;
            }
        }

        foreach (var item in processing)
        {
            Post(() => ItemStatusChanged?.Invoke(this, item));
        }
    }

    public void ResumeQueue()
    {
        _isPaused = false;
        List<QueueItem> paused;
        lock (_syncRoot)
        {
            paused = _queue.Where(x => x.Status == ConversionStatus.Paused).ToList();
            foreach (var item in paused)
            {
                item.Status = ConversionStatus.Processing;
            }
        }

        foreach (var item in paused)
        {
            Post(() => ItemStatusChanged?.Invoke(this, item));
        }
    }

    public void StopQueue()
    {
        _isPaused = false;
        _stopRequested = true;
        if (_isProcessing)
        {
            _cancellationTokenSource.Cancel();
        }

        foreach (var token in _itemTokens.Values)
        {
            token.Cancel();
        }
    }

    public void RetryItem(Guid itemId)
    {
        lock (_syncRoot)
        {
            var item = _queue.FirstOrDefault(x => x.Id == itemId);
            if (item == null)
            {
                return;
            }

            item.Status = ConversionStatus.Pending;
            item.Progress = 0;
            item.ErrorMessage = null;
            item.StartedAt = null;
            item.CompletedAt = null;
            // ConversionDuration очищается автоматически при установке StartedAt в null
            item.OutputFileSizeBytes = null;
            Post(() => ItemStatusChanged?.Invoke(this, item));
        }
    }

    public void CancelItem(Guid itemId)
    {
        if (_itemTokens.TryRemove(itemId, out var token))
        {
            token.Cancel();
            token.Dispose();
        }

        lock (_syncRoot)
        {
            var item = _queue.FirstOrDefault(x => x.Id == itemId);
            if (item == null)
            {
                return;
            }

            item.Status = ConversionStatus.Cancelled;
            item.Progress = 0;
            Post(() => ItemStatusChanged?.Invoke(this, item));
        }
    }

    public IReadOnlyList<QueueItem> GetQueue()
    {
        lock (_syncRoot)
        {
            return _queue.ToList();
        }
    }

    public QueueItem? GetItem(Guid itemId)
    {
        lock (_syncRoot)
        {
            return _queue.FirstOrDefault(x => x.Id == itemId);
        }
    }

    public void UpdateItem(Guid itemId, Action<QueueItem> updater)
    {
        QueueItem? target;
        lock (_syncRoot)
        {
            target = _queue.FirstOrDefault(x => x.Id == itemId);
            if (target == null)
            {
                return;
            }

            updater(target);
        }

        if (target != null)
        {
            Post(() => ItemStatusChanged?.Invoke(this, target));
        }
    }

    public int GetPendingCount() => GetCountByStatus(ConversionStatus.Pending);
    public int GetProcessingCount() => GetCountByStatus(ConversionStatus.Processing);
    public int GetCompletedCount() => GetCountByStatus(ConversionStatus.Completed);
    public int GetFailedCount() => GetCountByStatus(ConversionStatus.Failed);

    private int GetCountByStatus(ConversionStatus status)
    {
        lock (_syncRoot)
        {
            return _queue.Count(x => x.Status == status);
        }
    }

    public QueueStatistics GetStatistics()
    {
        List<QueueItem> snapshot;
        lock (_syncRoot)
        {
            snapshot = _queue.ToList();
        }

        var stats = new QueueStatistics
        {
            TotalItems = snapshot.Count,
            PendingItems = snapshot.Count(x => x.Status == ConversionStatus.Pending),
            ProcessingItems = snapshot.Count(x => x.Status == ConversionStatus.Processing || x.Status == ConversionStatus.Paused),
            CompletedItems = snapshot.Count(x => x.Status == ConversionStatus.Completed),
            FailedItems = snapshot.Count(x => x.Status == ConversionStatus.Failed),
            TotalInputSize = snapshot.Sum(x => Math.Max(0, x.FileSizeBytes)),
            TotalOutputSize = snapshot.Sum(x => Math.Max(0, x.OutputFileSizeBytes ?? 0)),
            TotalProcessingTime = TimeSpan.FromTicks(snapshot.Sum(x => (x.ConversionDuration ?? TimeSpan.Zero).Ticks)),
            EstimatedTimeRemaining = CalculateEstimatedRemaining(snapshot)
        };

        var completed = snapshot.Where(x => x.Status == ConversionStatus.Completed && x.ConversionDuration?.TotalSeconds > 0).ToList();
        if (completed.Any())
        {
            var totalMb = completed.Sum(x => x.FileSizeBytes) / (1024d * 1024d);
            var totalSeconds = completed.Sum(x => x.ConversionDuration!.Value.TotalSeconds);
            if (totalSeconds > 0)
            {
                stats.AverageSpeed = totalMb / totalSeconds;
            }
        }

        return stats;
    }

    private TimeSpan CalculateEstimatedRemaining(IEnumerable<QueueItem> items)
    {
        double seconds = 0;
        foreach (var item in items)
        {
            if (item.Status == ConversionStatus.Processing && item.Progress > 0 && item.ConversionDuration.HasValue && item.ConversionDuration.Value.TotalSeconds > 0)
            {
                var estimatedTotal = item.ConversionDuration.Value.TotalSeconds * 100 / Math.Max(1, item.Progress);
                seconds += Math.Max(0, estimatedTotal - item.ConversionDuration.Value.TotalSeconds);
            }
            else if (item.Status == ConversionStatus.Pending && item.ConversionDuration.HasValue && item.ConversionDuration.Value.TotalSeconds > 0)
            {
                seconds += item.ConversionDuration.Value.TotalSeconds;
            }
        }

        return TimeSpan.FromSeconds(seconds);
    }

    public void SortByPriority()
    {
        ReorderQueue(items => items
            .OrderBy(x => x.IsStarred ? 0 : 1)
            .ThenBy(x => x.Priority)
            .ThenBy(x => x.AddedAt));
    }

    public void SortBySize()
    {
        ReorderQueue(items => items.OrderByDescending(x => x.FileSizeBytes));
    }

    public void SortByDuration()
    {
        ReorderQueue(items => items.OrderByDescending(x => x.ConversionDuration ?? TimeSpan.Zero));
    }

    public void SortByAddedDate()
    {
        ReorderQueue(items => items.OrderBy(x => x.AddedAt));
    }

    private void ReorderQueue(Func<IEnumerable<QueueItem>, IEnumerable<QueueItem>> order)
    {
        lock (_syncRoot)
        {
            var snapshot = order(_queue.ToList()).ToList();
            _queue.Clear();
            foreach (var item in snapshot)
            {
                _queue.Add(item);
            }
        }
    }

    public IEnumerable<QueueItem> FilterByStatus(ConversionStatus status)
    {
        lock (_syncRoot)
        {
            return _queue.Where(x => x.Status == status).ToList();
        }
    }

    public IEnumerable<QueueItem> GetStarredItems()
    {
        lock (_syncRoot)
        {
            return _queue.Where(x => x.IsStarred).OrderBy(x => x.Priority).ToList();
        }
    }

    private Task<ConversionResult> ConvertVideoAsync(QueueItem item, IProgress<int> progress, CancellationToken ct)
    {
        return _conversionHandler(item, progress, ct);
    }

    private static async Task<ConversionResult> SimulateConversionAsync(QueueItem item, IProgress<int> progress, CancellationToken ct)
    {
        var random = new Random();
        for (int i = 1; i <= 100; i += random.Next(5, 15))
        {
            ct.ThrowIfCancellationRequested();
            progress.Report(Math.Min(100, i));
            await Task.Delay(200, ct).ConfigureAwait(false);
        }

        var outputSize = Math.Max(0, item.FileSizeBytes - random.Next(0, (int)Math.Max(1, item.FileSizeBytes / 10)));
        return new ConversionResult
        {
            Success = true,
            OutputFileSize = outputSize
        };
    }
}
