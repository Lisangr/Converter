namespace Converter.Domain.Models;

public sealed class ConversionSettings
{
    public string ContainerFormat { get; set; } = "mp4";
    public string VideoCodec { get; set; } = "libx264";
    public string AudioCodec { get; set; } = "aac";
    public int? AudioBitrate { get; set; } // in kbps
    public int? Crf { get; set; } // quality factor for x264/x265
    public bool EnableAudio { get; set; } = true;
    public string? PresetName { get; set; }
}
