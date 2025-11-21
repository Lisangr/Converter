namespace Converter.Domain.Models;

public class UserPreferences
{
    // Theme settings
    public string? ThemeName { get; set; }
    
    // Animation settings
    public bool EnableAnimations { get; set; } = true;
    public int AnimationDuration { get; set; } = 300;
    
    // Auto-switch settings
    public bool AutoSwitchEnabled { get; set; } = false;
    public TimeSpan DarkModeStart { get; set; } = new TimeSpan(20, 0, 0);
    public TimeSpan DarkModeEnd { get; set; } = new TimeSpan(7, 0, 0);
    public string PreferredDarkTheme { get; set; } = "dark";
    
    // File handling preferences
    public bool DeleteSourceAfterConversion { get; set; } = false;
    public bool OverwriteExistingFiles { get; set; } = false;
    public bool CreateSubfoldersByPreset { get; set; } = true;
    public string? LastUsedOutputFolder { get; set; }
    
    // Recent files and folders
    public List<string> RecentFolders { get; set; } = new();
    public List<string> RecentFiles { get; set; } = new();
    public int MaxRecentItems { get; set; } = 10;
    
    // Notification preferences
    public bool EnableDesktopNotifications { get; set; } = true;
    public bool EnableSoundNotifications { get; set; } = true;
    public bool ShowProgressNotifications { get; set; } = true;
    public bool PlaySoundOnCompletion { get; set; } = true;
    public string? CustomNotificationSound { get; set; }
    
    // Queue preferences
    public bool AutoStartQueue { get; set; } = false;
    public bool PauseOnError { get; set; } = true;
    public bool ContinueOnWarning { get; set; } = true;
    
    // UI preferences
    public bool ShowFileSizes { get; set; } = true;
    public bool ShowConversionEstimates { get; set; } = true;
    public bool ShowAdvancedOptions { get; set; } = false;
    public int WindowWidth { get; set; } = 1200;
    public int WindowHeight { get; set; } = 800;
    public bool WindowMaximized { get; set; } = false;
    
    // Preset preferences
    public string? DefaultPreset { get; set; }
    public bool RememberLastPreset { get; set; } = true;
    
    public UserPreferences Clone()
    {
        return new UserPreferences
        {
            ThemeName = ThemeName,
            EnableAnimations = EnableAnimations,
            AnimationDuration = AnimationDuration,
            AutoSwitchEnabled = AutoSwitchEnabled,
            DarkModeStart = DarkModeStart,
            DarkModeEnd = DarkModeEnd,
            PreferredDarkTheme = PreferredDarkTheme,
            DeleteSourceAfterConversion = DeleteSourceAfterConversion,
            OverwriteExistingFiles = OverwriteExistingFiles,
            CreateSubfoldersByPreset = CreateSubfoldersByPreset,
            LastUsedOutputFolder = LastUsedOutputFolder,
            RecentFolders = new List<string>(RecentFolders),
            RecentFiles = new List<string>(RecentFiles),
            MaxRecentItems = MaxRecentItems,
            EnableDesktopNotifications = EnableDesktopNotifications,
            EnableSoundNotifications = EnableSoundNotifications,
            ShowProgressNotifications = ShowProgressNotifications,
            PlaySoundOnCompletion = PlaySoundOnCompletion,
            CustomNotificationSound = CustomNotificationSound,
            AutoStartQueue = AutoStartQueue,
            PauseOnError = PauseOnError,
            ContinueOnWarning = ContinueOnWarning,
            ShowFileSizes = ShowFileSizes,
            ShowConversionEstimates = ShowConversionEstimates,
            ShowAdvancedOptions = ShowAdvancedOptions,
            WindowWidth = WindowWidth,
            WindowHeight = WindowHeight,
            WindowMaximized = WindowMaximized,
            DefaultPreset = DefaultPreset,
            RememberLastPreset = RememberLastPreset
        };
    }
}