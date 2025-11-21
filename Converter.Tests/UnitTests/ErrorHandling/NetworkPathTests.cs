using System.Threading.Tasks;
using Converter.Application.ErrorHandling;
using FluentAssertions;
using Xunit;

namespace Converter.Tests.UnitTests.ErrorHandling;

public class NetworkPathTests
{
    [Theory]
    [InlineData("\\\\server\\share\\file.mp4", true)]
    [InlineData("http://example.com/video", true)]
    [InlineData("/local/path/file.mp4", false)]
    public void IsNetworkPath_ShouldDetectNetworkLocations(string path, bool expected)
    {
        // Arrange
        var validator = new NetworkPathHandler();

        // Act
        var isNetwork = validator.IsNetworkPath(path);

        // Assert
        isNetwork.Should().Be(expected);
    }

    [Fact]
    public async Task ValidateNetworkPathAsync_ShouldReturnValidityFlag()
    {
        // Arrange
        var validator = new NetworkPathHandler();

        // Act
        var result = await validator.ValidateNetworkPathAsync("\\\\server\\share\\video.mp4");

        // Assert
        result.IsValid.Should().BeTrue();
        result.IsNetworkPath.Should().BeTrue();
    }
}
