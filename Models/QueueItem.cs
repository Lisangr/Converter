using System;

namespace Converter.Models;

public class QueueItem
{
    public string InputPath { get; set; } = string.Empty;
    public string? OutputPath { get; set; }
    public long FileSizeBytes { get; set; }
    public long? OutputFileSizeBytes { get; set; }
    public TimeSpan? ConversionDuration { get; set; }
    public ConversionStatus Status { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public QueueItemSettings? Settings { get; set; }
}

public class QueueItemSettings
{
    public string? VideoCodec { get; set; }
    public string? PresetName { get; set; }
}
