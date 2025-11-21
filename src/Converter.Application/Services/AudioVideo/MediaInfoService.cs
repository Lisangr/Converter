using System;
using System.Threading.Tasks;
using Converter.Application.Abstractions;

namespace Converter.Application.Services.AudioVideo;

/// <summary>
/// Провайдер информации о медиафайлах.
/// </summary>
public interface IMediaInfoProvider
{
    /// <summary>
    /// Получает информацию о медиафайле.
    /// </summary>
    Task<MediaInfo> GetMediaInfoAsync(string filePath);
}

/// <summary>
/// Информация о медиафайле.
/// </summary>
public class MediaInfo
{
    public string FilePath { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public long FileSize { get; set; }
    public string? VideoCodec { get; set; }
    public string? AudioCodec { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? Bitrate { get; set; }
    public int? FrameRate { get; set; }
}

/// <summary>
/// Сервис редактирования временной шкалы.
/// </summary>
public class TimelineEditingService
{
    private readonly IMediaInfoProvider _mediaInfoProvider;
    private readonly IFFmpegExecutor _ffmpegExecutor;

    public TimelineEditingService(IMediaInfoProvider mediaInfoProvider, IFFmpegExecutor ffmpegExecutor)
    {
        _mediaInfoProvider = mediaInfoProvider ?? throw new ArgumentNullException(nameof(mediaInfoProvider));
        _ffmpegExecutor = ffmpegExecutor ?? throw new ArgumentNullException(nameof(ffmpegExecutor));
    }

    public async Task<bool> SplitTimelineAsync(string inputPath, TimeSpan startTime, TimeSpan endTime, string outputPath)
    {
        // Заглушка для тестов
        await Task.CompletedTask;
        return true;
    }
}

/// <summary>
/// Сервис разбивки временной шкалы.
/// </summary>
public class TimelineSplitService
{
    private readonly IMediaInfoProvider _mediaInfoProvider;
    private readonly IFFmpegExecutor _ffmpegExecutor;

    public TimelineSplitService(IMediaInfoProvider mediaInfoProvider, IFFmpegExecutor ffmpegExecutor)
    {
        _mediaInfoProvider = mediaInfoProvider ?? throw new ArgumentNullException(nameof(mediaInfoProvider));
        _ffmpegExecutor = ffmpegExecutor ?? throw new ArgumentNullException(nameof(ffmpegExecutor));
    }
}