namespace Converter.Application.Abstractions;

using Converter.Services;

public interface INotificationSettingsStore
{
    NotificationSettings Load();
    void Save(NotificationSettings settings);
}
