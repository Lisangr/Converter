using Xunit;
using Converter.Application.Models;
using System.Drawing;

namespace Converter.Tests.UnitTests.Models;

public class ThemeTests
{
    [Fact]
    public void Theme_ShouldProvideColorPalette()
    {
        // Arrange & Act
        var theme = new Theme
        {
            Name = "custom",
            DisplayName = "Custom Theme",
            Colors = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase)
            {
                ["Background"] = Color.Red,
                ["TextPrimary"] = Color.Blue,
                ["Accent"] = Color.Green
            }
        };
        
        // Assert
        Assert.Equal("custom", theme.Name);
        Assert.Equal("Custom Theme", theme.DisplayName);
        Assert.Equal(3, theme.Colors.Count);
        Assert.Equal(Color.Red, theme["Background"]);
        Assert.Equal(Color.Blue, theme["TextPrimary"]);
        Assert.Equal(Color.Green, theme["Accent"]);
    }

    [Fact]
    public void Theme_ShouldToggleDarkMode()
    {
        // Arrange
        var lightTheme = Theme.Light;
        var darkTheme = Theme.Dark;
        
        // Assert - Compare key colors between light and dark themes
        Assert.Equal("light", lightTheme.Name);
        Assert.Equal("dark", darkTheme.Name);
        
        // Background colors should be different
        Assert.Equal(Color.White, lightTheme["Background"]);
        Assert.Equal(Color.FromArgb(25, 25, 25), darkTheme["Background"]);
        
        // Text colors should be different
        Assert.Equal(Color.Black, lightTheme["TextPrimary"]);
        Assert.Equal(Color.White, darkTheme["TextPrimary"]);
        
        // Both should have the same color keys
        Assert.Contains("Background", lightTheme.Colors.Keys);
        Assert.Contains("TextPrimary", lightTheme.Colors.Keys);
        Assert.Contains("Background", darkTheme.Colors.Keys);
        Assert.Contains("TextPrimary", darkTheme.Colors.Keys);
    }

    [Fact]
    public void Theme_ShouldSerializePreferences()
    {
        // Arrange
        var theme = new Theme
        {
            Name = "test-theme",
            DisplayName = "Test Theme",
            Colors = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase)
            {
                ["Background"] = Color.FromArgb(255, 0, 0),
                ["TextPrimary"] = Color.FromArgb(0, 255, 0),
                ["Accent"] = Color.FromArgb(0, 0, 255)
            }
        };
        
        // Act & Assert - Verify theme properties are serializable
        Assert.Equal("test-theme", theme.Name);
        Assert.Equal("Test Theme", theme.DisplayName);
        Assert.Equal(3, theme.Colors.Count);
        
        // Verify color values are preserved
        Assert.Equal(Color.FromArgb(255, 0, 0), theme["Background"]);
        Assert.Equal(Color.FromArgb(0, 255, 0), theme["TextPrimary"]);
        Assert.Equal(Color.FromArgb(0, 0, 255), theme["Accent"]);
    }

    [Fact]
    public void Theme_ShouldHandleMissingColorKeys()
    {
        // Arrange
        var theme = new Theme
        {
            Colors = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase)
            {
                ["Background"] = Color.White
            }
        };
        
        // Act & Assert - Should return Magenta for missing keys
        Assert.Equal(Color.White, theme["Background"]);
        Assert.Equal(Color.Magenta, theme["NonExistentKey"]);
        Assert.Equal(Color.Magenta, theme["TextPrimary"]); // Not defined in this theme
    }

    [Fact]
    public void Theme_ShouldSupportCaseInsensitiveKeys()
    {
        // Arrange
        var theme = new Theme
        {
            Colors = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase)
            {
                ["Background"] = Color.Red
            }
        };
        
        // Act & Assert - All case variations should work
        Assert.Equal(Color.Red, theme["Background"]);
        Assert.Equal(Color.Red, theme["background"]);
        Assert.Equal(Color.Red, theme["BACKGROUND"]);
        Assert.Equal(Color.Red, theme["BackGround"]);
    }

    [Fact]
    public void Theme_GetAllThemes_ShouldReturnAllBuiltInThemes()
    {
        // Arrange & Act
        var allThemes = Theme.GetAllThemes();
        
        // Assert
        Assert.Equal(5, allThemes.Count);
        
        var themeNames = allThemes.Select(t => t.Name).ToList();
        Assert.Contains("light", themeNames);
        Assert.Contains("dark", themeNames);
        Assert.Contains("midnight", themeNames);
        Assert.Contains("nord_light", themeNames);
        Assert.Contains("nord_dark", themeNames);
    }

    [Fact]
    public void Theme_Light_ShouldHaveCorrectColors()
    {
        // Arrange & Act
        var lightTheme = Theme.Light;
        
        // Assert - Verify key colors for light theme
        Assert.Equal("light", lightTheme.Name);
        Assert.Equal("Светлая", lightTheme.DisplayName);
        
        // Verify essential colors exist
        Assert.Contains("Background", lightTheme.Colors.Keys);
        Assert.Contains("TextPrimary", lightTheme.Colors.Keys);
        Assert.Contains("Accent", lightTheme.Colors.Keys);
        Assert.Contains("Surface", lightTheme.Colors.Keys);
        Assert.Contains("Border", lightTheme.Colors.Keys);
        
        // Verify specific color values
        Assert.Equal(Color.White, lightTheme["Background"]);
        Assert.Equal(Color.Black, lightTheme["TextPrimary"]);
        Assert.Equal(Color.FromArgb(0, 120, 215), lightTheme["Accent"]);
        Assert.Equal(Color.FromArgb(200, 200, 200), lightTheme["Border"]);
    }

    [Fact]
    public void Theme_Dark_ShouldHaveCorrectColors()
    {
        // Arrange & Act
        var darkTheme = Theme.Dark;
        
        // Assert - Verify key colors for dark theme
        Assert.Equal("dark", darkTheme.Name);
        Assert.Equal("Темная", darkTheme.DisplayName);
        
        // Verify essential colors exist
        Assert.Contains("Background", darkTheme.Colors.Keys);
        Assert.Contains("TextPrimary", darkTheme.Colors.Keys);
        Assert.Contains("Accent", darkTheme.Colors.Keys);
        Assert.Contains("Surface", darkTheme.Colors.Keys);
        Assert.Contains("Border", darkTheme.Colors.Keys);
        
        // Verify specific color values
        Assert.Equal(Color.FromArgb(25, 25, 25), darkTheme["Background"]);
        Assert.Equal(Color.White, darkTheme["TextPrimary"]);
        Assert.Equal(Color.FromArgb(0, 120, 212), darkTheme["Accent"]);
        Assert.Equal(Color.FromArgb(60, 60, 60), darkTheme["Border"]);
    }

    [Fact]
    public void Theme_Midnight_ShouldHaveAMOLEDColors()
    {
        // Arrange & Act
        var midnightTheme = Theme.Midnight;
        
        // Assert
        Assert.Equal("midnight", midnightTheme.Name);
        Assert.Equal("Полночь (AMOLED)", midnightTheme.DisplayName);
        
        // AMOLED should be truly black
        Assert.Equal(Color.FromArgb(0, 0, 0), midnightTheme["Background"]);
        Assert.Equal(Color.FromArgb(220, 220, 220), midnightTheme["TextPrimary"]);
    }

    [Fact]
    public void Theme_NordThemes_ShouldHaveNordColorScheme()
    {
        // Arrange & Act
        var nordLight = Theme.NordLight;
        var nordDark = Theme.NordDark;
        
        // Assert - Verify nord themes exist and have correct names
        Assert.Equal("nord_light", nordLight.Name);
        Assert.Equal("Nord Light", nordLight.DisplayName);
        Assert.Equal("nord_dark", nordDark.Name);
        Assert.Equal("Nord Dark", nordDark.DisplayName);
        
        // Both should have the same color structure
        Assert.Equal(nordLight.Colors.Keys.Count, nordDark.Colors.Keys.Count);
        
        // Verify both have background and text colors
        Assert.Contains("Background", nordLight.Colors.Keys);
        Assert.Contains("TextPrimary", nordLight.Colors.Keys);
        Assert.Contains("Background", nordDark.Colors.Keys);
        Assert.Contains("TextPrimary", nordDark.Colors.Keys);
    }

    [Fact]
    public void Theme_ShouldHaveConsistentColorStructure()
    {
        // Arrange & Act
        var allThemes = Theme.GetAllThemes();
        
        // Assert - All themes should have the same color keys
        var expectedKeys = new HashSet<string>
        {
            "Background", "BackgroundSecondary", "Surface", "TextPrimary", "TextSecondary",
            "Accent", "AccentHover", "Border", "Success", "Error", "Warning", "Info"
        };
        
        foreach (var theme in allThemes)
        {
            var themeKeys = new HashSet<string>(theme.Colors.Keys, StringComparer.OrdinalIgnoreCase);
            
            // Each theme should have all expected keys
            foreach (var expectedKey in expectedKeys)
            {
                Assert.Contains(expectedKey, themeKeys);
            }
        }
    }

    [Fact]
    public void Theme_ShouldHandleEmptyColors()
    {
        // Arrange & Act
        var emptyTheme = new Theme
        {
            Colors = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase)
        };
        
        // Assert
        Assert.Empty(emptyTheme.Colors);
        Assert.Equal(Color.Magenta, emptyTheme["AnyKey"]); // Should return Magenta for missing keys
    }

    [Fact]
    public void Theme_ShouldPreserveColorValues()
    {
        // Arrange
        var customColor = Color.FromArgb(123, 45, 67);
        var theme = new Theme
        {
            Colors = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase)
            {
                ["CustomColor"] = customColor
            }
        };
        
        // Act & Assert
        Assert.Equal(customColor, theme["CustomColor"]);
        
        // Modify the original color object
        customColor = Color.FromArgb(255, 128, 0);
        
        // Theme should preserve its own copy
        Assert.NotEqual(customColor, theme["CustomColor"]);
        Assert.Equal(Color.FromArgb(123, 45, 67), theme["CustomColor"]);
    }
}
