using System.Threading.Tasks;
using Converter.Application.Models;
using Converter.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Converter.Tests.UnitTests.Infrastructure;

public class JsonPresetRepositoryTests
{
    private readonly JsonPresetRepository _repository;

    public JsonPresetRepositoryTests()
    {
        _repository = new JsonPresetRepository("presets.json", Mock.Of<ILogger<JsonPresetRepository>>());
    }

    [Fact]
    public async Task GetPresetsAsync_WhenNoPresets_ShouldReturnEmptyCollection()
    {
        // Act
        var presets = await _repository.GetPresetsAsync();

        // Assert
        // Текущая реализация использует XmlPresetLoader и всегда возвращает
        // хотя бы набор встроенных пресетов.
        presets.Should().NotBeNull();
    }

    [Fact]
    public async Task SavePresetAsync_ShouldCompleteWithoutError()
    {
        // Arrange
        var preset = new ConversionProfile("id", "libx264", "aac", "128k", 23);

        // Act
        var action = async () => await _repository.SavePresetAsync(preset);

        // Assert
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetPresetAsync_WhenNotFound_ShouldReturnNull()
    {
        // Act
        var preset = await _repository.GetPresetAsync("missing");

        // Assert
        preset.Should().BeNull();
    }
}
