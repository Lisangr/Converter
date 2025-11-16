using Converter.Application.Abstractions;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Converter.Models;

namespace Converter.Infrastructure.Notifications;

public sealed class NotificationGateway : INotificationGateway
{
    public IProgressReporter CreateProgressReporter(string operationName)
    {
        return new ProgressReporter(operationName, this);
    }

    public async Task ShowInfoAsync(string message, string? title = null, CancellationToken ct = default)
    {
        Debug.WriteLine($"INFO: {title ?? "Info"} - {message}");
        await Task.CompletedTask;
    }

    public async Task ShowWarningAsync(string message, string? title = null, CancellationToken ct = default)
    {
        Debug.WriteLine($"WARNING: {title ?? "Warning"} - {message}");
        await Task.CompletedTask;
    }

    public async Task ShowErrorAsync(string message, string? title = null, CancellationToken ct = default)
    {
        Debug.WriteLine($"ERROR: {title ?? "Error"} - {message}");
        await Task.CompletedTask;
    }

    public async Task ShowSuccessAsync(string message, string? title = null, CancellationToken ct = default)
    {
        Debug.WriteLine($"SUCCESS: {title ?? "Success"} - {message}");
        await Task.CompletedTask;
    }

    public async Task ShowToastAsync(string message, TimeSpan duration, CancellationToken ct = default)
    {
        Debug.WriteLine($"TOAST ({duration}): {message}");
        await Task.CompletedTask;
    }

    public async Task<bool> ShowConfirmationAsync(string message, string title, string confirmText, string cancelText, CancellationToken ct = default)
    {
        Debug.WriteLine($"CONFIRM: {title} - {message} [{confirmText}/{cancelText}]");
        // For simplicity, always return true in the debug implementation
        // In a real implementation, this would show a dialog and return the user\'s choice
        return await Task.FromResult(true);
    }
}

public class ProgressReporter : IProgressReporter
{
    private readonly string _operationName;
    private readonly INotificationGateway _notificationGateway;

    public ProgressReporter(string operationName, INotificationGateway notificationGateway)
    {
        _operationName = operationName;
        _notificationGateway = notificationGateway;
    }

    public void ReportItemProgress(QueueItem item, int progress, string? status = null)
    {
        Debug.WriteLine($"PROGRESS ({_operationName}) - Item {item.Id}: {progress}% - {status}");
    }

    public void ReportGlobalProgress(int progress, string? status = null)
    {
        Debug.WriteLine($"PROGRESS ({_operationName}) - Global: {progress}% - {status}");
    }

    public void ReportError(QueueItem item, string error)
    {
        _notificationGateway.ShowErrorAsync($"Item {item.Id}: {error}", "Error").ConfigureAwait(false);
    }

    public void ReportWarning(QueueItem item, string warning)
    {
        _notificationGateway.ShowWarningAsync($"Item {item.Id}: {warning}", "Warning").ConfigureAwait(false);
    }

    public void ReportInfo(QueueItem item, string message)
    {
        _notificationGateway.ShowInfoAsync($"Item {item.Id}: {message}", "Info").ConfigureAwait(false);
    }
}
