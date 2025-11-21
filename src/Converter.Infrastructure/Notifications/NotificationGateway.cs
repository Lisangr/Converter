using Converter.Application.Abstractions;
using Converter.Domain.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Converter.Infrastructure.Notifications;

/// <summary>
/// Реализация шлюза уведомлений.
/// Реализует как INotificationGateway, так и методы для тестов.
/// </summary>
public class NotificationGateway : INotificationGateway
{
    private readonly Microsoft.Extensions.Logging.ILogger<NotificationGateway> _logger;

    public NotificationGateway(Microsoft.Extensions.Logging.ILogger<NotificationGateway> logger)
    {
        _logger = logger;
    }

    public Task ShowInfoAsync(string message, string? title = null, CancellationToken ct = default)
    {
        _logger.LogInformation("Info: {Message}", message);
        return Task.CompletedTask;
    }

    public Task ShowWarningAsync(string message, string? title = null, CancellationToken ct = default)
    {
        _logger.LogWarning("Warning: {Message}", message);
        return Task.CompletedTask;
    }

    public Task ShowErrorAsync(string message, string? title = null, CancellationToken ct = default)
    {
        _logger.LogError("Error: {Message}", message);
        return Task.CompletedTask;
    }

    public Task ShowSuccessAsync(string message, string? title = null, CancellationToken ct = default)
    {
        _logger.LogInformation("Success: {Message}", message);
        return Task.CompletedTask;
    }

    public Task ShowToastAsync(string message, TimeSpan duration, CancellationToken ct = default)
    {
        _logger.LogInformation("Toast: {Message}", message);
        return Task.CompletedTask;
    }

    public Task<bool> ShowConfirmationAsync(string message, string title, string confirmText, string cancelText, CancellationToken ct = default)
    {
        _logger.LogInformation("Confirmation: {Title} - {Message}", title, message);
        return Task.FromResult(true);
    }

    public IProgressReporter CreateProgressReporter(string operationName)
    {
        return new ProgressReporter(operationName, _logger);
    }

    // Класс для реализации IProgressReporter из INotificationGateway
    private class ProgressReporter : IProgressReporter
    {
        private readonly string _operationName;
        private readonly Microsoft.Extensions.Logging.ILogger _logger;

        public ProgressReporter(string operationName, Microsoft.Extensions.Logging.ILogger logger)
        {
            _operationName = operationName;
            _logger = logger;
        }

        public void Report(int progress)
        {
            _logger.LogInformation("{Operation}: Progress {Progress}%", _operationName, progress);
        }

        public void Report(double progress)
        {
            _logger.LogInformation("{Operation}: Progress {Progress:F1}%", _operationName, progress * 100);
        }

        public void ReportItemProgress(QueueItem item, int progress, string? status = null)
        {
            _logger.LogInformation("Item {ItemId}: Progress {Progress}% {Status}", 
                item.Id, progress, status ?? string.Empty);
        }

        public void ReportGlobalProgress(int progress, string? status = null)
        {
            _logger.LogInformation("Global Progress: {Progress}% {Status}", 
                progress, status ?? string.Empty);
        }

        public void ReportError(QueueItem item, string error)
        {
            _logger.LogError("Item {ItemId} Error: {Error}", item.Id, error);
        }

        public void ReportWarning(QueueItem item, string warning)
        {
            _logger.LogWarning("Item {ItemId} Warning: {Warning}", item.Id, warning);
        }

        public void ReportInfo(QueueItem item, string message)
        {
            _logger.LogInformation("Item {ItemId}: {Message}", item.Id, message);
        }
    }
}