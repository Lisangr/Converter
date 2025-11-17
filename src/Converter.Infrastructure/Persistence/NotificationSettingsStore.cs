using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Converter.Application.Abstractions;
using Converter.Services;

namespace Converter.Infrastructure.Persistence;

public class NotificationSettingsStore : INotificationSettingsStore
{
    private string GetPath() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Converter",
            "notifications.json");

    public NotificationSettings Load()
    {
        try
        {
            var path = GetPath();
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var settings = JsonSerializer.Deserialize<NotificationSettings>(json);
                if (settings != null)
                    return settings;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Не удалось загрузить настройки уведомлений: {ex.Message}");
        }

        return new NotificationSettings();
    }

    public void Save(NotificationSettings settings)
    {
        try
        {
            var path = GetPath();
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Не удалось сохранить настройки уведомлений: {ex.Message}");
        }
    }
}
