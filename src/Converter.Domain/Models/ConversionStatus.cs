using System;

namespace Converter.Domain.Models;

/// <summary>
/// Статус конвертации элемента очереди.
/// </summary>
public enum ConversionStatus
{
    /// <summary>
    /// Элемент ожидает обработки.
    /// </summary>
    Pending = 0,
    
    /// <summary>
    /// Элемент обрабатывается.
    /// </summary>
    Processing = 1,
    
    /// <summary>
    /// Элемент успешно обработан.
    /// </summary>
    Completed = 2,
    
    /// <summary>
    /// Обработка элемента завершилась с ошибкой.
    /// </summary>
    Failed = 3,
    
    /// <summary>
    /// Обработка элемента приостановлена.
    /// </summary>
    Paused = 4,
    
    /// <summary>
    /// Обработка элемента отменена пользователем.
    /// </summary>
    Cancelled = 5
}