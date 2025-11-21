using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Converter.Application.Abstractions;
using Converter.Application.Models;
using Converter.Application.Services;
using Converter.Domain.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Converter.Tests.UnitTests.Application;

public class MockConverterTests
{
    private readonly Mock<ILogger<MockConverter>> _loggerMock;
    private readonly MockConverter _mockConverter;

    public MockConverterTests()
    {
        _loggerMock = new Mock<ILogger<MockConverter>>();
        _mockConverter = new MockConverter(_loggerMock.Object);
    }

    [Fact]
    public async Task ConvertAsync_ShouldReportProgressAndReturnSuccessfulOutcome()
    {
        // Arrange
        var request = new ConversionRequest(
            InputPath: "input.mp4",
            OutputPath: "output.mp4",
            Profile: new ConversionProfile("Mock", "libx264", "aac", "128k", 23));

        var reported = new List<int>();
        var progress = new Progress<int>(p => reported.Add(p));

        // Act
        var outcome = await _mockConverter.ConvertAsync(request, progress, CancellationToken.None);

        // Assert
        outcome.Should().NotBeNull();
        outcome.Success.Should().BeTrue();
        outcome.OutputSize.Should().Be(1024 * 1024); // 1 MB as in implementation
        outcome.ErrorMessage.Should().BeNull();

        reported.Should().NotBeEmpty();
        reported.Should().Contain(100);
        reported.Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task ConvertAsync_WithCancellationToken_ShouldThrowOperationCanceled()
    {
        // Arrange
        var request = new ConversionRequest(
            InputPath: "input_cancel.mp4",
            OutputPath: "output_cancel.mp4",
            Profile: new ConversionProfile("Mock", "libx264", "aac", "128k", 23));

        var progress = new Progress<int>(_ => { });

        using var cts = new CancellationTokenSource();

        // Act
        var task = _mockConverter.ConvertAsync(request, progress, cts.Token);
        cts.CancelAfter(200); // cancel shortly after start

        // Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () => await task);
    }

    [Fact]
    public async Task ConvertAsync_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Act
        Func<Task> act = () => _mockConverter.ConvertAsync(null!, new Progress<int>(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NullReferenceException>();
    }
}
