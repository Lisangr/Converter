using System;
using System.Threading.Tasks;
using Converter.Application.ErrorHandling;
using FluentAssertions;
using Xunit;

namespace Converter.Tests.UnitTests.ErrorHandling;

public class FileSystemErrorTests
{
    [Fact]
    public async Task HandleErrorAsync_ShouldCompleteWithoutThrowing()
    {
        // Arrange
        var handler = new FileSystemErrorHandler();

        // Act
        var act = async () => await handler.HandleErrorAsync("/tmp/file.mp4", new InvalidOperationException("boom"), "test");

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task FileValidator_ShouldReturnFalseForMissingFile()
    {
        // Arrange
        var validator = new FileValidator();

        // Act
        var result = await validator.ValidateAsync("/path/that/does/not/exist.mp4");

        // Assert
        result.IsValid.Should().BeFalse();
    }
}
