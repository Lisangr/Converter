using System.Threading;
using System.Threading.Tasks;

namespace Converter.Application.Abstractions;

public interface IQueueService
{
    void Enqueue(QueueItemModel item);
    void EnqueueMany(IEnumerable<QueueItemModel> items);
    Task StartAsync(CancellationToken ct);
    void Pause();
    void Resume();
    void Stop();
    event EventHandler<QueueItemModel> ItemChanged;
    event EventHandler QueueCompleted;
    IReadOnlyList<QueueItemModel> Snapshot();
}

public sealed class QueueItemModel
{
    public Guid Id { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public TimeSpan Duration { get; set; }
    public int Progress { get; set; }
    public string Status { get; set; } = "Pending";
    public bool IsStarred { get; set; }
    public int Priority { get; set; } = 3;
    public string? OutputPath { get; set; }
    public string? ErrorMessage { get; set; }
}
