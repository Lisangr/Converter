using System.Threading;
using System.Threading.Tasks;
using Converter.Domain.Models;
using Converter.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Converter.Tests.UnitTests.Infrastructure;

public class FileSettingsStoreTests
{
    private readonly FileSettingsStore _store;

    public FileSettingsStoreTests()
    {
        _store = new FileSettingsStore("settings.json", Mock.Of<ILogger<FileSettingsStore>>());
    }

    [Fact]
    public async Task GetSettingAsync_WhenNotPresent_ShouldReturnDefault()
    {
        // Act
        var result = await _store.GetSettingAsync("key", 42, CancellationToken.None);

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public async Task SetSettingAsync_ShouldCompleteSuccessfully()
    {
        // Act
        var action = async () => await _store.SetSettingAsync("Volume", 10); 

        // Assert
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetAppSettingsAsync_ShouldReturnEmptySettings()
    {
        // Act
        var settings = await _store.GetAppSettingsAsync();

        // Assert
        settings.Should().BeOfType<AppSettings>();
    }

    [Fact]
    public async Task SaveUserPreferencesAsync_ShouldCompleteWithoutError()
    {
        // Act
        var action = async () => await _store.SaveUserPreferencesAsync(new UserPreferences());

        // Assert
        await action.Should().NotThrowAsync();
    }
}
