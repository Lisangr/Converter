using System;
using Converter.Domain.Models;

namespace Converter.Application.Abstractions;

/// <summary>
/// Хранилище настроек уведомлений.
/// </summary>
public interface INotificationSettingsStore
{
    /// <summary>
    /// Загружает настройки уведомлений.
    /// </summary>
    Converter.Domain.Models.NotificationOptions Load();

    /// <summary>
    /// Сохраняет настройки уведомлений.
    /// </summary>
    void Save(Converter.Domain.Models.NotificationOptions settings);
}