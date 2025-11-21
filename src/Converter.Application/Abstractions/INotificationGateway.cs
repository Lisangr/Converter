using System;
using System.Threading;
using System.Threading.Tasks;
using Converter.Domain.Models;

namespace Converter.Application.Abstractions;

/// <summary>
/// Шлюз уведомлений для отображения уведомлений пользователю.
/// </summary>
public interface INotificationGateway
{
    /// <summary>
    /// Отображает информационное уведомление.
    /// </summary>
    Task ShowInfoAsync(string message, string? title = null, CancellationToken ct = default);
    
    /// <summary>
    /// Отображает предупреждающее уведомление.
    /// </summary>
    Task ShowWarningAsync(string message, string? title = null, CancellationToken ct = default);
    
    /// <summary>
    /// Отображает уведомление об ошибке.
    /// </summary>
    Task ShowErrorAsync(string message, string? title = null, CancellationToken ct = default);
    
    /// <summary>
    /// Отображает уведомление об успешном завершении.
    /// </summary>
    Task ShowSuccessAsync(string message, string? title = null, CancellationToken ct = default);
    
    /// <summary>
    /// Отображает кратковременное toast-уведомление.
    /// </summary>
    Task ShowToastAsync(string message, TimeSpan duration, CancellationToken ct = default);
    
    /// <summary>
    /// Отображает диалог подтверждения с настраиваемыми кнопками.
    /// </summary>
    Task<bool> ShowConfirmationAsync(string message, string title, string confirmText, string cancelText, CancellationToken ct = default);
    
    /// <summary>
    /// Создает объект для отслеживания прогресса операции.
    /// </summary>
    IProgressReporter CreateProgressReporter(string operationName);
}

/// <summary>
/// Интерфейс для отслеживания прогресса операций.
/// </summary>
public interface IProgressReporter
{
    /// <summary>
    /// Сообщает о прогрессе в процентах.
    /// </summary>
    /// <param name="progress">Прогресс (0-100)</param>
    void Report(int progress);
    
    /// <summary>
    /// Сообщает о прогрессе в долях от единицы.
    /// </summary>
    /// <param name="progress">Прогресс (0.0-1.0)</param>
    void Report(double progress);
    
    /// <summary>
    /// Отчитывается о прогрессе конвертации конкретного элемента.
    /// </summary>
    /// <param name="item">Элемент очереди для отчета</param>
    /// <param name="progress">Процент выполнения (0-100)</param>
    /// <param name="status">Необязательный текст статуса</param>
    void ReportItemProgress(QueueItem item, int progress, string? status = null);
    
    /// <summary>
    /// Отчитывается об общем прогрессе всех операций.
    /// </summary>
    /// <param name="progress">Общий процент выполнения (0-100)</param>
    /// <param name="status">Необязательный глобальный статус</param>
    void ReportGlobalProgress(int progress, string? status = null);
    
    /// <summary>
    /// Регистрирует ошибку при обработке элемента.
    /// </summary>
    /// <param name="item">Элемент очереди, где произошла ошибка</param>
    /// <param name="error">Описание ошибки</param>
    void ReportError(QueueItem item, string error);
    
    /// <summary>
    /// Регистрирует предупреждение при обработке элемента.
    /// </summary>
    /// <param name="item">Элемент очереди, где возникло предупреждение</param>
    /// <param name="warning">Текст предупреждения</param>
    void ReportWarning(QueueItem item, string warning);
    
    /// <summary>
    /// Регистрирует информационное сообщение о процессе.
    /// </summary>
    /// <param name="item">Элемент очереди, к которому относится сообщение</param>
    /// <param name="message">Информационное сообщение</param>
    void ReportInfo(QueueItem item, string message);
}