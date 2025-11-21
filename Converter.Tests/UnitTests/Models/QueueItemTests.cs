using Xunit;
using Converter.Domain.Models;

namespace Converter.Tests.UnitTests.Models;

public class QueueItemTests
{
    [Fact]
    public void QueueItem_ShouldDefaultToPending()
    {
        // Arrange & Act
        var item = new QueueItem();
        
        // Assert
        Assert.Equal(ConversionStatus.Pending, item.Status);
        Assert.Equal(0, item.Progress);
        Assert.True(item.Id != Guid.Empty); // Should have an ID
    }

    [Fact]
    public void QueueItem_ShouldTrackProgress()
    {
        // Arrange
        var item = new QueueItem
        {
            Status = ConversionStatus.Processing
        };
        
        // Act & Assert - Track progress from 0 to 100
        for (int progress = 0; progress <= 100; progress += 10)
        {
            item.Progress = progress;
            Assert.Equal(progress, item.Progress);
        }
        
        // Test boundary values
        item.Progress = 0;
        Assert.Equal(0, item.Progress);
        
        item.Progress = 100;
        Assert.Equal(100, item.Progress);
    }

    [Fact]
    public void QueueItem_ShouldCaptureErrorMessages()
    {
        // Arrange & Act
        var item = new QueueItem
        {
            Status = ConversionStatus.Failed,
            ErrorMessage = "FFmpeg error: Invalid input format"
        };
        
        // Assert
        Assert.Equal(ConversionStatus.Failed, item.Status);
        Assert.Equal("FFmpeg error: Invalid input format", item.ErrorMessage);
    }

    [Fact]
    public void QueueItem_ShouldHandleAllStatuses()
    {
        // Test all possible statuses
        var statuses = Enum.GetValues<ConversionStatus>();
        
        foreach (var status in statuses)
        {
            var item = new QueueItem { Status = status };
            Assert.Equal(status, item.Status);
        }
    }

    [Fact]
    public void QueueItem_ShouldTrackTiming()
    {
        // Arrange
        var item = new QueueItem();
        var startTime = DateTime.Now;
        
        // Act
        item.StartedAt = startTime;
        System.Threading.Thread.Sleep(10); // Small delay
        item.CompletedAt = DateTime.Now;
        
        // Assert
        Assert.NotNull(item.StartedAt);
        Assert.NotNull(item.CompletedAt);
        Assert.True(item.CompletedAt > item.StartedAt);
        
        // Test ConversionDuration calculation
        var duration = item.ConversionDuration;
        Assert.NotNull(duration);
        Assert.True(duration.Value.TotalMilliseconds > 0);
    }

    [Fact]
    public void QueueItem_ShouldHandleNullTiming()
    {
        // Arrange
        var item = new QueueItem();
        
        // Assert - Should handle null timing gracefully
        Assert.Null(item.StartedAt);
        Assert.Null(item.CompletedAt);
        Assert.Null(item.ConversionDuration);
    }

    [Fact]
    public void QueueItem_ShouldTrackFileInformation()
    {
        // Arrange
        var inputPath = "/input/video.mp4";
        var outputPath = "/output/compressed.mp4";
        var fileSize = 10485760L; // 10MB
        
        // Act
        var item = new QueueItem
        {
            InputPath = inputPath,
            OutputPath = outputPath,
            FileSizeBytes = fileSize,
            OutputFileSizeBytes = 5242880L // 5MB compressed
        };
        
        // Assert
        Assert.Equal(inputPath, item.InputPath);
        Assert.Equal(outputPath, item.OutputPath);
        Assert.Equal(fileSize, item.FileSizeBytes);
        Assert.Equal(5242880L, item.OutputFileSizeBytes);
        Assert.Equal("video.mp4", item.FileName);
    }

    [Fact]
    public void QueueItem_ShouldHandleFilePathProperties()
    {
        // Arrange
        var path = "/test/path/video.mp4";
        var item = new QueueItem();
        
        // Act
        item.FilePath = path;
        
        // Assert - FilePath should be an alias for InputPath
        Assert.Equal(path, item.FilePath);
        Assert.Equal(path, item.InputPath);
    }

    [Fact]
    public void QueueItem_ShouldHandleAddedAtProperty()
    {
        // Arrange
        var item = new QueueItem();
        var addedTime = DateTime.Now.AddMinutes(-5);
        
        // Act
        item.AddedAt = addedTime;
        
        // Assert - AddedAt should be an alias for CreatedAt
        Assert.Equal(addedTime, item.AddedAt);
        Assert.Equal(addedTime, item.CreatedAt);
    }

    [Fact]
    public void QueueItem_ShouldSupportSettings()
    {
        // Arrange
        var settings = new ConversionSettings
        {
            VideoCodec = "libx264",
            AudioCodec = "aac",
            Crf = 23
        };
        
        // Act
        var item = new QueueItem
        {
            Settings = settings
        };
        
        // Assert
        Assert.NotNull(item.Settings);
        Assert.Equal("libx264", item.Settings.VideoCodec);
        Assert.Equal("aac", item.Settings.AudioCodec);
        Assert.Equal(23, item.Settings.Crf);
    }

    [Fact]
    public void QueueItem_ShouldSupportAdditionalProperties()
    {
        // Arrange & Act
        var item = new QueueItem
        {
            ProfileId = "youtube-hd-preset",
            Priority = 5,
            IsStarred = true,
            OutputDirectory = "/output/youtube"
        };
        
        // Assert
        Assert.Equal("youtube-hd-preset", item.ProfileId);
        Assert.Equal(5, item.Priority);
        Assert.True(item.IsStarred);
        Assert.Equal("/output/youtube", item.OutputDirectory);
    }

    [Fact]
    public void QueueItem_ShouldCalculateCompressionRatio()
    {
        // Arrange
        var item = new QueueItem
        {
            FileSizeBytes = 10485760L, // 10MB original
            OutputFileSizeBytes = 5242880L // 5MB compressed
        };
        
        // Act
        var compressionRatio = (double)item.OutputFileSizeBytes / item.FileSizeBytes;
        
        // Assert
        Assert.Equal(0.5, compressionRatio, 2); // 50% compression
        Assert.Equal(5242880L, item.OutputFileSizeBytes);
        Assert.Equal(10485760L, item.FileSizeBytes);
    }

    [Fact]
    public void QueueItem_ShouldHandleNullOutputSize()
    {
        // Arrange
        var item = new QueueItem
        {
            FileSizeBytes = 10485760L,
            OutputFileSizeBytes = null
        };
        
        // Assert
        Assert.Null(item.OutputFileSizeBytes);
        Assert.True(item.FileSizeBytes > 0);
    }

    [Fact]
    public void QueueItem_ShouldHaveUniqueId()
    {
        // Act - Create multiple items
        var item1 = new QueueItem();
        var item2 = new QueueItem();
        var item3 = new QueueItem();
        
        // Assert - All should have different IDs
        Assert.NotEqual(item1.Id, item2.Id);
        Assert.NotEqual(item1.Id, item3.Id);
        Assert.NotEqual(item2.Id, item3.Id);
        
        // All should be non-empty GUIDs
        Assert.NotEqual(Guid.Empty, item1.Id);
        Assert.NotEqual(Guid.Empty, item2.Id);
        Assert.NotEqual(Guid.Empty, item3.Id);
    }

    [Fact]
    public void QueueItem_ShouldTrackCreationTime()
    {
        // Arrange
        var beforeCreation = DateTime.Now;
        
        // Act
        var item = new QueueItem();
        var afterCreation = DateTime.Now;
        
        // Assert
        Assert.True(item.CreatedAt >= beforeCreation);
        Assert.True(item.CreatedAt <= afterCreation);
    }

    [Fact]
    public void QueueItem_ShouldHandleVariousFileTypes()
    {
        // Test different file types
        var fileTypes = new[]
        {
            ("video.mp4", "MP4 Video"),
            ("movie.avi", "AVI Video"),
            ("clip.mkv", "MKV Video"),
            ("stream.mov", "MOV Video"),
            ("recording.wmv", "WMV Video"),
            ("clip.webm", "WebM Video")
        };

        foreach (var (fileName, description) in fileTypes)
        {
            var item = new QueueItem
            {
                InputPath = $"/input/{fileName}",
                FileSizeBytes = 1024000 // 1MB for all
            };
            
            Assert.Equal(fileName, item.FileName);
            Assert.Equal($"/input/{fileName}", item.InputPath);
        }
    }
}
