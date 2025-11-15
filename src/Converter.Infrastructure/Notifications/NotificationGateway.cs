using Converter.Application.Abstractions;
using System.Diagnostics;

namespace Converter.Infrastructure.Notifications;

public sealed class NotificationGateway : INotificationGateway
{
    public void Info(string title, string message)
    {
        Debug.WriteLine($"INFO: {title} - {message}");
    }

    public void Error(string title, string message)
    {
        Debug.WriteLine($"ERROR: {title} - {message}");
    }

    public void Progress(string title, string message, int percent)
    {
        Debug.WriteLine($"PROGRESS({percent}%): {title} - {message}");
    }
}
