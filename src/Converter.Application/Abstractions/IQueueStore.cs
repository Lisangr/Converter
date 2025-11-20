using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Converter.Domain.Models;

namespace Converter.Application.Abstractions
{
    /// <summary>
    /// Асинхронное хранилище очереди конвертации.
    /// Высокопроизводительная замена IQueueRepository с поддержкой асинхронных потоков
    /// и оптимизированными операциями для конкурентной обработки элементов очереди.
    /// Обеспечивает thread-safe операции и эффективное управление состоянием.
    /// </summary>
    public interface IQueueStore
    {
        // ===== АСИНХРОННЫЕ ПОТОКИ =====
        
        /// <summary>
        /// Получает асинхронный поток всех элементов очереди.
        /// Позволяет эффективно обрабатывать большие коллекции без блокировки.
        /// </summary>
        /// <param name="cancellationToken">Токен отмены операции</param>
        /// <returns>Асинхронный поток элементов очереди</returns>
        IAsyncEnumerable<QueueItem> GetAllAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Получает асинхронный поток только ожидающих обработки элементов.
        /// Используется процессором для получения следующих задач.
        /// </summary>
        /// <param name="cancellationToken">Токен отмены операции</param>
        /// <returns>Асинхронный поток ожидающих элементов</returns>
        IAsyncEnumerable<QueueItem> GetPendingAsync(CancellationToken cancellationToken = default);
        
        // ===== ОСНОВНЫЕ ОПЕРАЦИИ =====
        
        /// <summary>
        /// Добавляет новый элемент в асинхронное хранилище.
        /// Автоматически устанавливает временные метки и начальный статус.
        /// </summary>
        /// <param name="item">Добавляемый элемент</param>
        /// <param name="cancellationToken">Токен отмены операции</param>
        Task AddAsync(QueueItem item, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Обновляет существующий элемент в хранилище.
        /// Поддерживает конкурентные обновления с автоматическим разрешением конфликтов.
        /// </summary>
        /// <param name="item">Элемент с обновленными данными</param>
        /// <param name="cancellationToken">Токен отмены операции</param>
        Task UpdateAsync(QueueItem item, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Удаляет элемент из хранилища по идентификатору.
        /// Безопасно удаляет элемент даже во время обработки.
        /// </summary>
        /// <param name="id">Идентификатор удаляемого элемента</param>
        /// <param name="cancellationToken">Токен отмены операции</param>
        Task RemoveAsync(Guid id, CancellationToken cancellationToken = default);
        
        // ===== СПЕЦИАЛИЗИРОВАННЫЕ ОПЕРАЦИИ =====
        
        /// <summary>
        /// Пытается зарезервировать элемент для эксклюзивной обработки.
        /// Предотвращает одновременную обработку одного элемента несколькими процессорами.
        /// </summary>
        /// <param name="id">Идентификатор элемента для резервации</param>
        /// <param name="cancellationToken">Токен отмены операции</param>
        /// <returns>True если элемент успешно зарезервирован, иначе false</returns>
        Task<bool> TryReserveAsync(Guid id, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Завершает обработку элемента с установкой финального статуса.
        /// Оптимизированная операция для завершения процесса конвертации.
        /// </summary>
        /// <param name="id">Идентификатор завершаемого элемента</param>
        /// <param name="finalStatus">Финальный статус обработки</param>
        /// <param name="errorMessage">Сообщение об ошибке (если есть)</param>
        /// <param name="outputFileSizeBytes">Размер выходного файла в байтах</param>
        /// <param name="completedAt">Время завершения обработки</param>
        /// <param name="cancellationToken">Токен отмены операции</param>
        Task CompleteAsync(Guid id, ConversionStatus finalStatus, string? errorMessage = null, long? outputFileSizeBytes = null, DateTime? completedAt = null, CancellationToken cancellationToken = default);
    }
}
