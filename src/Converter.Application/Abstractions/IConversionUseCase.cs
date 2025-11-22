using System;
using System.Threading;
using System.Threading.Tasks;
using Converter.Domain.Models;

namespace Converter.Application.Abstractions
{
    /// <summary>
    /// Use Case для выполнения конвертации отдельного элемента очереди.
    /// Инкапсулирует бизнес-логику конвертации одного файла, обеспечивая
    /// чистоту архитектуры и соблюдение принципов Clean Architecture.
    /// </summary>
    public interface IConversionUseCase
    {
        /// <summary>
        /// Выполняет конвертацию указанного элемента очереди.
        /// Обрабатывает все необходимые операции для преобразования файла.
        /// </summary>
        /// <param name="item">Элемент очереди для конвертации</param>
        /// <param name="progress">Необязательный объект для отслеживания прогресса</param>
        /// <param name="cancellationToken">Токен отмены операции</param>
        /// <returns>Результат конвертации с информацией об успешности и метриках</returns>
        Task<ConversionResult> ExecuteAsync(QueueItem item, IProgress<int>? progress = null, CancellationToken cancellationToken = default);
    }
}
