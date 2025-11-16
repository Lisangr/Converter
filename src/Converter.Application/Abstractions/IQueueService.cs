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
    event EventHandler<QueueItemSnapshot> ItemChanged;
    event EventHandler QueueCompleted;
    IReadOnlyList<QueueItemSnapshot> Snapshot();
}

public sealed class QueueItemModel
{
    public Guid Id { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public TimeSpan Duration { get; set; }
    public int Progress { get; set; }
    public string Status { get; set; } = QueueItemStatuses.Pending;
    public bool IsStarred { get; set; }
    public int Priority { get; set; } = 3;
    public string? OutputPath { get; set; }
    public string? ErrorMessage { get; set; }
    public ConversionProfile Profile { get; set; } = default!;
    public DateTime EnqueuedAtUtc { get; set; } = DateTime.UtcNow;

    public QueueItemSnapshot ToSnapshot() => new(
        Id,
        FilePath,
        FileSizeBytes,
        Duration,
        Progress,
        Status,
        IsStarred,
        Priority,
        OutputPath,
        ErrorMessage,
        Profile,
        EnqueuedAtUtc);
}

public sealed record QueueItemSnapshot(
    Guid Id,
    string FilePath,
    long FileSizeBytes,
    TimeSpan Duration,
    int Progress,
    string Status,
    bool IsStarred,
    int Priority,
    string? OutputPath,
    string? ErrorMessage,
    ConversionProfile Profile,
    DateTime EnqueuedAtUtc);

public static class QueueItemStatuses
{
    public const string Pending = "Pending";
    public const string Processing = "Processing";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
    public const string Cancelled = "Cancelled";
}
