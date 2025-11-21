using Xunit;
using Converter.Application.Models;

namespace Converter.Tests.UnitTests.Models;

public class QueueStatisticsTests
{
    [Fact]
    public void QueueStatistics_ShouldAggregateDurations()
    {
        // Arrange
        var stats = new QueueStatistics
        {
            TotalProcessingTime = TimeSpan.FromMinutes(15),
            EstimatedTimeRemaining = TimeSpan.FromMinutes(5),
            AverageSpeed = 1.5 // 1.5x real-time speed
        };
        
        // Assert
        Assert.Equal(TimeSpan.FromMinutes(15), stats.TotalProcessingTime);
        Assert.Equal(TimeSpan.FromMinutes(5), stats.EstimatedTimeRemaining);
        Assert.Equal(1.5, stats.AverageSpeed);
    }

    [Fact]
    public void QueueStatistics_ShouldCountStatuses()
    {
        // Arrange
        var stats = new QueueStatistics
        {
            TotalItems = 10,
            PendingItems = 3,
            ProcessingItems = 2,
            CompletedItems = 4,
            FailedItems = 1
        };
        
        // Assert
        Assert.Equal(10, stats.TotalItems);
        Assert.Equal(3, stats.PendingItems);
        Assert.Equal(2, stats.ProcessingItems);
        Assert.Equal(4, stats.CompletedItems);
        Assert.Equal(1, stats.FailedItems);
        
        // Verify total adds up
        Assert.Equal(10, stats.PendingItems + stats.ProcessingItems + stats.CompletedItems + stats.FailedItems);
    }

    [Fact]
    public void QueueStatistics_ShouldEstimateCompletion()
    {
        // Arrange
        var stats = new QueueStatistics
        {
            ProcessingItems = 2,
            PendingItems = 5,
            AverageSpeed = 2.0, // 2x real-time
            EstimatedTimeRemaining = TimeSpan.FromMinutes(10)
        };
        
        // Assert
        Assert.Equal(2, stats.ProcessingItems);
        Assert.Equal(5, stats.PendingItems);
        Assert.Equal(2.0, stats.AverageSpeed);
        Assert.Equal(TimeSpan.FromMinutes(10), stats.EstimatedTimeRemaining);
    }

    [Fact]
    public void QueueStatistics_ShouldCalculateSpaceSaved()
    {
        // Arrange
        var stats = new QueueStatistics
        {
            TotalInputSize = 1073741824L, // 1GB
            TotalOutputSize = 536870912L  // 512MB
        };
        
        // Act
        var spaceSaved = stats.SpaceSaved;
        
        // Assert
        Assert.Equal(536870912L, spaceSaved); // 1GB - 512MB = 512MB
        Assert.Equal(536870912L, stats.TotalInputSize - stats.TotalOutputSize);
    }

    [Fact]
    public void QueueStatistics_ShouldCalculateCompressionRatio()
    {
        // Test various compression scenarios
        var testCases = new[]
        {
            (1073741824L, 536870912L, 0.5),    // 1GB -> 512MB = 50%
            (2147483648L, 2147483648L, 1.0),   // 2GB -> 2GB = 100% (no compression)
            (1073741824L, 268435456L, 0.25),   // 1GB -> 256MB = 25%
            (0L, 0L, 0.0)                       // Empty queue = 0%
        };

        foreach (var (inputSize, outputSize, expectedRatio) in testCases)
        {
            var stats = new QueueStatistics
            {
                TotalInputSize = inputSize,
                TotalOutputSize = outputSize
            };
            
            Assert.Equal(expectedRatio, stats.CompressionRatio, 2);
        }
    }

    [Fact]
    public void QueueStatistics_ShouldCalculateSuccessRate()
    {
        // Test various success scenarios
        var testCases = new[]
        {
            (10, 8, 80),    // 10 total, 8 completed = 80%
            (5, 5, 100),    // 5 total, 5 completed = 100%
            (20, 0, 0),     // 20 total, 0 completed = 0%
            (0, 0, 0)       // Empty queue = 0%
        };

        foreach (var (total, completed, expectedRate) in testCases)
        {
            var stats = new QueueStatistics
            {
                TotalItems = total,
                CompletedItems = completed
            };
            
            Assert.Equal(expectedRate, stats.SuccessRate);
        }
    }

    [Fact]
    public void QueueStatistics_ShouldHandleZeroInputSize()
    {
        // Arrange
        var stats = new QueueStatistics
        {
            TotalInputSize = 0,
            TotalOutputSize = 0
        };
        
        // Assert
        Assert.Equal(0.0, stats.CompressionRatio);
        Assert.Equal(0, stats.SpaceSaved);
    }

    [Fact]
    public void QueueStatistics_ShouldHandleLargeValues()
    {
        // Arrange - Test with large file sizes
        var stats = new QueueStatistics
        {
            TotalInputSize = 1099511627776L, // 1TB
            TotalOutputSize = 549755813888L, // 512GB
            TotalItems = 1000,
            CompletedItems = 750,
            ProcessingItems = 50,
            PendingItems = 150,
            FailedItems = 50,
            TotalProcessingTime = TimeSpan.FromHours(25),
            EstimatedTimeRemaining = TimeSpan.FromHours(5),
            AverageSpeed = 1.2
        };
        
        // Assert
        Assert.Equal(549755813888L, stats.SpaceSaved); // 1TB - 512GB = 512GB
        Assert.Equal(0.5, stats.CompressionRatio, 2);
        Assert.Equal(75, stats.SuccessRate); // 750/1000 = 75%
        Assert.Equal(TimeSpan.FromHours(25), stats.TotalProcessingTime);
        Assert.Equal(TimeSpan.FromHours(5), stats.EstimatedTimeRemaining);
        Assert.Equal(1.2, stats.AverageSpeed);
    }

    [Fact]
    public void QueueStatistics_ShouldHandleEmptyQueue()
    {
        // Arrange
        var stats = new QueueStatistics();
        
        // Assert - All values should be zero
        Assert.Equal(0, stats.TotalItems);
        Assert.Equal(0, stats.PendingItems);
        Assert.Equal(0, stats.ProcessingItems);
        Assert.Equal(0, stats.CompletedItems);
        Assert.Equal(0, stats.FailedItems);
        Assert.Equal(0L, stats.TotalInputSize);
        Assert.Equal(0L, stats.TotalOutputSize);
        Assert.Equal(0, stats.SpaceSaved);
        Assert.Equal(0.0, stats.CompressionRatio);
        Assert.Equal(TimeSpan.Zero, stats.TotalProcessingTime);
        Assert.Equal(TimeSpan.Zero, stats.EstimatedTimeRemaining);
        Assert.Equal(0.0, stats.AverageSpeed);
        Assert.Equal(0, stats.SuccessRate);
    }

    [Fact]
    public void QueueStatistics_ShouldHandlePartialProcessing()
    {
        // Arrange - Real-world scenario
        var stats = new QueueStatistics
        {
            TotalItems = 50,
            PendingItems = 20,
            ProcessingItems = 3,
            CompletedItems = 25,
            FailedItems = 2,
            TotalInputSize = 53687091200L, // 50GB
            TotalOutputSize = 32212254720L, // 30GB
            TotalProcessingTime = TimeSpan.FromHours(2),
            EstimatedTimeRemaining = TimeSpan.FromMinutes(30),
            AverageSpeed = 1.8
        };
        
        // Assert
        Assert.Equal(50, stats.TotalItems);
        Assert.Equal(20, stats.PendingItems);
        Assert.Equal(3, stats.ProcessingItems);
        Assert.Equal(25, stats.CompletedItems);
        Assert.Equal(2, stats.FailedItems);
        
        // Verify total adds up
        Assert.Equal(50, stats.PendingItems + stats.ProcessingItems + stats.CompletedItems + stats.FailedItems);
        
        // Calculate metrics
        Assert.Equal(21474836480L, stats.SpaceSaved); // 50GB - 30GB = 20GB
        Assert.Equal(0.6, stats.CompressionRatio, 2); // 30GB/50GB = 60%
        Assert.Equal(50, stats.SuccessRate); // 25/50 = 50%
        
        // Time metrics
        Assert.Equal(TimeSpan.FromHours(2), stats.TotalProcessingTime);
        Assert.Equal(TimeSpan.FromMinutes(30), stats.EstimatedTimeRemaining);
        Assert.Equal(1.8, stats.AverageSpeed);
    }

    [Fact]
    public void QueueStatistics_ShouldHandleNegativeSpaceSaved()
    {
        // Arrange - In case output is larger than input (shouldn't happen normally)
        var stats = new QueueStatistics
        {
            TotalInputSize = 536870912L,  // 512MB
            TotalOutputSize = 1073741824L // 1GB
        };
        
        // Assert
        Assert.Equal(-536870912L, stats.SpaceSaved); // Negative means file got larger
        Assert.Equal(2.0, stats.CompressionRatio, 2); // 100% larger
    }

    [Fact]
    public void QueueStatistics_ShouldCalculateAverageSpeed()
    {
        // Arrange
        var stats = new QueueStatistics
        {
            AverageSpeed = 0.5 // Half real-time speed
        };
        
        // Assert
        Assert.Equal(0.5, stats.AverageSpeed);
        
        // Test different speeds
        var fastStats = new QueueStatistics { AverageSpeed = 4.0 };
        var realtimeStats = new QueueStatistics { AverageSpeed = 1.0 };
        var slowStats = new QueueStatistics { AverageSpeed = 0.1 };
        
        Assert.Equal(4.0, fastStats.AverageSpeed);
        Assert.Equal(1.0, realtimeStats.AverageSpeed);
        Assert.Equal(0.1, slowStats.AverageSpeed);
    }
}
