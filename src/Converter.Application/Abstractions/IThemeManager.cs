using System;
using System.Windows.Forms;
using Converter.Application.Models;

namespace Converter.Application.Abstractions;

/// <summary>
/// Менеджер тем интерфейса.
/// </summary>
public interface IThemeManager : IDisposable
{
    /// <summary>
    /// Текущая активная тема.
    /// </summary>
    Theme CurrentTheme { get; }

    /// <summary>
    /// Событие изменения темы.
    /// </summary>
    event EventHandler<Theme>? ThemeChanged;

    /// <summary>
    /// Событие прогресса перехода между темами.
    /// </summary>
    event ThemeTransitionProgressEventHandler? ThemeTransitionProgress;

    /// <summary>
    /// Устанавливает новую тему.
    /// </summary>
    /// <param name="theme">Новая тема</param>
    /// <param name="animate">Использовать анимацию</param>
    void SetTheme(Theme theme, bool animate = true);

    /// <summary>
    /// Применяет тему к форме.
    /// </summary>
    /// <param name="form">Форма для применения темы</param>
    void ApplyTheme(Form form);
}

/// <summary>
/// Делегат для события прогресса перехода темы.
/// </summary>
public delegate void ThemeTransitionProgressEventHandler(object? sender, float progress);