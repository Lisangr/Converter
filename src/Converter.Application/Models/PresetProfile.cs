using System.Text.Json.Serialization;

namespace Converter.Application.Models;

/// <summary>
/// Профиль пресета конвертации с настройками видео и аудио.
/// Используется для хранения и применения предустановленных настроек конвертации.
/// </summary>
public class PresetProfile
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    
    // Video settings
    public string? VideoCodec { get; set; }
    public int? Bitrate { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? CRF { get; set; }
    public string? Format { get; set; }
    
    // Audio settings
    public string? AudioCodec { get; set; }
    public int? AudioBitrate { get; set; }
    public bool IncludeAudio { get; set; }
    
    // Constraints
    public long? MaxFileSizeMB { get; set; }
    public int? MaxDurationSeconds { get; set; }

    // UI settings
    public string? Icon { get; set; }
    public string? ColorHex { get; set; }
    public bool IsPro { get; set; }
}