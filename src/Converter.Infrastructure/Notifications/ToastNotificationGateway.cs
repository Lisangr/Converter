using Converter.Application.Abstractions;
using Microsoft.Toolkit.Uwp.Notifications;

namespace Converter.Infrastructure.Notifications;

public sealed class ToastNotificationGateway : INotificationGateway
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task ShowInfoAsync(string title, string message, CancellationToken cancellationToken)
    {
        await ShowToastAsync(title, message, ToastScenario.Default, cancellationToken).ConfigureAwait(false);
    }

    public async Task ShowErrorAsync(string title, string message, CancellationToken cancellationToken)
    {
        await ShowToastAsync(title, message, ToastScenario.Reminder, cancellationToken).ConfigureAwait(false);
    }

    private async Task ShowToastAsync(string title, string message, ToastScenario scenario, CancellationToken token)
    {
        await _gate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            new ToastContentBuilder()
                .AddText(title)
                .AddText(message)
                .SetToastScenario(scenario)
                .Show();
        }
        finally
        {
            _gate.Release();
        }
    }

    public ValueTask DisposeAsync()
    {
        _gate.Dispose();
        return ValueTask.CompletedTask;
    }
}
