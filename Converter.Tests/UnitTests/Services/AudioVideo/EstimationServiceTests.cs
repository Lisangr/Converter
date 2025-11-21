using System;
using System.Reflection;
using System.Threading.Tasks;
using Converter.Services;
using FluentAssertions;
using Xunit;

namespace Converter.Tests.UnitTests.Services.AudioVideo;

public class EstimationServiceTests
{
    [Fact]
    public void CalculateOutputSize_ShouldReturnNonNegative()
    {
        // Arrange
        var service = new EstimationService();
        var method = typeof(EstimationService)
            .GetMethod("CalculateOutputSize", BindingFlags.NonPublic | BindingFlags.Instance)!;

        // Act
        var result = (long)method.Invoke(service, new object[] { 4000, 192, 60.0 })!;

        // Assert
        result.Should().BeGreaterThan(0);
    }

    [Fact]
    public void EstimateConversionTime_ShouldReturnPositiveDuration_ForZeroDurationInput()
    {
        // Arrange
        var service = new EstimationService();
        var method = typeof(EstimationService)
            .GetMethod("EstimateConversionTime", BindingFlags.NonPublic | BindingFlags.Instance)!;

        // Create minimal VideoInfo instance via reflection
        var videoInfoType = typeof(EstimationService).GetNestedType("VideoInfo", BindingFlags.Public | BindingFlags.NonPublic)!;
        var videoInfo = Activator.CreateInstance(videoInfoType)!;

        // Act
        var timeSpan = (TimeSpan)method.Invoke(
            service,
            new object[] { videoInfo, "libx264", null!, null!, 0.0, null! })!;

        // Assert
        Assert.True(timeSpan.TotalSeconds >= 0);
    }
}
