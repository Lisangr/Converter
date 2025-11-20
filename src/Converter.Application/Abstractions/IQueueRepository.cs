using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Converter.Domain.Models;

namespace Converter.Application.Abstractions
{
    /// <summary>
    /// Репозиторий для работы с очередью конвертации.
    /// Обеспечивает CRUD операции с элементами очереди и уведомления об изменениях.
    /// Использует событийную модель для синхронизации состояния с UI.
    /// </summary>
    public interface IQueueRepository
    {
        // ===== СОБЫТИЯ РЕПОЗИТОРИЯ =====
        
        /// <summary>Событие добавления нового элемента в очередь</summary>
        event EventHandler<QueueItem> ItemAdded;
        
        /// <summary>Событие обновления элемента очереди</summary>
        event EventHandler<QueueItem> ItemUpdated;
        
        /// <summary>Событие удаления элемента из очереди</summary>
        event EventHandler<Guid> ItemRemoved;
        
        // ===== ОПЕРАЦИИ ДОБАВЛЕНИЯ =====
        
        /// <summary>
        /// Добавляет новый элемент в очередь конвертации.
        /// Генерирует уникальный идентификатор и устанавливает начальный статус.
        /// </summary>
        /// <param name="item">Элемент для добавления в очередь</param>
        Task AddAsync(QueueItem item);
        
        /// <summary>
        /// Добавляет несколько элементов в очередь за одну операцию.
        /// Более эффективно для массового добавления файлов.
        /// </summary>
        /// <param name="items">Коллекция элементов для добавления</param>
        Task AddRangeAsync(IEnumerable<QueueItem> items);
        
        // ===== ОПЕРАЦИИ ОБНОВЛЕНИЯ =====
        
        /// <summary>
        /// Обновляет существующий элемент очереди.
        /// Сохраняет изменения статуса, прогресса и других свойств.
        /// </summary>
        /// <param name="item">Элемент с обновленными данными</param>
        Task UpdateAsync(QueueItem item);
        
        // ===== ОПЕРАЦИИ УДАЛЕНИЯ =====
        
        /// <summary>
        /// Удаляет элемент из очереди по идентификатору.
        /// Если элемент не найден, операция завершается без ошибки.
        /// </summary>
        /// <param name="id">Идентификатор удаляемого элемента</param>
        Task RemoveAsync(Guid id);
        
        /// <summary>
        /// Удаляет несколько элементов из очереди за одну операцию.
        /// </summary>
        /// <param name="ids">Коллекция идентификаторов элементов для удаления</param>
        Task RemoveRangeAsync(IEnumerable<Guid> ids);
        
        // ===== ОПЕРАЦИИ ЧТЕНИЯ =====
        
        /// <summary>
        /// Получает элемент очереди по идентификатору.
        /// </summary>
        /// <param name="id">Идентификатор искомого элемента</param>
        /// <returns>Найденный элемент или null</returns>
        Task<QueueItem> GetByIdAsync(Guid id);
        
        /// <summary>
        /// Получает все элементы очереди.
        /// Возвращает снимок состояния на момент вызова.
        /// </summary>
        /// <returns>Коллекция всех элементов очереди</returns>
        Task<IReadOnlyList<QueueItem>> GetAllAsync();
        
        /// <summary>
        /// Получает только ожидающие обработки элементы.
        /// Используется для определения работы процессора.
        /// </summary>
        /// <returns>Коллекция ожидающих элементов</returns>
        Task<IReadOnlyList<QueueItem>> GetPendingItemsAsync();
        
        /// <summary>
        /// Получает количество ожидающих обработки элементов.
        /// Более эффективно, чем получение всей коллекции.
        /// </summary>
        /// <returns>Количество ожидающих элементов</returns>
        Task<int> GetPendingCountAsync();
    }
}
