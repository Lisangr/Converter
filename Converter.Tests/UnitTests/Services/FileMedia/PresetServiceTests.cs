using System.Collections.Generic;
using System.Threading.Tasks;
using Converter.Application.Models;
using Converter.Application.Services.FileMedia;
using FluentAssertions;
using Moq;
using Xunit;

namespace Converter.Tests.UnitTests.Services.FileMedia;

public class PresetServiceTests
{
    [Fact]
    public async Task GetPresetsAsync_ShouldReturnPresetsFromRepository()
    {
        // Arrange
        var presets = new List<PresetProfile>
        {
            new() { Id = "p1", Name = "Preset1" },
            new() { Id = "p2", Name = "Preset2" }
        };

        var repoMock = new Mock<IPresetRepository>();
        repoMock.Setup(r => r.GetAllAsync(default)).ReturnsAsync(presets);

        var service = new PresetService(repoMock.Object);

        // Act
        var result = await service.GetPresetsAsync();

        // Assert
        result.Should().BeEquivalentTo(presets);
        repoMock.Verify(r => r.GetAllAsync(default), Times.Once);
    }

    [Fact]
    public async Task SavePresetAsync_ShouldDelegateToRepository()
    {
        // Arrange
        var repoMock = new Mock<IPresetRepository>();
        var service = new PresetService(repoMock.Object);
        var preset = new PresetProfile { Id = "p1", Name = "Preset1" };

        // Act
        await service.SavePresetAsync(preset);

        // Assert
        repoMock.Verify(r => r.SaveAsync(preset, default), Times.Once);
    }
}
