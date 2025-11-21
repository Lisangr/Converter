using System.IO;
using System.Threading.Tasks;
using Converter.Application.Services;
using FluentAssertions;
using Xunit;

namespace Converter.Tests.UnitTests.Services.FileMedia;

public class ThumbnailServiceTests
{
    [Fact]
    public async Task GetThumbnailAsync_ShouldReturnStream()
    {
        // Arrange
        var service = new ThumbnailService(new ThumbnailGenerator());

        // Act
        var result = await service.GetThumbnailAsync("video.mp4", 320, 180);

        // Assert
        result.Should().NotBeNull();
    }
}
