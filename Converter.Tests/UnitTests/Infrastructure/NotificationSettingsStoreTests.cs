using System.IO;
using Converter.Domain.Models;
using Converter.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Converter.Tests.UnitTests.Infrastructure;

public class NotificationSettingsStoreTests
{
    [Fact]
    public void SaveSettings_ShouldPersistPreferences()
    {
        // Arrange
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var store = new NotificationSettingsStore(path, Mock.Of<ILogger<NotificationSettingsStore>>());
        var options = new NotificationOptions
        {
            EnableSound = true,
            ShowToasts = true,
            SoundVolume = 0.7f
        };

        // Act
        store.Save(options);
        var loaded = store.Load();

        // Assert
        loaded.EnableSound.Should().BeTrue();
        loaded.ShowToasts.Should().BeTrue();
        loaded.SoundVolume.Should().BeApproximately(0.7f, 0.0001f);
    }

    [Fact]
    public void LoadSettings_WhenFileMissing_ShouldReturnDefaults()
    {
        // Arrange
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var store = new NotificationSettingsStore(path, Mock.Of<ILogger<NotificationSettingsStore>>());

        // Act
        var options = store.Load();

        // Assert
        options.Should().NotBeNull();
        options.EnableSound.Should().BeFalse();
    }

    [Fact]
    public void LoadSettings_WithInvalidJson_ShouldReturnDefaultsAndLogError()
    {
        // Arrange
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        File.WriteAllText(path, "invalid json");
        var loggerMock = new Mock<ILogger<NotificationSettingsStore>>();
        var store = new NotificationSettingsStore(path, loggerMock.Object);

        // Act
        var options = store.Load();

        // Assert
        options.Should().NotBeNull();
        loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<System.Exception>(),
            It.IsAny<Func<It.IsAnyType, System.Exception?, string>>()), Times.Once);
    }
}
