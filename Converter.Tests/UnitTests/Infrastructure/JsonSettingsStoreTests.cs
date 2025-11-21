using System.Threading;
using System.Threading.Tasks;
using Converter.Domain.Models;
using Converter.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Converter.Tests.UnitTests.Infrastructure;

public class JsonSettingsStoreTests
{
    private readonly JsonSettingsStore _store;

    public JsonSettingsStoreTests()
    {
        _store = new JsonSettingsStore("test.json", Mock.Of<ILogger<JsonSettingsStore>>());
    }

    [Fact]
    public async Task SetSettingAsync_ShouldCompleteWithoutError()
    {
        // Act
        var action = async () => await _store.SetSettingAsync("Theme", "Dark");

        // Assert
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetSettingAsync_WhenNoValue_ShouldReturnDefault()
    {
        // Act
        var result = await _store.GetSettingAsync("Missing", defaultValue: "Default", ct: CancellationToken.None);

        // Assert
        result.Should().Be("Default");
    }

    [Fact]
    public async Task AppSettingsOperations_ShouldReturnNewInstances()
    {
        // Act
        var appSettings = await _store.GetAppSettingsAsync();
        await _store.SaveAppSettingsAsync(new AppSettings());

        // Assert
        appSettings.Should().NotBeNull();
    }

    [Fact]
    public async Task UserPreferencesOperations_ShouldReturnNewInstances()
    {
        // Act
        var preferences = await _store.GetUserPreferencesAsync();
        await _store.SaveUserPreferencesAsync(new UserPreferences());

        // Assert
        preferences.Should().NotBeNull();
    }

    [Fact]
    public async Task SecureValueOperations_ShouldReturnNullWhenNotSet()
    {
        // Act
        var value = await _store.GetSecureValueAsync("token");

        // Assert
        value.Should().BeNull();
    }
}
