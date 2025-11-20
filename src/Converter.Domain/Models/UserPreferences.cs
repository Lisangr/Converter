namespace Converter.Domain.Models;

public class UserPreferences
{
    public string? ThemeName { get; set; }
    
    // Animation settings
    public bool EnableAnimations { get; set; } = true;
    public int AnimationDuration { get; set; } = 300;
    
    // Auto-switch settings
    public bool AutoSwitchEnabled { get; set; } = false;
    public TimeSpan DarkModeStart { get; set; } = new TimeSpan(20, 0, 0);
    public TimeSpan DarkModeEnd { get; set; } = new TimeSpan(7, 0, 0);
    public string PreferredDarkTheme { get; set; } = "dark";
    
    // Add other user preferences here
    // For example:
    // public string? DefaultOutputFolder { get; set; }
}