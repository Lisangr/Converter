using System;

namespace Converter.Domain.Models;

public class QueueStatistics
{
    public int TotalItems { get; set; }
    public int PendingItems { get; set; }
    public int ProcessingItems { get; set; }
    public int CompletedItems { get; set; }
    public int FailedItems { get; set; }
    public long TotalInputSize { get; set; }
    public long TotalOutputSize { get; set; }
    public long SpaceSaved => TotalInputSize - TotalOutputSize;
    public double CompressionRatio => TotalInputSize > 0
        ? (double)TotalOutputSize / TotalInputSize
        : 0;
    public TimeSpan TotalProcessingTime { get; set; }
    public TimeSpan EstimatedTimeRemaining { get; set; }
    public double AverageSpeed { get; set; }
    public int SuccessRate => TotalItems > 0
        ? (int)Math.Round((double)CompletedItems / TotalItems * 100)
        : 0;
}
