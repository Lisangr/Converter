using System;
using System.IO;
using Converter.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Converter.Application.Services;

public sealed class QueueService : IQueueService
{
    private readonly IConversionQueue _queue;
    private readonly ISchedulingStrategy _schedulingStrategy;
    private readonly IProgressReporter _progressReporter;
    private readonly IConversionOrchestrator _orchestrator;
    private readonly ILogger<QueueService> _logger;
    private bool _isRunning;
    private bool _stopRequested;

    public event EventHandler<QueueItemSnapshot>? ItemChanged
    {
        add => _queue.ItemChanged += value;
        remove => _queue.ItemChanged -= value;
    }

    public event EventHandler? QueueCompleted;

    public QueueService(
        IConversionQueue queue,
        ISchedulingStrategy schedulingStrategy,
        IProgressReporter progressReporter,
        IConversionOrchestrator orchestrator,
        ILogger<QueueService> logger)
    {
        _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        _schedulingStrategy = schedulingStrategy ?? throw new ArgumentNullException(nameof(schedulingStrategy));
        _progressReporter = progressReporter ?? throw new ArgumentNullException(nameof(progressReporter));
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Enqueue(QueueItemModel item)
    {
        ValidateQueueItem(item);
        _queue.Enqueue(item);
        _logger.LogInformation("Enqueued item {Id}: {Path}", item.Id, item.FilePath);
    }

    public void EnqueueMany(IEnumerable<QueueItemModel> items)
    {
        if (items is null) throw new ArgumentNullException(nameof(items));
        foreach (var item in items)
        {
            Enqueue(item);
        }
    }

    public IReadOnlyList<QueueItemSnapshot> Snapshot() => _queue.Snapshot();

    public void Pause()
    {
        // Reserved for future versions
    }

    public void Resume()
    {
        // Reserved for future versions
    }

    public void Stop()
    {
        _stopRequested = true;
        _logger.LogInformation("Stop requested for conversion queue");
    }

    public async Task StartAsync(CancellationToken ct)
    {
        if (_isRunning)
        {
            _logger.LogDebug("Queue already running");
            return;
        }

        _isRunning = true;
        _stopRequested = false;
        try
        {
            _logger.LogInformation("Queue started");
            while (!_stopRequested)
            {
                ct.ThrowIfCancellationRequested();
                var next = _queue.TryMarkNextProcessing(_schedulingStrategy);
                if (next == null)
                {
                    break;
                }

                _logger.LogInformation("Processing item {Id}: {Path}", next.Id, next.FilePath);
                var outputPath = EnsureOutputPath(next);
                var progress = _progressReporter.Create(next.Id, (id, percent) =>
                    _queue.Update(id, item => item.Progress = percent));

                ConversionOutcome outcome;
                try
                {
                    var request = new ConversionRequest(next.FilePath, outputPath, next.Profile);
                    outcome = await _orchestrator.ConvertAsync(request, progress, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    _queue.Update(next.Id, item =>
                    {
                        item.Status = QueueItemStatuses.Cancelled;
                        item.ErrorMessage = null;
                    });
                    _logger.LogWarning("Item {Id} canceled", next.Id);
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Item {Id} failed with unhandled exception", next.Id);
                    outcome = new ConversionOutcome(false, null, ex.Message);
                }

                if (outcome.Success)
                {
                    _queue.Update(next.Id, item =>
                    {
                        item.Status = QueueItemStatuses.Completed;
                        item.Progress = 100;
                        item.OutputPath = outputPath;
                        item.ErrorMessage = null;
                    });
                    _logger.LogInformation("Item {Id} completed", next.Id);
                }
                else
                {
                    _queue.Update(next.Id, item =>
                    {
                        item.Status = QueueItemStatuses.Failed;
                        item.ErrorMessage = outcome.ErrorMessage;
                    });
                    _logger.LogWarning("Item {Id} failed: {Error}", next.Id, outcome.ErrorMessage);
                }
            }
        }
        finally
        {
            _isRunning = false;
            _stopRequested = false;
            _logger.LogInformation("Queue completed");
            QueueCompleted?.Invoke(this, EventArgs.Empty);
        }
    }

    private static void ValidateQueueItem(QueueItemModel item)
    {
        if (item is null) throw new ArgumentNullException(nameof(item));
        if (item.Profile is null)
        {
            throw new ArgumentException("Conversion profile is required", nameof(item));
        }
        if (string.IsNullOrWhiteSpace(item.FilePath))
        {
            throw new ArgumentException("File path is required", nameof(item));
        }
    }

    private static string EnsureOutputPath(QueueItemSnapshot item)
    {
        if (!string.IsNullOrWhiteSpace(item.OutputPath))
        {
            return item.OutputPath;
        }

        var extension = Path.GetExtension(item.FilePath);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".mp4";
        }

        var directory = Path.GetDirectoryName(item.FilePath) ?? Environment.CurrentDirectory;
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(item.FilePath);
        return Path.Combine(directory, fileNameWithoutExtension + extension);
    }
}
