using System.IO;
using System.Threading.Tasks;
using Converter.Application.ErrorHandling;
using FluentAssertions;
using Xunit;

namespace Converter.Tests.UnitTests.ErrorHandling;

public class CorruptedFileTests
{
    [Fact]
    public async Task ValidateAsync_ShouldReturnValidForExistingFile()
    {
        // Arrange
        var validator = new FileValidator();
        var tempFile = Path.GetTempFileName();

        try
        {
            // Act
            var result = await validator.ValidateAsync(tempFile);

            // Assert
            result.IsValid.Should().BeTrue();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ValidateAsync_ShouldReturnInvalidForEmptyPath()
    {
        // Arrange
        var validator = new FileValidator();

        // Act
        var result = await validator.ValidateAsync(string.Empty);

        // Assert
        result.IsValid.Should().BeFalse();
    }
}
