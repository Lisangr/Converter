namespace Converter.Domain.Models;

public sealed class ConversionSettings
{
    public string? VideoCodec { get; set; } = "libx264";
    public int? Bitrate { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public string? AudioCodec { get; set; } = "aac";
    public int? AudioBitrate { get; set; }
    public string? PresetName { get; set; }
    public string? ContainerFormat { get; set; } = "mp4";
    public int? Crf { get; set; }
    public bool EnableAudio { get; set; } = true;
    public bool CopyVideo { get; set; }
    public bool CopyAudio { get; set; }
    public bool UseHardwareAcceleration { get; set; }
    public int? Threads { get; set; }
    public AudioProcessingOptions? AudioProcessing { get; set; }
}
