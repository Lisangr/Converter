using System;
using System.Windows.Forms;
using Converter.Models;

namespace Converter.Application.Abstractions
{
    /// <summary>
    /// Менеджер тем интерфейса с поддержкой анимированных переходов.
    /// Отвечает за применение визуальных тем к формам и элементам управления,
    /// включая плавные анимации переходов между темами.
    /// </summary>
    public delegate void ThemeTransitionProgressEventHandler(object? sender, float progress);

    public interface IThemeManager : IDisposable
    {
        // ===== СОСТОЯНИЕ =====
        
        /// <summary>Текущая активная тема интерфейса</summary>
        Theme CurrentTheme { get; }

        // ===== СОБЫТИЯ =====
        
        /// <summary>Событие изменения темы</summary>
        event EventHandler<Theme>? ThemeChanged;
        
        /// <summary>Событие прогресса анимированного перехода между темами</summary>
        event ThemeTransitionProgressEventHandler? ThemeTransitionProgress;

        // ===== ОПЕРАЦИИ =====
        
        /// <summary>
        /// Устанавливает новую тему с возможностью анимированного перехода.
        /// Поддерживает плавную смену цветов и стилей интерфейса.
        /// </summary>
        /// <param name="theme">Новая тема для применения</param>
        /// <param name="animate">Использовать ли анимацию перехода</param>
        void SetTheme(Theme theme, bool animate = true);
        
        /// <summary>
        /// Применяет текущую тему к указанной форме и всем её элементам.
        /// Рекурсивно обрабатывает все дочерние элементы управления.
        /// </summary>
        /// <param name="form">Форма для применения темы</param>
        void ApplyTheme(Form form);
    }
}
