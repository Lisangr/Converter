using System;
using System.Windows.Forms;
using Converter.Models;

namespace Converter.Application.Abstractions
{
    public delegate void ThemeTransitionProgressEventHandler(object? sender, float progress);

    public interface IThemeManager : IDisposable
    {
        Theme CurrentTheme { get; }

        event EventHandler<Theme>? ThemeChanged;
        event ThemeTransitionProgressEventHandler? ThemeTransitionProgress;

        void SetTheme(Theme theme, bool animate = true);
        void ApplyTheme(Form form);
    }
}
