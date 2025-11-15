using Converter.Application.Interfaces;
using Microsoft.Toolkit.Uwp.Notifications;

namespace Converter.Infrastructure.Services;

public sealed class NotificationGateway : INotificationGateway
{
    public Task ShowSuccessAsync(string title, string message, CancellationToken cancellationToken)
        => ShowAsync(title, message, ToastScenario.Default, cancellationToken);

    public Task ShowWarningAsync(string title, string message, CancellationToken cancellationToken)
        => ShowAsync(title, message, ToastScenario.Reminder, cancellationToken);

    public Task ShowErrorAsync(string title, string message, CancellationToken cancellationToken)
        => ShowAsync(title, message, ToastScenario.Alarm, cancellationToken);

    private Task ShowAsync(string title, string message, ToastScenario scenario, CancellationToken cancellationToken)
    {
        var builder = new ToastContentBuilder()
            .AddText(title)
            .AddText(message)
            .SetToastScenario(scenario);

        cancellationToken.ThrowIfCancellationRequested();
        builder.Show();
        return Task.CompletedTask;
    }
}
