using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Converter.Application.Abstractions;
using Converter.Application.Models;
using Converter.UI.Controls;
using FluentAssertions;
using Moq;
using Xunit;

namespace Converter.Tests.UnitTests.UI.Controls;

public class ThemeSelectorControlTests
{
    [Fact]
    public void ThemeSelector_ShouldSwitchThemes()
    {
        RunSta(async () =>
        {
            // Arrange
            var currentTheme = Theme.Light;
            var setThemeTcs = new TaskCompletionSource<Theme>();
            var serviceMock = CreateThemeServiceMock(() => currentTheme, t =>
            {
                currentTheme = t;
                setThemeTcs.TrySetResult(t);
                return Task.CompletedTask;
            });

            using var control = new ThemeSelectorControl(serviceMock.Object);
            var combo = GetField<ComboBox>(control, "_themeCombo");

            // Act
            combo.SelectedIndex = 1; // select dark theme
            await setThemeTcs.Task;

            // Assert
            currentTheme.Name.Should().Be(Theme.Dark.Name);
            serviceMock.Verify(s => s.SetTheme(It.Is<Theme>(t => t.Name == Theme.Dark.Name), true), Times.Once);
        });
    }

    [Fact]
    public void ThemeSelector_ShouldPersistSelection()
    {
        RunSta(() =>
        {
            // Arrange
            var currentTheme = Theme.NordDark;
            var serviceMock = CreateThemeServiceMock(() => currentTheme, _ => Task.CompletedTask);

            using var control = new ThemeSelectorControl(serviceMock.Object);
            var combo = GetField<ComboBox>(control, "_themeCombo");

            // Assert
            combo.SelectedItem.Should().Be(Theme.NordDark.DisplayName);
        });
    }

    [Fact]
    public void ThemeSelector_ShouldRespondToSystemChanges()
    {
        RunSta(() =>
        {
            // Arrange
            var currentTheme = Theme.Light;
            var serviceMock = CreateThemeServiceMock(() => currentTheme, _ => Task.CompletedTask);

            using var control = new ThemeSelectorControl(serviceMock.Object);
            var preview = GetField<Panel>(control, "_previewPanel");

            // Act
            currentTheme = Theme.Dark;
            serviceMock.Raise(s => s.ThemeChanged += null!, control, currentTheme);

            // Assert
            preview.BackColor.Should().Be(currentTheme["Background"]);
        });
    }

    private static Mock<IThemeService> CreateThemeServiceMock(
        Func<Theme> currentThemeProvider,
        Func<Theme, Task> onSetTheme)
    {
        var serviceMock = new Mock<IThemeService>();
        serviceMock.SetupGet(s => s.CurrentTheme).Returns(() => currentThemeProvider());
        serviceMock.SetupProperty(s => s.EnableAnimations, true);
        serviceMock.SetupProperty(s => s.AutoSwitchEnabled, false);
        serviceMock.Setup(s => s.SetTheme(It.IsAny<Theme>(), It.IsAny<bool>()))
            .Returns<Theme, bool>((theme, _) => onSetTheme(theme));
        serviceMock.Setup(s => s.EnableAutoSwitchAsync(It.IsAny<bool>(), default))
            .Returns(Task.CompletedTask);
        return serviceMock;
    }

    private static T GetField<T>(object target, string name)
    {
        return (T)(target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(target)
            ?? throw new InvalidOperationException());
    }

    private static void RunSta(Func<Task> action)
    {
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                action().GetAwaiter().GetResult();
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
