using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Converter.Models;
using Converter.Domain.Models;
using Converter.Services.UIServices; 

namespace Converter.Services.UIServices
{
    /// <summary>
    /// Сервис операций с файлами в пользовательском интерфейсе.
    /// Обеспечивает высокоуровневые операции для управления файлами в очереди
    /// конвертации с интеграцией событийной модели для синхронизации с UI.
    /// </summary>
    public interface IFileOperationsService
    {
        /// <summary>
        /// Добавляет файлы в очередь конвертации.
        /// Валидирует файлы и создает элементы очереди для каждого файла.
        /// </summary>
        /// <param name="filePaths">Коллекция путей к добавляемым файлам</param>
        Task AddFilesAsync(IEnumerable<string> filePaths);

        /// <summary>
        /// Удаляет выбранные файлы из очереди конвертации.
        /// Удаляет элементы из очереди и освобождает связанные ресурсы.
        /// </summary>
        /// <param name="selectedItems">Коллекция элементов очереди для удаления</param>
        Task RemoveSelectedFilesAsync(IEnumerable<QueueItem> selectedItems);

        /// <summary>
        /// Очищает все файлы из очереди конвертации.
        /// Полностью очищает очередь, включая все элементы и их ресурсы.
        /// </summary>
        Task ClearAllFilesAsync();

        /// <summary>
        /// Получает текущие элементы очереди.
        /// Возвращает снимок состояния очереди на момент вызова.
        /// </summary>
        /// <returns>Коллекция элементов очереди</returns>
        IReadOnlyList<QueueItem> GetQueueItems();

        /// <summary>
        /// Событие обновления очереди.
        /// Вызывается при любых изменениях в состоянии очереди.
        /// </summary>
        event EventHandler<QueueUpdatedEventArgs> QueueUpdated;

        /// <summary>
        /// Событие добавления элемента в очередь.
        /// Вызывается при добавлении нового элемента.
        /// </summary>
        event EventHandler<QueueItem>? QueueItemAdded;

        /// <summary>
        /// Обновляет существующий элемент очереди.
        /// Синхронизирует изменения с хранилищем и уведомляет подписчиков.
        /// </summary>
        /// <param name="item">Элемент очереди с обновленными данными</param>
        Task UpdateQueueItem(QueueItem item);
    }

    /// <summary>
    /// Аргументы события обновления очереди.
    /// Содержит полное состояние очереди на момент события.
    /// </summary>
    public class QueueUpdatedEventArgs : EventArgs
    {
        /// <summary>Текущее состояние всех элементов очереди</summary>
        public IReadOnlyList<QueueItem> QueueItems { get; }

        /// <summary>
        /// Создает новый экземпляр аргументов события обновления очереди.
        /// </summary>
        /// <param name="queueItems">Коллекция элементов очереди</param>
        public QueueUpdatedEventArgs(IReadOnlyList<QueueItem> queueItems)
        {
            QueueItems = queueItems ?? throw new ArgumentNullException(nameof(queueItems));
        }
    }
}
