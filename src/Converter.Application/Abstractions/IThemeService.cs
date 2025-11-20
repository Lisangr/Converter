namespace Converter.Application.Abstractions;

using System;
using System.Windows.Forms; // Added namespace for Control class
using System.Threading;
using System.Threading.Tasks;
using Converter.Models;

/// <summary>
/// Сервис управления темами с расширенными настройками.
/// Обеспечивает автоматическое переключение тем, управление анимациями
/// и персонализацию пользовательского интерфейса.
/// </summary>
public interface IThemeService : IDisposable
{
    // ===== СОБЫТИЯ =====
    
    /// <summary>Событие изменения темы</summary>
    event EventHandler<Theme>? ThemeChanged;
    
    // ===== СОСТОЯНИЕ =====
    
    /// <summary>Текущая активная тема</summary>
    Theme CurrentTheme { get; }

    // ===== НАСТРОЙКИ АНИМАЦИИ =====
    
    /// <summary>Включает или отключает анимации переходов между темами</summary>
    bool EnableAnimations { get; set; }
    
    /// <summary>Продолжительность анимации перехода в миллисекундах</summary>
    int AnimationDuration { get; set; }

    // ===== НАСТРОЙКИ АВТОПЕРЕКЛЮЧЕНИЯ =====
    
    /// <summary>Включает автоматическое переключение тем по времени суток</summary>
    bool AutoSwitchEnabled { get; set; }
    
    /// <summary>Время начала темного режима</summary>
    TimeSpan DarkModeStart { get; set; }
    
    /// <summary>Время окончания темного режима</summary>
    TimeSpan DarkModeEnd { get; set; }
    
    /// <summary>Предпочитаемая темная тема для автопереключения</summary>
    string PreferredDarkTheme { get; set; }

    // ===== ОСНОВНЫЕ ОПЕРАЦИИ =====
    
    /// <summary>
    /// Применяет текущую тему к указанному элементу управления и его дочерним элементам.
    /// Рекурсивно обрабатывает всю иерархию элементов управления.
    /// </summary>
    /// <param name="control">Элемент управления для применения темы</param>
    void ApplyTheme(Control control);
    
    /// <summary>
    /// Устанавливает указанную тему с возможностью анимированного перехода.
    /// Координирует с ThemeManager для обеспечения плавных переходов.
    /// </summary>
    /// <param name="theme">Новая тема для установки</param>
    /// <param name="animate">Использовать ли анимацию перехода</param>
    /// <returns>Асинхронная задача</returns>
    Task SetTheme(Theme theme, bool animate = true);
    
    /// <summary>
    /// Включает или отключает автоматическое переключение тем по времени суток.
    /// При активации автоматически переключает между светлой и темной темами.
    /// </summary>
    /// <param name="enable">Включить ли автопереключение</param>
    /// <param name="ct">Токен отмены операции</param>
    /// <returns>Асинхронная задача</returns>
    Task EnableAutoSwitchAsync(bool enable, CancellationToken ct = default);
}
