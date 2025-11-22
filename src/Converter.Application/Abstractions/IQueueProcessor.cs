using Converter.Domain.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Converter.Application.Abstractions
{
    /// <summary>
    /// Процессор очереди конвертации.
    /// Управляет выполнением операций конвертации из очереди, обеспечивая
    /// последовательную или параллельную обработку элементов с поддержкой
    /// паузы, отмены и отслеживания прогресса.
    /// </summary>
    public interface IQueueProcessor
    {
        // ===== СОБЫТИЯ ПРОЦЕССОРА =====
        
        /// <summary>Событие начала обработки элемента очереди</summary>
        event EventHandler<QueueItem> ItemStarted;
        
        /// <summary>Событие успешного завершения элемента очереди</summary>
        event EventHandler<QueueItem> ItemCompleted;
        
        /// <summary>Событие неудачного завершения элемента очереди</summary>
        event EventHandler<QueueItem> ItemFailed;
        
        /// <summary>Событие изменения прогресса обработки элемента</summary>
        event EventHandler<QueueProgressEventArgs> ProgressChanged;
        
        /// <summary>Событие завершения всей очереди</summary>
        event EventHandler QueueCompleted;
        
        // ===== СОСТОЯНИЕ ПРОЦЕССОРА =====
        
        /// <summary>Указывает, выполняется ли обработка очереди</summary>
        bool IsRunning { get; }
        
        /// <summary>Указывает, приостановлена ли обработка очереди</summary>
        bool IsPaused { get; }
        
        // ===== УПРАВЛЕНИЕ ПРОЦЕССОРОМ =====
        
        /// <summary>
        /// Запускает обработку очереди конвертации.
        /// Начинает последовательную или параллельную обработку элементов.
        /// </summary>
        /// <param name="cancellationToken">Токен отмены операции</param>
        Task StartProcessingAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Полностью останавливает обработку очереди.
        /// Отменяет все текущие операции и сбрасывает состояние.
        /// </summary>
        Task StopProcessingAsync();
        
        /// <summary>
        /// Приостанавливает обработку очереди.
        /// Текущие операции завершаются, новые не начинаются.
        /// </summary>
        Task PauseProcessingAsync();
        
        /// <summary>
        /// Возобновляет приостановленную обработку очереди.
        /// Продолжает с того места, где была остановка.
        /// </summary>
        Task ResumeProcessingAsync();
        
        /// <summary>
        /// Обрабатывает отдельный элемент очереди синхронно.
        /// Используется для отладки или ручной обработки.
        /// </summary>
        /// <param name="item">Элемент очереди для обработки</param>
        /// <param name="cancellationToken">Токен отмены операции</param>
        Task ProcessItemAsync(QueueItem item, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Добавляет элемент в очередь на обработку.
        /// Реализация на базе Channel должна быть потокобезопасной и не блокирующей UI.
        /// </summary>
        /// <param name="item">Элемент очереди</param>
        /// <param name="cancellationToken">Токен отмены операции</param>
        Task EnqueueAsync(QueueItem item, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Возвращает все элементы очереди как асинхронный поток.
        /// </summary>
        System.Collections.Generic.IAsyncEnumerable<QueueItem> GetItemsAsync(CancellationToken cancellationToken = default);
        

    }

    /// <summary>
    /// Аргументы события изменения прогресса очереди.
    /// Содержит информацию о текущем состоянии обработки элемента.
    /// </summary>
    public class QueueProgressEventArgs : EventArgs
    {
        /// <summary>Элемент очереди, о котором сообщается прогресс</summary>
        public QueueItem Item { get; }
        
        /// <summary>Текущий процент выполнения (0-100)</summary>
        public int Progress { get; }
        
        /// <summary>Текстовое описание текущего статуса</summary>
        public string Status { get; }

        /// <summary>
        /// Создает новый экземпляр аргументов события прогресса.
        /// </summary>
        /// <param name="item">Элемент очереди</param>
        /// <param name="progress">Процент выполнения</param>
        /// <param name="status">Статус операции</param>
        public QueueProgressEventArgs(QueueItem item, int progress, string status = null)
        {
            Item = item ?? throw new ArgumentNullException(nameof(item));
            Progress = progress;
            Status = status;
        }
    }
}
