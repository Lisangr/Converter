using System.IO;
using System.Text;
using System.Threading.Tasks;
using Converter.Application.Services.FileMedia;
using FluentAssertions;
using Xunit;

namespace Converter.Tests.UnitTests.Services.FileMedia;

public class XmlPresetLoaderTests
{
    [Fact]
    public async Task LoadPresetsAsync_WithValidXml_ShouldReturnEmptyListForStub()
    {
        // Arrange
        var xml = "<?xml version=\"1.0\"?><presets></presets>";
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var loader = new XmlPresetLoader();

        // Act
        var result = await loader.LoadPresetsAsync(stream);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadPresetsAsync_WithNullStream_ShouldThrow()
    {
        // Arrange
        var loader = new XmlPresetLoader();

        // Act
        Func<Task> act = () => loader.LoadPresetsAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SavePresetsAsync_ShouldWriteBasicXml()
    {
        // Arrange
        await using var stream = new MemoryStream();
        var loader = new XmlPresetLoader();

        // Act
        await loader.SavePresetsAsync(stream, Array.Empty<Converter.Application.Models.PresetProfile>());

        // Assert
        stream.Position = 0;
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var content = await reader.ReadToEndAsync();
        content.Should().Contain("<presets>");
    }
}
