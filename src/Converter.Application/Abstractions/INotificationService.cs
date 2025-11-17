namespace Converter.Application.Abstractions;

using System;
using Converter.Services;

public interface INotificationService : IDisposable
{
    void NotifyConversionComplete(NotificationSummary result);
    void NotifyProgress(int current, int total);
    void ResetProgressNotifications();

    NotificationSettings GetSettings();
    void UpdateSettings(NotificationSettings settings);
}
