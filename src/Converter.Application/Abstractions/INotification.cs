namespace Converter.Application.Abstractions;

/// <summary>
/// Базовый интерфейс для всех уведомлений.
/// </summary>
public interface INotification
{
    /// <summary>
    /// Заголовок уведомления.
    /// </summary>
    string Title { get; }
    
    /// <summary>
    /// Сообщение уведомления.
    /// </summary>
    string Message { get; }
    
    /// <summary>
    /// Тип уведомления.
    /// </summary>
    NotificationType Type { get; }
}

/// <summary>
/// Типы уведомлений.
/// </summary>
public enum NotificationType
{
    Info,
    Warning,
    Error,
    Success
}

/// <summary>
/// Интерфейс для обработчиков уведомлений.
/// </summary>
/// <typeparam name="T">Тип уведомления для обработки</typeparam>
public interface INotificationHandler<in T> where T : INotification
{
    /// <summary>
    /// Обрабатывает уведомление.
    /// </summary>
    /// <param name="notification">Уведомление для обработки</param>
    /// <param name="cancellationToken">Токен отмены</param>
    Task HandleAsync(T notification, CancellationToken cancellationToken = default);
}