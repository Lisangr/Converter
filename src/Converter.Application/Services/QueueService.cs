using Converter.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Converter.Application.Services;

public sealed class QueueService : IQueueService
{
    private readonly IConversionOrchestrator _orchestrator;
    private readonly List<QueueItemModel> _items = new();
    private readonly object _sync = new();
    private bool _isRunning;
    private bool _stopRequested;
    private readonly ILogger<QueueService> _logger;

    public event EventHandler<QueueItemModel>? ItemChanged;
    public event EventHandler? QueueCompleted;

    public QueueService(IConversionOrchestrator orchestrator, ILogger<QueueService> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    public void Enqueue(QueueItemModel item)
    {
        lock (_sync)
        {
            if (item.Id == Guid.Empty) item.Id = Guid.NewGuid();
            item.Status = "Pending";
            _items.Add(item);
        }
        _logger.LogInformation("Enqueued item {Id}: {Path}", item.Id, item.FilePath);
        ItemChanged?.Invoke(this, item);
    }

    public void EnqueueMany(IEnumerable<QueueItemModel> items)
    {
        foreach (var i in items) Enqueue(i);
    }

    public IReadOnlyList<QueueItemModel> Snapshot()
    {
        lock (_sync) return _items.ToList();
    }

    public void Pause() { /* optional in v1 */ }
    public void Resume() { /* optional in v1 */ }

    public void Stop()
    {
        _stopRequested = true;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        if (_isRunning) return;
        _isRunning = true;
        try
        {
            _logger.LogInformation("Queue started");
            while (true)
            {
                QueueItemModel? next;
                lock (_sync)
                {
                    next = _items.FirstOrDefault(x => x.Status == "Pending");
                }
                if (next == null || _stopRequested) break;

                Update(next, i => i.Status = "Processing");
                _logger.LogInformation("Processing item {Id}: {Path}", next.Id, next.FilePath);

                var progress = new Progress<int>(p => Update(next!, i => i.Progress = p));
                var outputPath = next.OutputPath ?? Path.ChangeExtension(next.FilePath, ".mp4");
                var profile = new ConversionProfile("mp4", "libx264", "aac", "192k", 23);
                var request = new ConversionRequest(next.FilePath, outputPath, profile);

                ConversionOutcome outcome;
                try
                {
                    outcome = await _orchestrator.ConvertAsync(request, progress, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
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
                    Update(next, i => { i.Status = "Completed"; i.Progress = 100; i.OutputPath = outputPath; });
                    _logger.LogInformation("Item {Id} completed", next.Id);
                }
                else
                {
                    Update(next, i => { i.Status = "Failed"; i.ErrorMessage = outcome.ErrorMessage; });
                    _logger.LogWarning("Item {Id} failed: {Error}", next.Id, outcome.ErrorMessage);
                }
            }
        }
        finally
        {
            _isRunning = false;
            _logger.LogInformation("Queue completed");
            QueueCompleted?.Invoke(this, EventArgs.Empty);
        }
    }

    private void Update(QueueItemModel item, Action<QueueItemModel> mutate)
    {
        lock (_sync)
        {
            mutate(item);
        }
        ItemChanged?.Invoke(this, item);
    }
}
