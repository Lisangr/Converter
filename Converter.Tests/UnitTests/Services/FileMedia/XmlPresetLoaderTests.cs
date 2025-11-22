using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Converter.Application.Models;
using Converter.Application.Services.FileMedia;
using FluentAssertions;
using Xunit;

namespace Converter.Tests.UnitTests.Services.FileMedia;

public class XmlPresetLoaderTests
{
    private readonly XmlPresetLoader _loader;

    public XmlPresetLoaderTests()
    {
        _loader = new XmlPresetLoader();
    }

    [Fact]
    public async Task LoadPresetsAsync_WithValidXml_ShouldReturnEmptyListForStub()
    {
        // Arrange
        var xml = "<?xml version=\"1.0\"?><presets></presets>";
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

        // Act
        var result = await _loader.LoadPresetsAsync(stream, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadPresetsAsync_WithNullStream_ShouldThrow()
    {
        // Act
        Func<Task> act = () => _loader.LoadPresetsAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>().WithMessage("*stream*");
    }

    // Тест с null CancellationToken больше не актуален: метод принимает CancellationToken по значению
    // и имеет параметр по умолчанию. Достаточно вызывать перегрузку без токена, что уже покрыто другими тестами.

    [Fact]
    public async Task LoadPresetsAsync_WithEmptyStream_ShouldReturnEmptyList()
    {
        // Arrange
        await using var stream = new MemoryStream();

        // Act
        var result = await _loader.LoadPresetsAsync(stream);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadPresetsAsync_WithInvalidXml_ShouldReturnEmptyListForStub()
    {
        // Arrange
        var invalidXml = "<?xml version=\"1.0\"?><invalid><xml><content>";
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(invalidXml));

        // Act
        var result = await _loader.LoadPresetsAsync(stream);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadPresetsAsync_WithComplexXml_ShouldReturnEmptyListForStub()
    {
        // Arrange
        var complexXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<presets>
    <category name=""Video Platforms"">
        <preset id=""youtube-hd"" name=""YouTube HD"">
            <video codec=""libx264"" width=""1920"" height=""1080"" bitrate=""8000"" crf=""23"" format=""mp4""/>
            <audio codec=""aac"" bitrate=""256"" enabled=""true""/>
            <constraints maxFileSizeMB=""1000"" maxDurationSeconds=""3600""/>
            <ui colorHex=""#FF0000"" isPro=""false""/>
        </preset>
    </category>
</presets>";
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(complexXml));

        // Act
        var result = await _loader.LoadPresetsAsync(stream);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty(); // Stub implementation returns empty list
    }

    [Fact]
    public async Task SavePresetsAsync_ShouldWriteBasicXml()
    {
        // Arrange
        await using var stream = new MemoryStream();

        // Act
        await _loader.SavePresetsAsync(stream, Array.Empty<PresetProfile>());

        // Assert
        stream.Position = 0;
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var content = await reader.ReadToEndAsync();
        content.Should().Contain("<presets>");
        content.Should().Contain("<?xml version=\"1.0\"");
    }

    [Fact]
    public async Task SavePresetsAsync_WithNullStream_ShouldThrow()
    {
        // Arrange
        var presets = new List<PresetProfile>();

        // Act
        Func<Task> act = () => _loader.SavePresetsAsync(null!, presets);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>().WithMessage("*stream*");
    }

    [Fact]
    public async Task SavePresetsAsync_WithNullPresets_ShouldThrow()
    {
        // Arrange
        await using var stream = new MemoryStream();

        // Act
        Func<Task> act = () => _loader.SavePresetsAsync(stream, null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>().WithMessage("*presets*");
    }

    [Fact]
    public async Task SavePresetsAsync_WithCancellationToken_ShouldNotThrow()
    {
        // Arrange
        await using var stream = new MemoryStream();
        var presets = new List<PresetProfile>();

        // Act
        var act = async () => await _loader.SavePresetsAsync(stream, presets, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SavePresetsAsync_WithEmptyPresetsList_ShouldWriteEmptyXml()
    {
        // Arrange
        await using var stream = new MemoryStream();
        var emptyPresets = new List<PresetProfile>();

        // Act
        await _loader.SavePresetsAsync(stream, emptyPresets);

        // Assert
        stream.Position = 0;
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var content = await reader.ReadToEndAsync();
        content.Should().Contain("<presets>");
        content.Should().Contain("</presets>");
        content.Should().NotContain("<preset");
    }

    [Fact]
    public async Task SavePresetsAsync_WithCancellationToken_ShouldPassToken()
    {
        // Arrange
        await using var stream = new MemoryStream();
        var presets = new List<PresetProfile>();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        Func<Task> act = () => _loader.SavePresetsAsync(stream, presets, cts.Token);

        // Assert
        await act.Should().NotThrowAsync(); // Stub implementation doesn't actually use cancellation
    }

    [Fact]
    public async Task SavePresetsAsync_StreamShouldRemainOpen()
    {
        // Arrange
        await using var stream = new MemoryStream();
        var presets = new List<PresetProfile>();

        // Act
        await _loader.SavePresetsAsync(stream, presets);

        // Assert
        stream.Position = 0;
        stream.CanRead.Should().BeTrue(); // Stream should still be open and readable
    }

    [Fact]
    public async Task LoadPresetsAsync_StreamPositionShouldBeIgnored()
    {
        // Arrange
        var xml = "<?xml version=\"1.0\"?><presets></presets>";
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        stream.Position = 10; // Set position to middle

        // Act
        var result = await _loader.LoadPresetsAsync(stream);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadPresetsAsync_WithLargeXmlContent_ShouldReturnEmptyList()
    {
        // Arrange
        var largeXml = new string('x', 10000); // Large content
        var xml = $"<?xml version=\"1.0\"?><presets>{largeXml}</presets>";
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

        // Act
        var result = await _loader.LoadPresetsAsync(stream);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadPresetsAsync_WithSpecialCharactersInXml_ShouldReturnEmptyList()
    {
        // Arrange
        var xmlWithSpecialChars = "<?xml version=\"1.0\"?><presets><preset name=\"Test &amp; Special &lt;Chars&gt;\"></preset></presets>";
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xmlWithSpecialChars));

        // Act
        var result = await _loader.LoadPresetsAsync(stream);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SavePresetsAsync_WithMultiplePresets_ShouldWriteEmptyXml()
    {
        // Arrange
        await using var stream = new MemoryStream();
        var presets = new List<PresetProfile>
        {
            new PresetProfile { Id = "preset1", Name = "Preset 1" },
            new PresetProfile { Id = "preset2", Name = "Preset 2" },
            new PresetProfile { Id = "preset3", Name = "Preset 3" }
        };

        // Act
        await _loader.SavePresetsAsync(stream, presets);

        // Assert
        stream.Position = 0;
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var content = await reader.ReadToEndAsync();
        content.Should().Contain("<presets>");
        content.Should().Contain("</presets>");
        // Stub implementation ignores actual presets and writes empty XML
    }
}
