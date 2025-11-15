namespace Converter.Domain.Models;

public enum ConversionStatus
{
    Pending,
    Processing,
    Paused,
    Completed,
    Failed,
    Cancelled
}

public sealed class QueueItem
{
    public Guid Id { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public TimeSpan Duration { get; set; }
    public ConversionStatus Status { get; set; } = ConversionStatus.Pending;
    public int Progress { get; set; }
    public bool IsStarred { get; set; }
    public int Priority { get; set; } = 3;
    public string? OutputPath { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.Now;
}
