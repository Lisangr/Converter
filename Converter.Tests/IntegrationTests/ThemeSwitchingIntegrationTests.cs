using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Converter.Application.Abstractions;
using Converter.Application.Models;
using Converter.Application.Services;
using Converter.Domain.Models;
using Moq;
using Xunit;

namespace Converter.Tests.IntegrationTests;

public class ThemeSwitchingIntegrationTests
{
    [Fact]
    public void ThemeSwitch_ShouldUpdateUi()
    {
        var settings = new Mock<ISettingsStore>();
        settings.Setup(s => s.GetUserPreferencesAsync(default)).ReturnsAsync(new UserPreferences());
        var manager = new Mock<IThemeManager>();
        manager.SetupGet(m => m.CurrentTheme).Returns(Theme.Light);
        var service = new ThemeService(settings.Object, manager.Object);
        Theme? observed = null;
        service.ThemeChanged += (_, theme) => observed = theme;

        manager.Raise(m => m.ThemeChanged += null, Theme.Dark);

        Assert.Equal(Theme.Dark, observed);
    }

    [Fact]
    public async Task ThemeSwitch_ShouldPersistSelection()
    {
        var savedPreferences = default(UserPreferences);
        var settings = new Mock<ISettingsStore>();
        settings.Setup(s => s.GetUserPreferencesAsync(default)).ReturnsAsync(new UserPreferences());
        settings.Setup(s => s.SaveUserPreferencesAsync(It.IsAny<UserPreferences>(), default))
            .Callback<UserPreferences, System.Threading.CancellationToken>((prefs, _) => savedPreferences = prefs)
            .Returns(Task.CompletedTask);

        var manager = new Mock<IThemeManager>();
        manager.SetupGet(m => m.CurrentTheme).Returns((Theme?)null);
        var service = new ThemeService(settings.Object, manager.Object);
        var targetTheme = Theme.NordDark;

        await service.SetTheme(targetTheme);

        manager.Verify(m => m.SetTheme(targetTheme, true), Times.Once);
        Assert.Equal(targetTheme.Name, savedPreferences?.ThemeName);
    }

    [Fact]
    public void ThemeSwitch_ShouldRefreshDependentComponents()
    {
        var settings = new Mock<ISettingsStore>();
        settings.Setup(s => s.GetUserPreferencesAsync(default)).ReturnsAsync(new UserPreferences());
        var manager = new Mock<IThemeManager>();
        manager.SetupGet(m => m.CurrentTheme).Returns(Theme.Light);
        var service = new ThemeService(settings.Object, manager.Object);
        using var form = new Form();

        service.ApplyTheme(form);

        manager.Verify(m => m.ApplyTheme(form), Times.Once);
    }
}
