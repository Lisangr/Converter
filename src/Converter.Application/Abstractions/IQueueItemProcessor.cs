using System.Threading;
using System.Threading.Tasks;
using Converter.Domain.Models;

namespace Converter.Application.Abstractions;

/// <summary>
/// Интерфейс процессора элементов очереди конвертации.
/// Отвечает за выполнение конвертации отдельного файла.
/// </summary>
public interface IQueueItemProcessor
{
    /// <summary>
    /// Обрабатывает элемент очереди конвертации.
    /// </summary>
    /// <param name="item">Элемент для обработки</param>
    /// <param name="cancellationToken">Токен отмены операции</param>
    /// <returns>True если обработка прошла успешно, иначе false</returns>
    Task<bool> ProcessAsync(QueueItem item, CancellationToken cancellationToken);
}