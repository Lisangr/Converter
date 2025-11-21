using System;
using Converter.Domain.Models;

namespace Converter.Application.Abstractions;

/// <summary>
/// Сервис уведомлений для отображения системных и пользовательских уведомлений.
/// </summary>
public class NotificationSummary
{
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public long TotalSpaceSaved { get; set; }
    public TimeSpan TotalProcessingTime { get; set; }
    public string? Message { get; set; }
}