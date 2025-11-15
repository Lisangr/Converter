using System.Text.Json.Serialization;

namespace Converter.Models
{
    public class PresetProfile
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;            // "Instagram Story"
        public string Category { get; set; } = string.Empty;        // "Social Media"
        public string Icon { get; set; } = string.Empty;            // Emoji or path
        public string Description { get; set; } = string.Empty;     // "Vertical 1080x1920"

        // Video
        public int? Width { get; set; }             // null = keep
        public int? Height { get; set; }
        public string? VideoCodec { get; set; }      // "libx264"
        public int? Bitrate { get; set; }           // kbps
        public int? CRF { get; set; }               // for x264/x265
        public string? Format { get; set; }         // "mp4"

        // Audio
        public bool IncludeAudio { get; set; } = true;
        public string? AudioCodec { get; set; }     // "aac"
        public int? AudioBitrate { get; set; }      // kbps

        // Constraints
        public long? MaxFileSizeMB { get; set; }
        public int? MaxDurationSeconds { get; set; }

        // UI
        public string? ColorHex { get; set; }
        public bool IsPro { get; set; }
    }
}
