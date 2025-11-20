using System;
using System.Threading;
using System.Threading.Tasks;

namespace Converter.Application.Abstractions;

/// <summary>
/// Шлюз уведомлений - UI-агностичный интерфейс для отображения уведомлений.
/// Абстрагирует конкретную реализацию уведомлений (Windows Forms, WPF, консоль и т.д.),
/// обеспечивая кроссплатформенность и возможность тестирования.
/// </summary>
public interface INotificationGateway
{
    /// <summary>Отображает информационное уведомление</summary>
    /// <param name="message">Текст сообщения</param>
    /// <param name="title">Необязательный заголовок уведомления</param>
    /// <param name="ct">Токен отмены операции</param>
    Task ShowInfoAsync(string message, string? title = null, CancellationToken ct = default);
    
    /// <summary>Отображает предупреждающее уведомление</summary>
    /// <param name="message">Текст предупреждения</param>
    /// <param name="title">Необязательный заголовок уведомления</param>
    /// <param name="ct">Токен отмены операции</param>
    Task ShowWarningAsync(string message, string? title = null, CancellationToken ct = default);
    
    /// <summary>Отображает уведомление об ошибке</summary>
    /// <param name="message">Текст ошибки</param>
    /// <param name="title">Необязательный заголовок уведомления</param>
    /// <param name="ct">Токен отмены операции</param>
    Task ShowErrorAsync(string message, string? title = null, CancellationToken ct = default);
    
    /// <summary>Отображает уведомление об успешном завершении</summary>
    /// <param name="message">Текст сообщения об успехе</param>
    /// <param name="title">Необязательный заголовок уведомления</param>
    /// <param name="ct">Токен отмены операции</param>
    Task ShowSuccessAsync(string message, string? title = null, CancellationToken ct = default);
    
    /// <summary>Отображает кратковременное toast-уведомление</summary>
    /// <param name="message">Текст сообщения</param>
    /// <param name="duration">Продолжительность отображения</param>
    /// <param name="ct">Токен отмены операции</param>
    Task ShowToastAsync(string message, TimeSpan duration, CancellationToken ct = default);
    
    // ===== ДИАЛОГИ ПОДТВЕРЖДЕНИЯ =====
    
    /// <summary>Отображает диалог подтверждения с настраиваемыми кнопками</summary>
    /// <param name="message">Текст вопроса</param>
    /// <param name="title">Заголовок диалога</param>
    /// <param name="confirmText">Текст кнопки подтверждения</param>
    /// <param name="cancelText">Текст кнопки отмены</param>
    /// <param name="ct">Токен отмены операции</param>
    /// <returns>True если пользователь подтвердил, false если отменил</returns>
    Task<bool> ShowConfirmationAsync(string message, string title, string confirmText, string cancelText, CancellationToken ct = default);
    
    // ===== ПРОГРЕСС УВЕДОМЛЕНИЙ =====
    
    /// <summary>Создает объект для отслеживания прогресса операции</summary>
    /// <param name="operationName">Название операции для идентификации</param>
    /// <returns>Объект для отчета о прогрессе</returns>
    IProgressReporter CreateProgressReporter(string operationName);
}
