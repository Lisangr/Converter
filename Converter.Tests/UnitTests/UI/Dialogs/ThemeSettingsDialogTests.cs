using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using Converter.Application.Abstractions;
using Converter.Application.Models;
using Converter.UI.Dialogs;
using FluentAssertions;
using Moq;
using Xunit;

namespace Converter.Tests.UnitTests.UI.Dialogs;

public class ThemeSettingsDialogTests
{
    [Fact]
    public void ThemeSettingsDialog_ShouldListThemes()
    {
        RunSta(() =>
        {
            // Arrange
            var themeService = CreateThemeServiceMock(
                preferredDark: Theme.Midnight.Name,
                darkStart: TimeSpan.FromHours(22),
                darkEnd: TimeSpan.FromHours(7),
                animationDuration: 350);

            using var dialog = new ThemeSettingsDialog(themeService.Object);
            var combo = GetPrivateField<System.Windows.Forms.ComboBox>(dialog, "_comboDarkTheme");

            // Assert
            combo.Items.Cast<string>()
                .Should()
                .Contain(new[]
                {
                    Theme.Dark.DisplayName,
                    Theme.Midnight.DisplayName,
                    Theme.NordDark.DisplayName
                });

            combo.SelectedItem.Should().Be(Theme.Midnight.DisplayName);
            themeService.Object.AnimationDuration.Should().Be(350);
        });
    }

    [Fact]
    public void ThemeSettingsDialog_ShouldApplySelection()
    {
        RunSta(() =>
        {
            // Arrange
            var themeService = CreateThemeServiceMock(
                preferredDark: Theme.Dark.Name,
                darkStart: TimeSpan.FromHours(20),
                darkEnd: TimeSpan.FromHours(6),
                animationDuration: 300);

            using var dialog = new ThemeSettingsDialog(themeService.Object);
            var start = GetPrivateField<System.Windows.Forms.DateTimePicker>(dialog, "_timeDarkStart");
            var end = GetPrivateField<System.Windows.Forms.DateTimePicker>(dialog, "_timeDarkEnd");
            var speed = GetPrivateField<System.Windows.Forms.NumericUpDown>(dialog, "_numAnimationSpeed");
            var combo = GetPrivateField<System.Windows.Forms.ComboBox>(dialog, "_comboDarkTheme");

            var newStart = DateTime.Today.AddHours(19.5);
            var newEnd = DateTime.Today.AddHours(5);
            start.Value = newStart;
            end.Value = newEnd;
            speed.Value = 650;
            combo.SelectedItem = Theme.NordDark.DisplayName;

            // Act
            InvokePrivateMethod(dialog, "OnSave", dialog, EventArgs.Empty);

            // Assert
            themeService.Object.DarkModeStart.Should().BeCloseTo(newStart.TimeOfDay, TimeSpan.FromSeconds(1));
            themeService.Object.DarkModeEnd.Should().BeCloseTo(newEnd.TimeOfDay, TimeSpan.FromSeconds(1));
            themeService.Object.AnimationDuration.Should().Be(650);
            themeService.Object.PreferredDarkTheme.Should().Be(Theme.NordDark.Name);
        });
    }

    [Fact]
    public void ThemeSettingsDialog_ShouldPersistChoice()
    {
        RunSta(() =>
        {
            // Arrange
            var themeService = CreateThemeServiceMock(
                preferredDark: Theme.Dark.Name,
                darkStart: TimeSpan.FromHours(21),
                darkEnd: TimeSpan.FromHours(5),
                animationDuration: 400);

            using var dialog = new ThemeSettingsDialog(themeService.Object);
            var combo = GetPrivateField<System.Windows.Forms.ComboBox>(dialog, "_comboDarkTheme");

            // Act
            combo.SelectedItem = Theme.Midnight.DisplayName;
            InvokePrivateMethod(dialog, "OnSave", dialog, EventArgs.Empty);

            // Assert
            themeService.Object.PreferredDarkTheme.Should().Be(Theme.Midnight.Name);
        });
    }

    private static Mock<IThemeService> CreateThemeServiceMock(
        string preferredDark,
        TimeSpan darkStart,
        TimeSpan darkEnd,
        int animationDuration)
    {
        var mock = new Mock<IThemeService>();
        mock.SetupProperty(s => s.PreferredDarkTheme, preferredDark);
        mock.SetupProperty(s => s.DarkModeStart, darkStart);
        mock.SetupProperty(s => s.DarkModeEnd, darkEnd);
        mock.SetupProperty(s => s.AnimationDuration, animationDuration);
        mock.SetupProperty(s => s.AutoSwitchEnabled, false);
        mock.SetupProperty(s => s.EnableAnimations, true);
        mock.SetupGet(s => s.CurrentTheme).Returns(Theme.Dark);
        return mock;
    }

    private static T GetPrivateField<T>(object target, string name)
    {
        var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        return (T)(field?.GetValue(target) ?? throw new InvalidOperationException());
    }

    private static void InvokePrivateMethod(object target, string name, params object?[]? args)
    {
        var method = target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
        method?.Invoke(target, args);
    }

    private static void RunSta(Action action)
    {
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        })
        {
            IsBackground = true
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (exception != null)
        {
            throw exception;
        }
    }
}
