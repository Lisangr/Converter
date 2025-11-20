namespace Converter.Application.Abstractions;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Converter.Domain.Models;
using Converter.Models;

/// <summary>
/// Сервис для создания и распространения отчетов о результатах конвертации.
/// Обеспечивает генерацию текстовых и графических отчетов для социальных сетей,
/// блогов и других платформ с возможностью копирования в буфер обмена.
/// </summary>
public interface IShareService : IDisposable
{
    /// <summary>
    /// Генерирует отчет о завершенных операциях конвертации.
    /// Анализирует результаты и создает сводную статистику для публикации.
    /// </summary>
    /// <param name="completedItems">Список завершенных элементов конвертации</param>
    /// <returns>Объект отчета с агрегированной статистикой или null</returns>
    ShareReport? GenerateReport(List<QueueItem> completedItems);
    
    /// <summary>
    /// Копирует текст в системный буфер обмена.
    /// Используется для быстрого распространения статистики и отчетов.
    /// </summary>
    /// <param name="text">Текст для копирования в буфер обмена</param>
    void CopyToClipboard(string text);
    
    /// <summary>
    /// Генерирует графический отчет в формате изображения.
    /// Создает визуально привлекательное изображение с результатами конвертации.
    /// </summary>
    /// <param name="report">Объект отчета с данными</param>
    /// <param name="outputPath">Путь для сохранения изображения</param>
    /// <returns>Путь к созданному файлу изображения</returns>
    Task<string> GenerateImageReport(ShareReport report, string outputPath);
}
