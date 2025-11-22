using System;
using System.IO;
using System.Threading.Tasks;
using Converter.Application.Services;
using FluentAssertions;
using Xunit;

namespace Converter.Tests.UnitTests.Services.FileMedia;

public class FileServiceTests
{
    private readonly FileService _service;

    public FileServiceTests()
    {
        _service = new FileService();
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnTrue_ForExistingFile()
    {
        // Arrange
        var path = Path.GetTempFileName();

        try
        {
            // Act
            var exists = await _service.ExistsAsync(path);

            // Assert
            exists.Should().BeTrue();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnFalse_ForNonExistentFile()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".nonexistent");

        // Act
        var exists = await _service.ExistsAsync(nonExistentPath);

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_WithNullPath_ShouldThrowArgumentException()
    {
        // Act
        Func<Task> act = () => _service.ExistsAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ExistsAsync_WithEmptyPath_ShouldThrowArgumentException()
    {
        // Act
        Func<Task> act = () => _service.ExistsAsync("");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetSizeAsync_ShouldReturnFileLength()
    {
        // Arrange
        var path = Path.GetTempFileName();
        var content = new string('x', 100);
        await File.WriteAllTextAsync(path, content);

        try
        {
            // Act
            var size = await _service.GetSizeAsync(path);

            // Assert
            size.Should().Be(new FileInfo(path).Length);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task GetSizeAsync_ForNonExistentFile_ShouldReturnZero()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".nonexistent");

        // Act
        var size = await _service.GetSizeAsync(nonExistentPath);

        // Assert
        size.Should().Be(0);
    }

    [Fact]
    public async Task GetSizeAsync_WithNullPath_ShouldThrowArgumentException()
    {
        // Act
        Func<Task> act = () => _service.GetSizeAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ReadAndWriteAllTextAsync_ShouldRoundtripContent()
    {
        // Arrange
        var path = Path.GetTempFileName();
        var text = "hello world";

        try
        {
            // Act
            await _service.WriteAllTextAsync(path, text);
            var read = await _service.ReadAllTextAsync(path);

            // Assert
            read.Should().Be(text);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ReadAllTextAsync_WithLargeContent_ShouldHandleLargeFiles()
    {
        // Arrange
        var path = Path.GetTempFileName();
        var largeContent = new string('a', 1024 * 1024); // 1MB

        try
        {
            await File.WriteAllTextAsync(path, largeContent);

            // Act
            var read = await _service.ReadAllTextAsync(path);

            // Assert
            read.Should().HaveLength(1024 * 1024);
            read.Should().Be(largeContent);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ReadAllTextAsync_ForNonExistentFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".nonexistent");

        // Act
        Func<Task> act = () => _service.ReadAllTextAsync(nonExistentPath);

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task WriteAllTextAsync_WithSpecialCharacters_ShouldPreserveEncoding()
    {
        // Arrange
        var path = Path.GetTempFileName();
        var textWithSpecialChars = "Привет мир! 🌍 Special chars: äöü ñáéíóú";

        try
        {
            // Act
            await _service.WriteAllTextAsync(path, textWithSpecialChars);
            var read = await _service.ReadAllTextAsync(path);

            // Assert
            read.Should().Be(textWithSpecialChars);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task WriteAllTextAsync_WithEmptyString_ShouldCreateEmptyFile()
    {
        // Arrange
        var path = Path.GetTempFileName();

        try
        {
            // Act
            await _service.WriteAllTextAsync(path, string.Empty);

            // Assert
            var fileInfo = new FileInfo(path);
            fileInfo.Length.Should().Be(0);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task WriteAllTextAsync_WithNullContent_ShouldThrowArgumentException()
    {
        // Arrange
        var path = Path.GetTempFileName();

        try
        {
            // Act
            Func<Task> act = () => _service.WriteAllTextAsync(path, null!);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>();
        }
        finally
        {
            File.Delete(path);
        }
    }

    // Тесты для методов DeleteAsync/CreateDirectoryAsync/GetDirectoryNameAsync/GetFileNameAsync/
    // GetFileNameWithoutExtensionAsync удалены, так как эти методы больше не входят в контракт
    // текущего FileService/IFileService.
}
