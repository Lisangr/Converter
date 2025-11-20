using System;
using System.Threading.Tasks;
using Converter.Domain.Models;

namespace Converter.Application.Abstractions
{
    /// <summary>
    /// Репортер прогресса операций.
    /// Обеспечивает централизованное отслеживание и отчетность о прогрессе
    /// конвертации, включая информацию об ошибках, предупреждениях и статусах.
    /// </summary>
    public interface IProgressReporter
    {
        /// <summary>
        /// Отчитывается о прогрессе конвертации конкретного элемента.
        /// Обновляет прогресс и статус отдельного файла в очереди.
        /// </summary>
        /// <param name="item">Элемент очереди для отчета</param>
        /// <param name="progress">Процент выполнения (0-100)</param>
        /// <param name="status">Необязательный текст статуса</param>
        void ReportItemProgress(QueueItem item, int progress, string? status = null);
        
        /// <summary>
        /// Отчитывается об общем прогрессе всех операций.
        /// Предоставляет глобальную статистику по всей очереди.
        /// </summary>
        /// <param name="progress">Общий процент выполнения (0-100)</param>
        /// <param name="status">Необязательный глобальный статус</param>
        void ReportGlobalProgress(int progress, string? status = null);
        
        /// <summary>
        /// Регистрирует ошибку, произошедшую при обработке элемента.
        /// Обеспечивает детальное логирование проблем конвертации.
        /// </summary>
        /// <param name="item">Элемент очереди, где произошла ошибка</param>
        /// <param name="error">Описание ошибки</param>
        void ReportError(QueueItem item, string error);
        
        /// <summary>
        /// Регистрирует предупреждение при обработке элемента.
        /// Информирует о потенциальных проблемах, не прерывающих процесс.
        /// </summary>
        /// <param name="item">Элемент очереди, где возникло предупреждение</param>
        /// <param name="warning">Текст предупреждения</param>
        void ReportWarning(QueueItem item, string warning);
        
        /// <summary>
        /// Регистрирует информационное сообщение о процессе.
        /// Используется для отслеживания важных этапов конвертации.
        /// </summary>
        /// <param name="item">Элемент очереди, к которому относится сообщение</param>
        /// <param name="message">Информационное сообщение</param>
        void ReportInfo(QueueItem item, string message);
    }
}
