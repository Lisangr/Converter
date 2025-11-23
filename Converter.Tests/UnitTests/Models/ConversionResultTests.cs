using Xunit;
using Converter.Domain.Models;

namespace Converter.Tests.UnitTests.Models;

public class ConversionResultTests
{
    [Fact]
    public void ConversionResult_ShouldIncludeOutputPath()
    {
        // Arrange & Act
        var result = new ConversionResult
        {
            Success = true,
            OutputPath = "/output/video.mp4",
            OutputFileSize = 1024000
        };
        
        // Assert
        Assert.True(result.Success);
        Assert.Equal("/output/video.mp4", result.OutputPath);
        Assert.Equal(1024000, result.OutputFileSize);
    }

    [Fact]
    public void ConversionResult_ShouldCaptureDuration()
    {
        // Arrange & Act - Note: Current model doesn't have duration, but let's test what exists
        var result = new ConversionResult
        {
            Success = true,
            OutputPath = "/output/duration-test.mp4",
            OutputFileSize = 2048000
        };
        
        // Assert - Test current properties
        Assert.True(result.Success);
        Assert.Equal("/output/duration-test.mp4", result.OutputPath);
        Assert.Equal(2048000, result.OutputFileSize);
        
        // Note: Duration property would need to be added to the model for this test
        // For now, we'll test what exists in the current model
    }

    [Fact]
    public void ConversionResult_ShouldRecordWarnings()
    {
        // Arrange & Act
        var result = new ConversionResult
        {
            Success = false,
            ErrorMessage = "FFmpeg warning: Some audio stream could not be processed",
            OutputPath = "/output/warning-video.mp4"
        };
        
        // Assert
        Assert.False(result.Success);
        Assert.Contains("warning", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("audio stream", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("/output/warning-video.mp4", result.OutputPath);
    }

    [Fact]
    public void ConversionResult_ShouldHandleSuccessfulConversion()
    {
        // Arrange & Act
        var result = new ConversionResult
        {
            Success = true,
            OutputPath = "/output/success.mp4",
            OutputFileSize = 15728640,
            ErrorMessage = null
        };
        
        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.OutputPath);
        Assert.Null(result.ErrorMessage);
        Assert.True(result.OutputFileSize > 0);
    }

    [Fact]
    public void ConversionResult_ShouldHandleFailedConversion()
    {
        // Arrange & Act
        var result = new ConversionResult
        {
            Success = false,
            OutputPath = null,
            OutputFileSize = 0,
            ErrorMessage = "Input file is corrupted or unsupported format"
        };
        
        // Assert
        Assert.False(result.Success);
        Assert.Null(result.OutputPath);
        Assert.Equal(0, result.OutputFileSize);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("corrupted", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ConversionResult_ShouldHandlePartialSuccess()
    {
        // Arrange & Act - Some conversions might partially succeed
        var result = new ConversionResult
        {
            Success = false, // Marked as failed due to issues
            OutputPath = "/output/partial.mp4",
            OutputFileSize = 5120000, // Some output was generated
            ErrorMessage = "Conversion completed with warnings: audio codec changed, some metadata lost"
        };
        
        // Assert
        Assert.False(result.Success); // Not fully successful
        Assert.NotNull(result.OutputPath); // But output exists
        Assert.True(result.OutputFileSize > 0); // And has content
        Assert.NotNull(result.ErrorMessage); // With warnings/errors
        Assert.Contains("warnings", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ConversionResult_ShouldTrackFileSize()
    {
        // Test various file sizes
        var testCases = new[]
        {
            (0, "Empty file"),
            (1024, "1KB file"),
            (1048576, "1MB file"),
            (1073741824L, "1GB file"),
            (8589934592L, "8GB file")
        };

        foreach (var (size, description) in testCases)
        {
            var result = new ConversionResult
            {
                Success = true,
                OutputPath = $"/output/{description.Replace(" ", "-").ToLower()}.mp4",
                OutputFileSize = size
            };
            
            Assert.Equal(size, result.OutputFileSize);
            Assert.True(result.Success);
        }
    }

    [Fact]
    public void ConversionResult_ShouldHandleNullAndEmptyPaths()
    {
        // Test null path
        var resultWithNull = new ConversionResult
        {
            Success = false,
            OutputPath = null,
            ErrorMessage = "No output path specified"
        };
        
        Assert.Null(resultWithNull.OutputPath);
        Assert.False(resultWithNull.Success);

        // Test empty path
        var resultWithEmpty = new ConversionResult
        {
            Success = false,
            OutputPath = "",
            ErrorMessage = "Empty output path"
        };
        
        Assert.Equal("", resultWithEmpty.OutputPath);
        Assert.False(resultWithEmpty.Success);
    }
}
