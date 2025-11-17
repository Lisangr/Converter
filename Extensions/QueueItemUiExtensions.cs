using System;
using System.Drawing;
using Converter.Domain.Models;

namespace Converter.Extensions;

public static class QueueItemUiExtensions
{
    public static string GetStatusText(this QueueItem item)
    {
        if (item == null) return "--";

        return item.Status switch
        {
            ConversionStatus.Pending => "В очереди",
            ConversionStatus.Processing => $"Конвертация {item.Progress}%",
            ConversionStatus.Completed => "Завершено",
            ConversionStatus.Failed => "Ошибка",
            ConversionStatus.Paused => "Пауза",
            ConversionStatus.Cancelled => "Отменено",
            _ => "Неизвестно"
        };
    }

    public static Color GetStatusColor(this QueueItem item)
    {
        if (item == null) return Color.Black;

        return item.Status switch
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

    public static string GetEta(this QueueItem item, DateTime? now = null)
    {
        if (item == null)
        {
            return "--";
        }

        if (item.Status != ConversionStatus.Processing || item.Progress <= 0 || !item.StartedAt.HasValue)
        {
            return "--";
        }

        var referenceTime = now ?? DateTime.Now;
        var elapsed = referenceTime - item.StartedAt.Value;
        if (elapsed.TotalSeconds <= 0)
        {
            return "--";
        }

        var totalEstimate = TimeSpan.FromTicks(elapsed.Ticks * 100 / Math.Max(1, item.Progress));
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
