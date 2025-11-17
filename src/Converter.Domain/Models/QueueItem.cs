using System;
using System.IO;

namespace Converter.Domain.Models;

public enum ConversionStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
    Paused,
    Cancelled
}

public sealed class QueueItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FilePath { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public TimeSpan Duration { get; set; }
    public int Priority { get; set; } = 3;
    public bool IsStarred { get; set; }
    public ConversionStatus Status { get; set; } = ConversionStatus.Pending;
    public int Progress { get; set; }
    public string? OutputPath { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.Now;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public ConversionSettings? Settings { get; set; }
    public long? OutputFileSizeBytes { get; set; }
    public TimeSpan? ConversionDuration { get; set; }

    public string FileName => Path.GetFileName(FilePath);
}
