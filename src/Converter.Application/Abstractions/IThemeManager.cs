using System;
using System.Windows.Forms;
using Converter.Models;

namespace Converter.Application.Abstractions
{
    public delegate void ThemeTransitionProgressEventHandler(object? sender, float progress);

    public interface IThemeManager : IDisposable
    {
        Theme CurrentTheme { get; }
        bool EnableAnimations { get; set; }
        int AnimationDuration { get; set; }
        bool AutoSwitchEnabled { get; }
        TimeSpan DarkModeStart { get; set; }
        TimeSpan DarkModeEnd { get; set; }
        string PreferredDarkTheme { get; set; }

        event EventHandler<Theme>? ThemeChanged;
        event ThemeTransitionProgressEventHandler? ThemeTransitionProgress;

        void SetTheme(Theme theme, bool animate = true);
        void ApplyTheme(Form form);
        void EnableAutoSwitch(bool enable);
    }
}
