namespace Converter.Application.Abstractions;

using System;
using System.Windows.Forms; // Added namespace for Control class
using System.Threading;
using System.Threading.Tasks;
using Converter.Models;

public interface IThemeService : IDisposable
{
    event EventHandler<Theme>? ThemeChanged;
    Theme CurrentTheme { get; }

    // Animation settings
    bool EnableAnimations { get; set; }
    int AnimationDuration { get; set; }

    // Auto-switch settings
    bool AutoSwitchEnabled { get; set; }
    TimeSpan DarkModeStart { get; set; }
    TimeSpan DarkModeEnd { get; set; }
    string PreferredDarkTheme { get; set; }

    /// <summary>
    /// Applies the current theme to the specified control and its children
    /// </summary>
    /// <param name="control">The control to apply the theme to</param>
    void ApplyTheme(Control control);
    
    /// <summary>
    /// Устанавливает указанную тему (с возможной анимации на уровне ThemeManager/UI).
    /// </summary>
    Task SetTheme(Theme theme, bool animate = true);
    
    /// <summary>
    /// Enable or disable automatic theme switching based on time of day
    /// </summary>
    Task EnableAutoSwitchAsync(bool enable, CancellationToken ct = default);
}
