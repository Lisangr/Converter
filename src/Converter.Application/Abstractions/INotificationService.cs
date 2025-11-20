namespace Converter.Application.Abstractions;

using System;
using Converter.Services;

/// <summary>
/// Сервис уведомлений для управления системными и пользовательскими уведомлениями.
/// Отвечает за отображение уведомлений о завершении конвертации, прогрессе операций
/// и управление настройками уведомлений.
/// </summary>
public interface INotificationService : IDisposable
{
    /// <summary>
    /// Уведомляет о завершении конвертации с результатами операции.
    /// Отображает соответствующее уведомление в зависимости от результата.
    /// </summary>
    /// <param name="result">Сводка результатов конвертации</param>
    void NotifyConversionComplete(NotificationSummary result);
    
    /// <summary>
    /// Уведомляет о текущем прогрессе операции.
    /// Может отображать промежуточные уведомления в зависимости от настроек.
    /// </summary>
    /// <param name="current">Текущий прогресс</param>
    /// <param name="total">Общий объем работы</param>
    void NotifyProgress(int current, int total);
    
    /// <summary>
    /// Сбрасывает состояние уведомлений о прогрессе.
    /// Используется для подготовки к новой операции.
    /// </summary>
    void ResetProgressNotifications();

    // ===== УПРАВЛЕНИЕ НАСТРОЙКАМИ =====
    
    /// <summary>Получает текущие настройки уведомлений</summary>
    /// <returns>Объект с настройками уведомлений</returns>
    NotificationSettings GetSettings();
    
    /// <summary>Обновляет настройки уведомлений</summary>
    /// <param name="settings">Новые настройки уведомлений</param>
    void UpdateSettings(NotificationSettings settings);
}
