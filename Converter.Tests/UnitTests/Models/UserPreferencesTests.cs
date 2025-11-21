using Xunit;
using Converter.Domain.Models;

namespace Converter.Tests.UnitTests.Models;

public class UserPreferencesTests
{
    [Fact]
    public void UserPreferences_ShouldPersistTheme()
    {
        // Arrange & Act
        var preferences = new UserPreferences
        {
            ThemeName = "dark",
            AutoSwitchEnabled = true,
            DarkModeStart = new TimeSpan(20, 0, 0),
            DarkModeEnd = new TimeSpan(7, 0, 0),
            PreferredDarkTheme = "midnight"
        };
        
        // Assert
        Assert.Equal("dark", preferences.ThemeName);
        Assert.True(preferences.AutoSwitchEnabled);
        Assert.Equal(new TimeSpan(20, 0, 0), preferences.DarkModeStart);
        Assert.Equal(new TimeSpan(7, 0, 0), preferences.DarkModeEnd);
        Assert.Equal("midnight", preferences.PreferredDarkTheme);
    }

    [Fact]
    public void UserPreferences_ShouldStoreRecentFolders()
    {
        // Arrange
        var preferences = new UserPreferences();
        
        // Act
        preferences.RecentFolders.Add("/videos/work");
        preferences.RecentFolders.Add("/videos/personal");
        preferences.RecentFolders.Add("/downloads/temp");
        preferences.RecentFiles.Add("movie1.mp4");
        preferences.RecentFiles.Add("clip2.avi");
        preferences.MaxRecentItems = 5;
        
        // Assert
        Assert.Equal(3, preferences.RecentFolders.Count);
        Assert.Equal("/videos/work", preferences.RecentFolders[0]);
        Assert.Equal("/videos/personal", preferences.RecentFolders[1]);
        Assert.Equal("/downloads/temp", preferences.RecentFolders[2]);
        
        Assert.Equal(2, preferences.RecentFiles.Count);
        Assert.Equal("movie1.mp4", preferences.RecentFiles[0]);
        Assert.Equal("clip2.avi", preferences.RecentFiles[1]);
        
        Assert.Equal(5, preferences.MaxRecentItems);
    }

    [Fact]
    public void UserPreferences_ShouldTrackNotifications()
    {
        // Arrange & Act
        var preferences = new UserPreferences
        {
            EnableDesktopNotifications = true,
            EnableSoundNotifications = false,
            ShowProgressNotifications = true,
            PlaySoundOnCompletion = false,
            CustomNotificationSound = "/sounds/custom.wav"
        };
        
        // Assert
        Assert.True(preferences.EnableDesktopNotifications);
        Assert.False(preferences.EnableSoundNotifications);
        Assert.True(preferences.ShowProgressNotifications);
        Assert.False(preferences.PlaySoundOnCompletion);
        Assert.Equal("/sounds/custom.wav", preferences.CustomNotificationSound);
    }

    [Fact]
    public void UserPreferences_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var preferences = new UserPreferences();
        
        // Assert
        Assert.Null(preferences.ThemeName);
        Assert.True(preferences.EnableAnimations);
        Assert.Equal(300, preferences.AnimationDuration);
        Assert.False(preferences.AutoSwitchEnabled);
        Assert.Equal(new TimeSpan(20, 0, 0), preferences.DarkModeStart);
        Assert.Equal(new TimeSpan(7, 0, 0), preferences.DarkModeEnd);
        Assert.Equal("dark", preferences.PreferredDarkTheme);
        
        Assert.False(preferences.DeleteSourceAfterConversion);
        Assert.False(preferences.OverwriteExistingFiles);
        Assert.True(preferences.CreateSubfoldersByPreset);
        Assert.Null(preferences.LastUsedOutputFolder);
        
        Assert.NotNull(preferences.RecentFolders);
        Assert.Empty(preferences.RecentFolders);
        Assert.NotNull(preferences.RecentFiles);
        Assert.Empty(preferences.RecentFiles);
        Assert.Equal(10, preferences.MaxRecentItems);
        
        Assert.True(preferences.EnableDesktopNotifications);
        Assert.True(preferences.EnableSoundNotifications);
        Assert.True(preferences.ShowProgressNotifications);
        Assert.True(preferences.PlaySoundOnCompletion);
        Assert.Null(preferences.CustomNotificationSound);
        
        Assert.False(preferences.AutoStartQueue);
        Assert.True(preferences.PauseOnError);
        Assert.True(preferences.ContinueOnWarning);
        
        Assert.True(preferences.ShowFileSizes);
        Assert.True(preferences.ShowConversionEstimates);
        Assert.False(preferences.ShowAdvancedOptions);
        Assert.Equal(1200, preferences.WindowWidth);
        Assert.Equal(800, preferences.WindowHeight);
        Assert.False(preferences.WindowMaximized);
        
        Assert.Null(preferences.DefaultPreset);
        Assert.True(preferences.RememberLastPreset);
    }

    [Fact]
    public void UserPreferences_Clone_ShouldCreateIndependentCopy()
    {
        // Arrange
        var original = new UserPreferences
        {
            ThemeName = "midnight",
            EnableAnimations = false,
            AutoSwitchEnabled = true,
            RecentFolders = new List<string> { "/folder1", "/folder2" },
            RecentFiles = new List<string> { "file1.mp4" },
            MaxRecentItems = 20,
            WindowWidth = 1400,
            WindowHeight = 900
        };
        
        // Act
        var clone = original.Clone();
        
        // Modify original
        original.ThemeName = "light";
        original.EnableAnimations = true;
        original.RecentFolders.Clear();
        original.RecentFolders.Add("/newfolder");
        original.WindowWidth = 1600;
        
        // Assert - Clone should have original values
        Assert.Equal("midnight", clone.ThemeName);
        Assert.False(clone.EnableAnimations);
        Assert.True(clone.AutoSwitchEnabled);
        Assert.Equal(2, clone.RecentFolders.Count);
        Assert.Equal("/folder1", clone.RecentFolders[0]);
        Assert.Equal("/folder2", clone.RecentFolders[1]);
        Assert.Equal(1, clone.RecentFiles.Count);
        Assert.Equal("file1.mp4", clone.RecentFiles[0]);
        Assert.Equal(20, clone.MaxRecentItems);
        Assert.Equal(1400, clone.WindowWidth);
        Assert.Equal(900, clone.WindowHeight);
        
        // Assert - Original should be modified
        Assert.Equal("light", original.ThemeName);
        Assert.True(original.EnableAnimations);
        Assert.Equal(1, original.RecentFolders.Count);
        Assert.Equal("/newfolder", original.RecentFolders[0]);
        Assert.Equal(1600, original.WindowWidth);
    }

    [Fact]
    public void UserPreferences_ShouldHandleRecentItemsManagement()
    {
        // Arrange
        var preferences = new UserPreferences
        {
            MaxRecentItems = 3
        };
        
        // Act - Add more items than the limit
        preferences.RecentFolders.Add("/folder1");
        preferences.RecentFolders.Add("/folder2");
        preferences.RecentFolders.Add("/folder3");
        preferences.RecentFolders.Add("/folder4"); // Should exceed limit
        
        preferences.RecentFiles.Add("file1.mp4");
        preferences.RecentFiles.Add("file2.avi");
        preferences.RecentFiles.Add("file3.mkv");
        preferences.RecentFiles.Add("file4.wmv"); // Should exceed limit
        
        // Assert - All items should be added (user is responsible for managing the limit)
        Assert.Equal(4, preferences.RecentFolders.Count);
        Assert.Equal(4, preferences.RecentFiles.Count);
        Assert.Equal(3, preferences.MaxRecentItems);
    }

    [Fact]
    public void UserPreferences_ShouldHandleEmptyLists()
    {
        // Arrange & Act
        var preferences = new UserPreferences();
        
        // Assert
        Assert.NotNull(preferences.RecentFolders);
        Assert.NotNull(preferences.RecentFiles);
        Assert.Empty(preferences.RecentFolders);
        Assert.Empty(preferences.RecentFiles);
    }

    [Fact]
    public void UserPreferences_ShouldHandleTimeSpans()
    {
        // Arrange & Act
        var preferences = new UserPreferences
        {
            DarkModeStart = new TimeSpan(18, 30, 0), // 6:30 PM
            DarkModeEnd = new TimeSpan(6, 0, 0)      // 6:00 AM
        };
        
        // Assert
        Assert.Equal(new TimeSpan(18, 30, 0), preferences.DarkModeStart);
        Assert.Equal(new TimeSpan(6, 0, 0), preferences.DarkModeEnd);
    }

    [Fact]
    public void UserPreferences_ShouldHandleWindowSettings()
    {
        // Arrange & Act
        var preferences = new UserPreferences
        {
            WindowWidth = 1920,
            WindowHeight = 1080,
            WindowMaximized = true
        };
        
        // Assert
        Assert.Equal(1920, preferences.WindowWidth);
        Assert.Equal(1080, preferences.WindowHeight);
        Assert.True(preferences.WindowMaximized);
        
        // Test minimum valid sizes
        preferences.WindowWidth = 800;
        preferences.WindowHeight = 600;
        preferences.WindowMaximized = false;
        
        Assert.Equal(800, preferences.WindowWidth);
        Assert.Equal(600, preferences.WindowHeight);
        Assert.False(preferences.WindowMaximized);
    }

    [Fact]
    public void UserPreferences_ShouldHandleQueuePreferences()
    {
        // Arrange & Act
        var preferences = new UserPreferences
        {
            AutoStartQueue = true,
            PauseOnError = false,
            ContinueOnWarning = false
        };
        
        // Assert
        Assert.True(preferences.AutoStartQueue);
        Assert.False(preferences.PauseOnError);
        Assert.False(preferences.ContinueOnWarning);
    }

    [Fact]
    public void UserPreferences_ShouldHandleUIPreferences()
    {
        // Arrange & Act
        var preferences = new UserPreferences
        {
            ShowFileSizes = false,
            ShowConversionEstimates = false,
            ShowAdvancedOptions = true
        };
        
        // Assert
        Assert.False(preferences.ShowFileSizes);
        Assert.False(preferences.ShowConversionEstimates);
        Assert.True(preferences.ShowAdvancedOptions);
    }

    [Fact]
    public void UserPreferences_ShouldHandlePresetPreferences()
    {
        // Arrange & Act
        var preferences = new UserPreferences
        {
            DefaultPreset = "YouTube HD",
            RememberLastPreset = false
        };
        
        // Assert
        Assert.Equal("YouTube HD", preferences.DefaultPreset);
        Assert.False(preferences.RememberLastPreset);
    }

    [Fact]
    public void UserPreferences_ShouldHandleFileHandlingPreferences()
    {
        // Arrange & Act
        var preferences = new UserPreferences
        {
            DeleteSourceAfterConversion = true,
            OverwriteExistingFiles = true,
            CreateSubfoldersByPreset = false,
            LastUsedOutputFolder = "/custom/output/path"
        };
        
        // Assert
        Assert.True(preferences.DeleteSourceAfterConversion);
        Assert.True(preferences.OverwriteExistingFiles);
        Assert.False(preferences.CreateSubfoldersByPreset);
        Assert.Equal("/custom/output/path", preferences.LastUsedOutputFolder);
    }

    [Fact]
    public void UserPreferences_ShouldHandleAnimationSettings()
    {
        // Arrange & Act
        var preferences = new UserPreferences
        {
            EnableAnimations = false,
            AnimationDuration = 0
        };
        
        // Assert
        Assert.False(preferences.EnableAnimations);
        Assert.Equal(0, preferences.AnimationDuration);
        
        // Test maximum animation duration
        preferences.AnimationDuration = 2000;
        Assert.Equal(2000, preferences.AnimationDuration);
    }

    [Fact]
    public void UserPreferences_ShouldHandleEdgeCaseValues()
    {
        // Arrange & Act
        var preferences = new UserPreferences
        {
            MaxRecentItems = 0, // Minimum
            AnimationDuration = -1, // Invalid but should be stored
            WindowWidth = -100, // Invalid but should be stored
            WindowHeight = -50  // Invalid but should be stored
        };
        
        // Assert - Values should be stored as-is
        Assert.Equal(0, preferences.MaxRecentItems);
        Assert.Equal(-1, preferences.AnimationDuration);
        Assert.Equal(-100, preferences.WindowWidth);
        Assert.Equal(-50, preferences.WindowHeight);
    }
}
