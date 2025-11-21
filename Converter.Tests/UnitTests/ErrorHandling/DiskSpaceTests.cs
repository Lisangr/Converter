using System.Threading.Tasks;
using Converter.Application.ErrorHandling;
using FluentAssertions;
using Xunit;

namespace Converter.Tests.UnitTests.ErrorHandling;

public class DiskSpaceTests
{
    [Fact]
    public async Task CheckAvailableSpaceAsync_ShouldReportConfiguredValues()
    {
        // Arrange
        var checker = new DiskSpaceHandler();

        // Act
        var result = await checker.CheckAvailableSpaceAsync("/video", 10_000_000);

        // Assert
        result.HasEnoughSpace.Should().BeTrue();
        result.AvailableSpace.Should().BeGreaterThan(0);
        result.RequiredSpace.Should().Be(10_000_000);
    }

    [Fact]
    public async Task CheckAvailableSpaceAsync_ShouldBeDeterministic()
    {
        // Arrange
        var checker = new DiskSpaceHandler();

        // Act
        var first = await checker.CheckAvailableSpaceAsync("/video", 1);
        var second = await checker.CheckAvailableSpaceAsync("/video", 2);

        // Assert
        second.AvailableSpace.Should().Be(first.AvailableSpace);
        second.HasEnoughSpace.Should().BeTrue();
    }
}
