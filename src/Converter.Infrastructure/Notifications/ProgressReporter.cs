using System;
using Converter.Application.Abstractions;
using Converter.Domain.Models;

namespace Converter.Infrastructure.Notifications;

/// <summary>
/// Устаревший класс прогресс-репортера.
/// Заменен на класс ProgressReporter в NotificationGateway.cs
/// </summary>
[System.Obsolete("Используйте NotificationGateway.CreateProgressReporter")]
public class LegacyProgressReporter
{
    public void ReportItemProgress(QueueItem item, int progress, string? status = null)
    {
        // Заглушка для тестов
    }

    public void ReportGlobalProgress(int progress, string? status = null)
    {
        // Заглушка для тестов
    }

    public void ReportError(QueueItem item, string error)
    {
        // Заглушка для тестов
    }

    public void ReportWarning(QueueItem item, string warning)
    {
        // Заглушка для тестов
    }

    public void ReportInfo(QueueItem item, string message)
    {
        // Заглушка для тестов
    }
}