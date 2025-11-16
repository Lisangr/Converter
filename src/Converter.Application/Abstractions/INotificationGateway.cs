using System;
using System.Threading;
using System.Threading.Tasks;

namespace Converter.Application.Abstractions;

public interface INotificationGateway
{
    Task ShowInfoAsync(string message, string? title = null, CancellationToken ct = default);
    Task ShowWarningAsync(string message, string? title = null, CancellationToken ct = default);
    Task ShowErrorAsync(string message, string? title = null, CancellationToken ct = default);
    Task ShowSuccessAsync(string message, string? title = null, CancellationToken ct = default);
    Task ShowToastAsync(string message, TimeSpan duration, CancellationToken ct = default);
    
    // For more complex notifications
    Task<bool> ShowConfirmationAsync(string message, string title, string confirmText, string cancelText, CancellationToken ct = default);
    
    // For progress notifications
    IProgressReporter CreateProgressReporter(string operationName);
}
