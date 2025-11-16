using System;
using System.Drawing;
using System.IO;
using Converter.Services;

namespace Converter.Models;

public class QueueItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FilePath { get; set; } = string.Empty;
    public string? OutputPath { get; set; }
    public long FileSizeBytes { get; set; }
    public TimeSpan Duration { get; set; }
    public int Priority { get; set; } = 3;
    public bool IsStarred { get; set; }
    public ConversionStatus Status { get; set; } = ConversionStatus.Pending;
    public int Progress { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.Now;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public ConversionSettings? Settings { get; set; }
    public long? OutputFileSizeBytes { get; set; }
    public TimeSpan? ConversionDuration { get; set; }
    public string FileName => Path.GetFileName(FilePath);
    public string StatusText => GetStatusText();
    public Color StatusColor => GetStatusColor();
    public string ETA => CalculateETA();

    private string GetStatusText()
    {
        return Status switch
        {
            ConversionStatus.Pending => "В очереди",
            ConversionStatus.Processing => $"Конвертация {Progress}%",
            ConversionStatus.Completed => "Завершено",
            ConversionStatus.Failed => "Ошибка",
            ConversionStatus.Paused => "Пауза",
            ConversionStatus.Cancelled => "Отменено",
            _ => "Неизвестно"
        };
    }

    private Color GetStatusColor()
    {
        return Status switch
        {
            ConversionStatus.Pending => Color.Gray,
            ConversionStatus.Processing => Color.Orange,
            ConversionStatus.Completed => Color.Green,
            ConversionStatus.Failed => Color.Red,
            ConversionStatus.Paused => Color.Blue,
            ConversionStatus.Cancelled => Color.DarkGray,
            _ => Color.Black
        };
    }

    private string CalculateETA()
    {
        if (Status != ConversionStatus.Processing || Progress <= 0 || !StartedAt.HasValue)
        {
            return "--";
        }

        var elapsed = DateTime.Now - StartedAt.Value;
        if (elapsed.TotalSeconds <= 0)
        {
            return "--";
        }

        var totalEstimate = TimeSpan.FromTicks(elapsed.Ticks * 100 / Math.Max(1, Progress));
        var remaining = totalEstimate - elapsed;
        if (remaining.TotalSeconds < 1)
        {
            remaining = TimeSpan.Zero;
        }

        return remaining.TotalMinutes < 1
            ? $"{Math.Max(0, remaining.Seconds)} сек"
            : $"{(int)remaining.TotalMinutes} мин {Math.Max(0, remaining.Seconds)} сек";
    }
}

public class ConversionSettings
{
    public string? VideoCodec { get; set; }
    public int? Bitrate { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public string? AudioCodec { get; set; }
    public int? AudioBitrate { get; set; }
    public string? PresetName { get; set; }
    public string? ContainerFormat { get; set; }
    public int? Crf { get; set; }
    public bool EnableAudio { get; set; } = true;
    public bool CopyVideo { get; set; }
    public bool CopyAudio { get; set; }
    public bool UseHardwareAcceleration { get; set; }
    public int? Threads { get; set; }
    public AudioProcessingOptions? AudioProcessing { get; set; }
}

public enum QueueItemStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}