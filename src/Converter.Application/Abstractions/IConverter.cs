using System;
using System.Threading;
using System.Threading.Tasks;
using Converter.Domain.Models;
using Converter.Application.Models;

namespace Converter.Application.Abstractions;

/// <summary>
/// Интерфейс конвертера для выполнения операций конвертации.
/// </summary>
public interface IConverter
{
    /// <summary>
    /// Выполняет конвертацию файла.
    /// </summary>
    /// <param name="item">Элемент очереди для конвертации</param>
    /// <param name="progress">Объект для отслеживания прогресса</param>
    /// <param name="cancellationToken">Токен отмены операции</param>
    /// <returns>Результат конвертации</returns>
    Task<ConversionResult> ConvertAsync(QueueItem item, IProgress<int>? progress = null, CancellationToken cancellationToken = default);
}