namespace Converter.Application.Abstractions;

using Converter.Services;

/// <summary>
/// Хранилище настроек уведомлений.
/// Обеспечивает персистентность пользовательских настроек уведомлений
/// между сеансами работы приложения.
/// </summary>
public interface INotificationSettingsStore
{
    /// <summary>
    /// Загружает сохраненные настройки уведомлений.
    /// Возвращает настройки по умолчанию, если сохраненных настроек нет.
    /// </summary>
    /// <returns>Объект с настройками уведомлений</returns>
    NotificationSettings Load();
    
    /// <summary>
    /// Сохраняет настройки уведомлений в постоянное хранилище.
    /// </summary>
    /// <param name="settings">Настройки для сохранения</param>
    void Save(NotificationSettings settings);
}
