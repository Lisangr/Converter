using Converter.Application.Abstractions;
using Converter.Domain.Models;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Converter.Infrastructure.Persistence;

/// <summary>
/// Хранилище настроек уведомлений.
/// </summary>
public class NotificationSettingsStore : INotificationSettingsStore
{
    private readonly string _settingsPath;
    private readonly Microsoft.Extensions.Logging.ILogger<NotificationSettingsStore> _logger;

    // Конструктор для DI в приложении: путь к файлу берём из AppData
    public NotificationSettingsStore(Microsoft.Extensions.Logging.ILogger<NotificationSettingsStore> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _settingsPath = Path.Combine(appData, "Converter", "notificationSettings.json");
    }

    // Конструктор для тестов/ручной инициализации с явным путём
    public NotificationSettingsStore(string settingsPath, Microsoft.Extensions.Logging.ILogger<NotificationSettingsStore> logger)
    {
        _settingsPath = settingsPath ?? throw new ArgumentNullException(nameof(settingsPath));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Converter.Domain.Models.NotificationOptions Load()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                var options = JsonSerializer.Deserialize<Converter.Domain.Models.NotificationOptions>(json);
                return options ?? new Converter.Domain.Models.NotificationOptions();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при загрузке настроек уведомлений");
        }

        return new Converter.Domain.Models.NotificationOptions();
    }

    public void Save(Converter.Domain.Models.NotificationOptions settings)
    {
        if (settings == null) throw new ArgumentNullException(nameof(settings));

        try
        {
            var directory = Path.GetDirectoryName(_settingsPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при сохранении настроек уведомлений");
            throw;
        }
    }
}