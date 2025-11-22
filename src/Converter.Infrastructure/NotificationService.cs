using System;
using System.Threading.Tasks;
using Converter.Application.Abstractions;
using Converter.Domain.Models;

namespace Converter.Infrastructure;

/// <summary>
/// Сервис уведомлений, реализующий INotificationService поверх INotificationGateway.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly INotificationGateway _gateway;
    private readonly INotificationSettingsStore _settingsStore;

    public NotificationService(INotificationGateway gateway, INotificationSettingsStore settingsStore)
    {
        _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        _settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
    }

    public void NotifyConversionComplete(NotificationSummary result)
    {
        var message = result.Message ?? "Conversion completed successfully";
        var title = "Video Converter";
        var type = result.FailedCount > 0 ? NotificationType.Warning : NotificationType.Success;

        _ = Task.Run(async () => await _gateway.ShowInfoAsync(message, title));
    }

    public void NotifyProgress(int current, int total)
    {
        var percentage = total > 0 ? (current * 100 / total) : 0;
        var message = $"Progress: {percentage}%";

        _ = Task.Run(async () => await _gateway.ShowInfoAsync(message, "Conversion Progress"));
    }

    public void ResetProgressNotifications()
    {
        // Заглушка для тестов / будущей реализации
    }

    public Converter.Domain.Models.NotificationOptions GetSettings()
    {
        // Заглушка для тестов - возвращаем дефолтные настройки
        return new Converter.Domain.Models.NotificationOptions();
    }

    public void UpdateSettings(Converter.Domain.Models.NotificationOptions settings)
    {
        // Заглушка для тестов / будущего сохранения настроек
    }

    public void Dispose()
    {
        // Заглушка для тестов
    }
}
