using Converter.Domain.Models;

namespace Converter.Application.DTOs;

public class QueueItemDto
{
    public Guid Id { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string FileName => Path.GetFileName(FilePath);
    public long FileSizeBytes { get; set; }
    public string FileSize => FormatFileSize(FileSizeBytes);
    public TimeSpan Duration { get; set; }
    public string DurationFormatted => Duration.ToString("hh':'mm':'ss");
    public int Progress { get; set; }
    public string Status { get; set; } = "Pending";
    public bool IsStarred { get; set; }
    public int Priority { get; set; } = 3;
    public string? OutputPath { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime AddedAt { get; set; }
    public string AddedAtFormatted => AddedAt.ToString("g");

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double len = bytes;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
