using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Converter.Application.Abstractions;
using Converter.Domain.Models;
using Converter.Models;
using Converter.Services;

namespace Converter.Application.Services;

public class ThemeService : IThemeService
{
    private readonly ISettingsStore _settingsStore;
    private readonly IThemeManager _themeManager;

    private UserPreferences _preferences = new();
    private System.Windows.Forms.Timer? _autoSwitchTimer;
    private DateTime _lastManualThemeChange = DateTime.MinValue;
    private readonly TimeSpan _manualChangeCooldown = TimeSpan.FromMinutes(5);

    public event EventHandler<Theme>? ThemeChanged;

    public Theme CurrentTheme => _themeManager.CurrentTheme;

    // Animation settings
    public bool EnableAnimations
    {
        get => _preferences.EnableAnimations;
        set
        {
            if (_preferences.EnableAnimations == value) return;
            _preferences.EnableAnimations = value;
            _ = SavePreferencesAsync();
        }
    }

    public int AnimationDuration
    {
        get => _preferences.AnimationDuration;
        set
        {
            var clamped = Math.Max(100, value);
            if (_preferences.AnimationDuration == clamped) return;
            _preferences.AnimationDuration = clamped;
            _ = SavePreferencesAsync();
        }
    }

    // Auto-switch settings
    public bool AutoSwitchEnabled
    {
        get => _preferences.AutoSwitchEnabled;
        set
        {
            if (_preferences.AutoSwitchEnabled == value) return;
            _preferences.AutoSwitchEnabled = value;
            _ = SavePreferencesAsync();
            _ = EnableAutoSwitchAsync(value);
        }
    }

    public TimeSpan DarkModeStart
    {
        get => _preferences.DarkModeStart;
        set
        {
            if (_preferences.DarkModeStart == value) return;
            _preferences.DarkModeStart = value;
            _ = SavePreferencesAsync();
        }
    }

    public TimeSpan DarkModeEnd
    {
        get => _preferences.DarkModeEnd;
        set
        {
            if (_preferences.DarkModeEnd == value) return;
            _preferences.DarkModeEnd = value;
            _ = SavePreferencesAsync();
        }
    }

    public string PreferredDarkTheme
    {
        get => _preferences.PreferredDarkTheme;
        set
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            if (string.Equals(_preferences.PreferredDarkTheme, value, StringComparison.OrdinalIgnoreCase)) return;
            _preferences.PreferredDarkTheme = value;
            _ = SavePreferencesAsync();
        }
    }

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
            _preferences = await _settingsStore.GetUserPreferencesAsync();
            
            // Load theme
            if (!string.IsNullOrWhiteSpace(_preferences.ThemeName))
            {
                var target = Theme.GetAllThemes().FirstOrDefault(t => t.Name == _preferences.ThemeName);
                if (target != null)
                {
                    _themeManager.SetTheme(target, animate: false);
                }
            }
            
            // Setup auto-switch if enabled
            if (AutoSwitchEnabled)
            {
                await EnableAutoSwitchAsync(true);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading theme settings: {ex.Message}");
        }
    }

    public async Task SetTheme(Theme theme, bool animate = true)
    {
        if (theme == null) throw new ArgumentNullException(nameof(theme));
        
        // Don't switch if already on this theme
        if (CurrentTheme != null && string.Equals(CurrentTheme.Name, theme.Name, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }
        
        _themeManager.SetTheme(theme, animate);
        
        // Remember manual theme change to prevent auto-switch for a while
        _lastManualThemeChange = DateTime.Now;
        
        // Save the theme name in user preferences
        _preferences.ThemeName = theme.Name;
        await SavePreferencesAsync();
    }

    public async Task EnableAutoSwitchAsync(bool enable, CancellationToken ct = default)
    {
        if (enable)
        {
            if (_autoSwitchTimer == null)
            {
                _autoSwitchTimer = new System.Windows.Forms.Timer { Interval = 60000 };
                _autoSwitchTimer.Tick += AutoSwitchTimerOnTick;
            }
            _autoSwitchTimer.Start();
            CheckAndAutoSwitch();
        }
        else
        {
            _autoSwitchTimer?.Stop();
        }
    }

    private void AutoSwitchTimerOnTick(object? sender, EventArgs e)
    {
        CheckAndAutoSwitch();
    }

    private void CheckAndAutoSwitch()
    {
        // Don't auto-switch if user manually changed theme recently
        var timeSinceManualChange = DateTime.Now - _lastManualThemeChange;
        if (timeSinceManualChange < _manualChangeCooldown)
        {
            return;
        }

        var now = DateTime.Now.TimeOfDay;
        var shouldBeDark = IsDarkModeTime(now);
        var targetThemeName = shouldBeDark ? PreferredDarkTheme : "light";
        var targetTheme = Theme.GetAllThemes().FirstOrDefault(t => t.Name == targetThemeName) ?? Theme.Light;

        if (CurrentTheme == null || !string.Equals(CurrentTheme.Name, targetTheme.Name, StringComparison.OrdinalIgnoreCase))
        {
            _themeManager.SetTheme(targetTheme, animate: EnableAnimations);
        }
    }

    private bool IsDarkModeTime(TimeSpan now)
    {
        if (DarkModeStart < DarkModeEnd)
        {
            return now >= DarkModeStart && now < DarkModeEnd;
        }

        return now >= DarkModeStart || now < DarkModeEnd;
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

    private async Task SavePreferencesAsync()
    {
        try
        {
            await _settingsStore.SaveUserPreferencesAsync(_preferences);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving theme preferences: {ex.Message}");
        }
    }

    private void OnManagerThemeChanged(object? sender, Theme theme)
    {
        ThemeChanged?.Invoke(this, theme);
    }

    public void Dispose()
    {
        _themeManager.ThemeChanged -= OnManagerThemeChanged;
        _autoSwitchTimer?.Stop();
        _autoSwitchTimer?.Dispose();
        _autoSwitchTimer = null;
    }
}
