using System;
using System.Threading.Tasks;
using Converter.Application.Abstractions;
using Converter.Models;
using Converter.Services;

namespace Converter.Application.Services;

public class ThemeService : IThemeService
{
    private readonly ISettingsStore _settingsStore;
    private readonly IThemeManager _themeManager;

    public event EventHandler<Theme>? ThemeChanged;

    public Theme CurrentTheme => _themeManager.CurrentTheme;

    public ThemeService(ISettingsStore settingsStore, IThemeManager themeManager)
    {
        _settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
        _themeManager = themeManager ?? throw new ArgumentNullException(nameof(themeManager));
        _themeManager.ThemeChanged += OnManagerThemeChanged;
        _ = InitializeFromSettingsAsync();
    }

    private async Task InitializeFromSettingsAsync()
    {
        try
        {
            var preferences = await _settingsStore.GetUserPreferencesAsync();
            if (!string.IsNullOrWhiteSpace(preferences.ThemeName))
            {
                var target = Theme.GetAllThemes().FirstOrDefault(t => t.Name == preferences.ThemeName);
                if (target != null)
                {
                    _themeManager.SetTheme(target, animate: false);
                }
            }
        }
        catch (Exception ex)
        {
            // Log error but don't crash the app
            System.Diagnostics.Debug.WriteLine($"Error loading theme: {ex.Message}");
        }
    }

    public async Task SetTheme(Theme theme, bool animate = true)
    {
        if (theme == null) throw new ArgumentNullException(nameof(theme));
        
        _themeManager.SetTheme(theme, animate);
        
        // Save the theme name in user preferences
        var preferences = await _settingsStore.GetUserPreferencesAsync();
        preferences.ThemeName = theme.Name;
        await _settingsStore.SaveUserPreferencesAsync(preferences);
    }

    public void ApplyTheme(Control control)
    {
        if (control is Form form)
        {
            // Для форм используем перегрузку с Form
            _themeManager.ApplyTheme(form);
        }
        else
        {
            // Для обычных контролов применяем тему к родительской форме
            var parentForm = control.FindForm();
            if (parentForm != null)
            {
                _themeManager.ApplyTheme(parentForm);
            }
        }
    }

    private void OnManagerThemeChanged(object? sender, Theme theme)
    {
        ThemeChanged?.Invoke(this, theme);
    }

    public void Dispose()
    {
        _themeManager.ThemeChanged -= OnManagerThemeChanged;
    }
}
