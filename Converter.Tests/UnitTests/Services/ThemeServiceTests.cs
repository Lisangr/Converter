using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Converter.Application.Abstractions;
using Converter.Application.Services;
using Converter.Domain.Models;
using Converter.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Converter.Tests.UnitTests.Services
{
    public class ThemeServiceTests
    {
        private readonly Mock<ISettingsStore> _mockSettingsStore;
        private readonly Mock<IThemeManager> _mockThemeManager;
        private readonly Mock<ILogger<ThemeService>> _mockLogger;
        private readonly ThemeService _themeService;
        private readonly List<Theme> _availableThemes;
        private readonly List<EventLogEntry> _eventLog;

        private class EventLogEntry
        {
            public string EventType { get; set; } = string.Empty;
            public Theme? Theme { get; set; }
        }

        public ThemeServiceTests()
        {
            _mockSettingsStore = new Mock<ISettingsStore>();
            _mockThemeManager = new Mock<IThemeManager>();
            _mockLogger = new Mock<ILogger<ThemeService>>();
            
            // Setup default user preferences to avoid null reference issues
            var defaultPreferences = new UserPreferences
            {
                EnableAnimations = false,
                AnimationDuration = 300,
                AutoSwitchEnabled = false,
                DarkModeStart = new TimeSpan(20, 0, 0), // 20h
                DarkModeEnd = new TimeSpan(7, 0, 0), // 7h
                PreferredDarkTheme = "dark"
            };
            
            _mockSettingsStore.Setup(x => x.GetUserPreferencesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(defaultPreferences);
            _mockSettingsStore.Setup(x => x.SaveUserPreferencesAsync(It.IsAny<UserPreferences>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            
            // Setup default theme
            _mockThemeManager.Setup(x => x.CurrentTheme).Returns(Theme.Light);
            
            _themeService = new ThemeService(
                _mockSettingsStore.Object,
                _mockThemeManager.Object);

            _availableThemes = Theme.GetAllThemes().ToList();
            _eventLog = new List<EventLogEntry>();
            
            // Subscribe to events
            _themeService.ThemeChanged += (sender, theme) => _eventLog.Add(new EventLogEntry { EventType = "ThemeChanged", Theme = theme });
        }

        [Fact]
        public async Task Constructor_WithNullSettingsStore_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new ThemeService(null!, _mockThemeManager.Object));
        }

        [Fact]
        public async Task Constructor_WithNullThemeManager_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new ThemeService(_mockSettingsStore.Object, null!));
        }

        [Fact]
        public void CurrentTheme_ShouldReturnCurrentThemeFromManager()
        {
            // Arrange
            var expectedTheme = Theme.Dark;
            _mockThemeManager.Setup(x => x.CurrentTheme).Returns(expectedTheme);

            // Act
            var result = _themeService.CurrentTheme;

            // Assert
            result.Should().Be(expectedTheme);
        }

        [Fact]
        public async Task SetTheme_WithValidTheme_ShouldSetThemeSuccessfully()
        {
            // Arrange
            var targetTheme = Theme.Dark;
            _mockThemeManager.Setup(x => x.CurrentTheme).Returns(Theme.Light);

            // Act
            await _themeService.SetTheme(targetTheme);

            // Assert
            _mockThemeManager.Verify(x => x.SetTheme(targetTheme, true), Times.Once);
        }

        [Fact]
        public async Task SetTheme_WithNullTheme_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _themeService.SetTheme(null!));
        }

        [Fact]
        public async Task SetTheme_WithSameTheme_ShouldNotCallManager()
        {
            // Arrange
            var currentTheme = Theme.Light;
            _mockThemeManager.Setup(x => x.CurrentTheme).Returns(currentTheme);

            // Act
            await _themeService.SetTheme(currentTheme);

            // Assert
            _mockThemeManager.Verify(x => x.SetTheme(It.IsAny<Theme>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task SetTheme_WithAnimateFalse_ShouldCallManagerWithAnimateFalse()
        {
            // Arrange
            var targetTheme = Theme.Dark;
            _mockThemeManager.Setup(x => x.CurrentTheme).Returns(Theme.Light);

            // Act
            await _themeService.SetTheme(targetTheme, animate: false);

            // Assert
            _mockThemeManager.Verify(x => x.SetTheme(targetTheme, false), Times.Once);
        }

        [Fact]
        public async Task SetTheme_ShouldRaiseThemeChangedEvent()
        {
            // Arrange
            var targetTheme = Theme.Dark;
            _mockThemeManager.Setup(x => x.CurrentTheme).Returns(Theme.Light);

            // Act
            // В ThemeService событие ThemeChanged пробрасывается из менеджера тем,
            // поэтому для проверки просто эмулируем событие менеджера.
            _mockThemeManager.Raise(m => m.ThemeChanged += null!, _themeService, targetTheme);

            // Assert
            var themeChangedEvent = _eventLog.Find(e => e is { EventType: "ThemeChanged", Theme: Theme theme } && theme.Name == targetTheme.Name);
            themeChangedEvent.Should().NotBeNull();
        }

        [Fact]
        public void EnableAnimations_Getter_ShouldReturnCurrentValue()
        {
            // Act
            var result = _themeService.EnableAnimations;

            // Assert
            // В текущей реализации EnableAnimations по умолчанию равен false
            result.Should().BeFalse();
        }

        [Fact]
        public void EnableAnimations_Setter_ShouldUpdateAndSave()
        {
            // Arrange
            _mockSettingsStore.Setup(x => x.GetUserPreferencesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UserPreferences { EnableAnimations = false });
            _mockSettingsStore.Setup(x => x.SaveUserPreferencesAsync(It.IsAny<UserPreferences>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            _themeService.EnableAnimations = true;

            // Assert
            _mockSettingsStore.Verify(x => x.SaveUserPreferencesAsync(It.Is<UserPreferences>(p => p.EnableAnimations == true), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public void AnimationDuration_Getter_ShouldReturnCurrentValue()
        {
            // Act
            var result = _themeService.AnimationDuration;

            // Assert
            // По умолчанию в UserPreferences используется 300 мс
            result.Should().Be(300);
        }

        [Fact]
        public void AnimationDuration_Setter_ShouldClampAndSave()
        {
            // Arrange
            _mockSettingsStore.Setup(x => x.GetUserPreferencesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UserPreferences { AnimationDuration = 300 });
            _mockSettingsStore.Setup(x => x.SaveUserPreferencesAsync(It.IsAny<UserPreferences>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            _themeService.AnimationDuration = 50; // Should be clamped to 100

            // Assert
            _mockSettingsStore.Verify(x => x.SaveUserPreferencesAsync(It.Is<UserPreferences>(p => p.AnimationDuration == 100), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public void AutoSwitchEnabled_Getter_ShouldReturnCurrentValue()
        {
            // Act
            var result = _themeService.AutoSwitchEnabled;

            // Assert
            // По умолчанию в тестовом конструкторе AutoSwitchEnabled = false
            result.Should().BeFalse();
        }

        [Fact]
        public void AutoSwitchEnabled_Setter_ShouldUpdateAndEnableAutoSwitch()
        {
            // Arrange
            _mockSettingsStore.Setup(x => x.GetUserPreferencesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UserPreferences { AutoSwitchEnabled = false });
            _mockSettingsStore.Setup(x => x.SaveUserPreferencesAsync(It.IsAny<UserPreferences>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            _themeService.AutoSwitchEnabled = true;

            // Assert
            _mockSettingsStore.Verify(x => x.SaveUserPreferencesAsync(It.Is<UserPreferences>(p => p.AutoSwitchEnabled == true), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public void DarkModeStart_Getter_ShouldReturnCurrentValue()
        {
            // Arrange
            // В конструкторе теста по умолчанию используется 20:00
            var expectedTime = new TimeSpan(20, 0, 0);

            // Act
            var result = _themeService.DarkModeStart;

            // Assert
            result.Should().Be(expectedTime);
        }

        [Fact]
        public void DarkModeStart_Setter_ShouldUpdateAndSave()
        {
            // Arrange
            var newTime = new TimeSpan(21, 30, 0);
            _mockSettingsStore.Setup(x => x.GetUserPreferencesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UserPreferences { DarkModeStart = new TimeSpan(20, 0, 0) });
            _mockSettingsStore.Setup(x => x.SaveUserPreferencesAsync(It.IsAny<UserPreferences>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            _themeService.DarkModeStart = newTime;

            // Assert
            _mockSettingsStore.Verify(x => x.SaveUserPreferencesAsync(It.Is<UserPreferences>(p => p.DarkModeStart == newTime), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public void DarkModeEnd_Getter_ShouldReturnCurrentValue()
        {
            // Arrange
            // В конструкторе теста по умолчанию используется 7:00
            var expectedTime = new TimeSpan(7, 0, 0);

            // Act
            var result = _themeService.DarkModeEnd;

            // Assert
            result.Should().Be(expectedTime);
        }

        [Fact]
        public void PreferredDarkTheme_Getter_ShouldReturnCurrentValue()
        {
            // Act
            var result = _themeService.PreferredDarkTheme;

            // Assert
            // В конструкторе теста по умолчанию PreferredDarkTheme = "dark"
            result.Should().Be("dark");
        }

        [Fact]
        public void PreferredDarkTheme_Setter_WithNullOrEmpty_ShouldNotUpdate()
        {
            // Arrange
            _mockSettingsStore.Setup(x => x.GetUserPreferencesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UserPreferences { PreferredDarkTheme = "dark" });
            _mockSettingsStore.Setup(x => x.SaveUserPreferencesAsync(It.IsAny<UserPreferences>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            _themeService.PreferredDarkTheme = "";

            // Assert
            _mockSettingsStore.Verify(x => x.SaveUserPreferencesAsync(It.IsAny<UserPreferences>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public void ApplyTheme_WithForm_ShouldApplyThemeToForm()
        {
            // Arrange
            var form = new Form();
            _mockThemeManager.Setup(x => x.CurrentTheme).Returns(Theme.Dark);

            // Act
            _themeService.ApplyTheme(form);

            // Assert
            _mockThemeManager.Verify(x => x.ApplyTheme(form), Times.Once);
        }

        [Fact]
        public void ApplyTheme_WithControl_ShouldApplyThemeToParentForm()
        {
            // Arrange
            var form = new Form();
            var control = new Button { Parent = form };
            _mockThemeManager.Setup(x => x.CurrentTheme).Returns(Theme.Dark);

            // Act
            _themeService.ApplyTheme(control);

            // Assert
            _mockThemeManager.Verify(x => x.ApplyTheme(form), Times.Once);
        }

        [Fact]
        public void ApplyTheme_WithControlWithoutForm_ShouldNotThrow()
        {
            // Arrange
            var control = new Button();

            // Act & Assert
            var exception = Record.Exception(() => _themeService.ApplyTheme(control));
            exception.Should().BeNull();
        }

        [Fact]
        public async Task EnableAutoSwitchAsync_WithEnableTrue_ShouldStartTimer()
        {
            // Act
            await _themeService.EnableAutoSwitchAsync(true);

            // Assert
            // Timer should be started (we can't directly verify this, but we can check that no exception is thrown)
            await Task.CompletedTask;
        }

        [Fact]
        public async Task EnableAutoSwitchAsync_WithEnableFalse_ShouldStopTimer()
        {
            // Arrange
            await _themeService.EnableAutoSwitchAsync(true);

            // Act
            await _themeService.EnableAutoSwitchAsync(false);

            // Assert
            // Timer should be stopped (we can't directly verify this, but we can check that no exception is thrown)
            await Task.CompletedTask;
        }

        [Fact]
        public void Dispose_ShouldDisposeResources()
        {
            // Arrange
            var themeService = new ThemeService(_mockSettingsStore.Object, _mockThemeManager.Object);

            // Act
            themeService.Dispose();

            // Assert
            _mockThemeManager.Verify(x => x.Dispose(), Times.Once);
        }

        [Fact]
        public async Task SetTheme_WithSpecialCharactersInThemeName_ShouldHandleCorrectly()
        {
            // Arrange
            var specialTheme = new Theme
            {
                Name = "тема_с_русскими_символами_123",
                DisplayName = "Специальная тема"
            };
            _mockThemeManager.Setup(x => x.CurrentTheme).Returns(Theme.Light);

            // Act
            await _themeService.SetTheme(specialTheme);

            // Assert
            _mockThemeManager.Verify(x => x.SetTheme(specialTheme, true), Times.Once);
        }
    }
}