using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using Converter.Application.Models;

namespace Converter.Application.Abstractions
{
    /// <summary>
    /// Главное представление приложения Video Converter.
    /// Определяет контракт между презентером и пользовательским интерфейсом.
    /// Поддерживает как синхронные, так и асинхронные события для обеспечения
    /// обратной совместимости и гибкости архитектуры.
    /// </summary>
    public interface IMainView
    {
        // ===== СИНХРОННЫЕ СОБЫТИЯ (для обратной совместимости) =====
        
        /// <summary>Событие запроса на добавление файлов</summary>
        event EventHandler AddFilesRequested;
        
        /// <summary>Событие запроса на запуск конвертации</summary>
        event EventHandler StartConversionRequested;
        
        /// <summary>Событие запроса на отмену конвертации</summary>
        event EventHandler CancelConversionRequested;
        
        /// <summary>Событие выбора пресета конвертации</summary>
        event EventHandler<Converter.Application.Models.ConversionProfile> PresetSelected;
        
        /// <summary>Событие изменения настроек приложения</summary>
        event EventHandler SettingsChanged;
        
        /// <summary>Событие перетаскивания файлов в область приложения</summary>
        event EventHandler<string[]>? FilesDropped;
        
        /// <summary>Событие запроса на удаление выбранных файлов</summary>
        event EventHandler? RemoveSelectedFilesRequested;
        
        /// <summary>Событие запроса на очистку всех файлов</summary>
        event EventHandler? ClearAllFilesRequested;

        // ===== АСИНХРОННЫЕ СОБЫТИЯ =====
        
        /// <summary>Асинхронное событие запроса на добавление файлов</summary>
        event Func<Task> AddFilesRequestedAsync;
        
        /// <summary>Асинхронное событие запроса на запуск конвертации</summary>
        event Func<Task> StartConversionRequestedAsync;
        
        /// <summary>Асинхронное событие запроса на отмену конвертации</summary>
        event Func<Task> CancelConversionRequestedAsync;
        
        /// <summary>Асинхронное событие перетаскивания файлов</summary>
        event Func<string[], Task> FilesDroppedAsync;
        
        /// <summary>Асинхронное событие запроса на удаление выбранных файлов</summary>
        event Func<Task> RemoveSelectedFilesRequestedAsync;
        
        /// <summary>Асинхронное событие запроса на очистку всех файлов</summary>
        event Func<Task>? ClearAllFilesRequestedAsync;

        // ===== ОСНОВНЫЕ СВОЙСТВА =====
        
        /// <summary>Путь к исполняемому файлу FFmpeg</summary>
        string FfmpegPath { get; set; }
        
        /// <summary>Папка для сохранения результатов конвертации</summary>
        string OutputFolder { get; set; }
        
        /// <summary>Выбранный шаблон именования выходных файлов</summary>
        string? NamingPattern { get; set; }
        
        /// <summary>Коллекция доступных пресетов конвертации</summary>
        ObservableCollection<Converter.Application.Models.ConversionProfile> AvailablePresets { get; set; }
        
        /// <summary>Текущий выбранный пресет конвертации</summary>
        Converter.Application.Models.ConversionProfile? SelectedPreset { get; set; }

        // ===== MVVM/MVP СВОЙСТВА ПРИВЯЗКИ =====
        
        /// <summary>Привязанная коллекция элементов очереди (единый источник правды)</summary>
        BindingList<ViewModels.QueueItemViewModel>? QueueItemsBinding { get; set; }
        
        /// <summary>Индикатор занятости интерфейса (для блокировки UI)</summary>
        bool IsBusy { get; set; }
        
        /// <summary>Текст статуса для отображения в интерфейсе</summary>
        string StatusText { get; set; }
        
        /// <summary>Общий прогресс конвертации</summary>
        int TotalProgress { get; set; }

        // ===== МЕТОДЫ УВЕДОМЛЕНИЙ =====
        
        /// <summary>Отображает сообщение об ошибке пользователю</summary>
        /// <param name="message">Текст сообщения об ошибке</param>
        void ShowError(string message);
        
        /// <summary>Отображает информационное сообщение пользователю</summary>
        /// <param name="message">Текст информационного сообщения</param>
        void ShowInfo(string message);
        
        /// <summary>Добавляет сообщение в лог UI</summary>
        /// <param name="message">Текст сообщения для лога</param>
        void AppendLog(string message);

        // ===== ОТСЛЕЖИВАНИЕ ПРОГРЕССА =====
        
        /// <summary>Обновляет прогресс текущего элемента конвертации</summary>
        /// <param name="percent">Процент выполнения (0-100)</param>
        void UpdateCurrentProgress(int percent);
        
        /// <summary>Обновляет общий прогресс всей очереди конвертации</summary>
        /// <param name="percent">Общий процент выполнения (0-100)</param>
        void UpdateTotalProgress(int percent);

        /// <summary>Устанавливает ссылку на главный презентер</summary>
        /// <param name="presenter">Объект презентера</param>
        void SetMainPresenter(object presenter);

        // ===== УТИЛИТАРНЫЕ МЕТОДЫ ДЛЯ АСИНХРОННОГО ВЫЗОВА СОБЫТИЙ =====
        
        /// <summary>Асинхронно вызывает событие AddFilesRequested</summary>
        Task RaiseAddFilesRequestedAsync();
        
        /// <summary>Асинхронно вызывает событие StartConversionRequested</summary>
        Task RaiseStartConversionRequestedAsync();
        
        /// <summary>Асинхронно вызывает событие CancelConversionRequested</summary>
        Task RaiseCancelConversionRequestedAsync();
        
        /// <summary>Асинхронно вызывает событие FilesDropped</summary>
        /// <param name="files">Массив путей к перетащенным файлам</param>
        Task RaiseFilesDroppedAsync(string[] files);
        
        /// <summary>Асинхронно вызывает событие RemoveSelectedFilesRequested</summary>
        Task RaiseRemoveSelectedFilesRequestedAsync();
        
        /// <summary>Асинхронно вызывает событие ClearAllFilesRequested</summary>
        Task RaiseClearAllFilesRequestedAsync();

        /// <summary>Обновляет статус, связанный с FFmpeg (например, прогресс загрузки).</summary>
        /// <param name="message">Текст статуса.</param>
        void UpdateFfmpegStatus(string message);

        /// <summary>
        /// Выполняет указанное действие в контексте UI-потока представления.
        /// Реализация обязана обеспечить корректную маршализацию вызова
        /// в UI-поток, если это необходимо.
        /// </summary>
        /// <param name="action">Действие, которое необходимо выполнить в UI-потоке.</param>
        void RunOnUiThread(Action action);

        /// <summary>
        /// Переводит UI в состояние ожидания расчёта оценки конвертации.
        /// Используется для отображения состояния "расчёт" в панели оценки.
        /// </summary>
        void ShowEstimateCalculating();

        /// <summary>
        /// Отображает рассчитанную совокупную оценку конвертации для текущей партии файлов.
        /// </summary>
        /// <param name="estimate">Совокупная оценка конвертации.</param>
        void ShowEstimate(ConversionEstimate estimate);

        void RemoveFileFromQueue(string filePath);
    }
}