using System.IO;
using System.Threading.Tasks;
using Converter.Application.Services;
using FluentAssertions;
using Xunit;

namespace Converter.Tests.UnitTests.Services.FileMedia;

public class FileServiceTests
{
    [Fact]
    public async Task ExistsAsync_ShouldReturnTrue_ForExistingFile()
    {
        // Arrange
        var service = new FileService();
        var path = Path.GetTempFileName();

        try
        {
            // Act
            var exists = await service.ExistsAsync(path);

            // Assert
            exists.Should().BeTrue();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task GetSizeAsync_ShouldReturnFileLength()
    {
        // Arrange
        var service = new FileService();
        var path = Path.GetTempFileName();
        var content = new string('x', 100);
        await File.WriteAllTextAsync(path, content);

        try
        {
            // Act
            var size = await service.GetSizeAsync(path);

            // Assert
            size.Should().Be(new FileInfo(path).Length);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ReadAndWriteAllTextAsync_ShouldRoundtripContent()
    {
        // Arrange
        var service = new FileService();
        var path = Path.GetTempFileName();
        var text = "hello world";

        try
        {
            // Act
            await service.WriteAllTextAsync(path, text);
            var read = await service.ReadAllTextAsync(path);

            // Assert
            read.Should().Be(text);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
