namespace Converter.Application.Abstractions;

using System;
using System.Windows.Forms; // Added namespace for Control class
using Converter.Models;

public interface IThemeService : IDisposable
{
    event EventHandler<Theme>? ThemeChanged;
    Theme CurrentTheme { get; }
    /// <summary>
    /// Applies the current theme to the specified control and its children
    /// </summary>
    /// <param name="control">The control to apply the theme to</param>
    void ApplyTheme(Control control);
    /// <summary>
    /// Устанавливает указанную тему (с возможной анимации на уровне ThemeManager/UI).
    /// </summary>
    Task SetTheme(Theme theme, bool animate = true);
}
