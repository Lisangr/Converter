using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Converter.Models;

namespace Converter.Services
{
    public class PresetService
    {
        private readonly List<PresetProfile> _builtIn;
        private readonly XmlPresetLoader _xmlLoader;
        private List<PresetProfile>? _xmlPresets;

        public PresetService()
        {
            _builtIn = CreateBuiltIn();
            _xmlLoader = new XmlPresetLoader();
            LoadXmlPresets();
        }

        public List<PresetProfile> GetAllPresets()
        {
            var allPresets = new List<PresetProfile>(_builtIn);
            
            System.Diagnostics.Debug.WriteLine($"Built-in presets: {_builtIn.Count}");
            
            if (_xmlPresets != null)
            {
                allPresets.AddRange(_xmlPresets);
                System.Diagnostics.Debug.WriteLine($"XML presets: {_xmlPresets.Count}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("XML presets is null");
            }

            var result = allPresets.DistinctBy(p => p.Id).ToList();
            System.Diagnostics.Debug.WriteLine($"Total presets after merge: {result.Count}");
            
            return result;
        }

        public PresetProfile? GetPresetById(string id)
        {
            return GetAllPresets().FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));
        }

        public List<PresetProfile> GetPresetsByCategory(string category)
        {
            return GetAllPresets()
                .Where(p => string.Equals(p.Category, category, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public void SavePresetToFile(PresetProfile preset, string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            
            if (extension == ".xml")
            {
                _xmlLoader.SavePresetToFile(preset, filePath);
            }
            else
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(preset, options);
                File.WriteAllText(filePath, json);
            }
        }

        public PresetProfile LoadPresetFromFile(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            
            if (extension == ".xml")
            {
                var presets = _xmlLoader.LoadFromFile(filePath);
                return presets.FirstOrDefault() ?? new PresetProfile();
            }
            else
            {
                var json = File.ReadAllText(filePath);
                var preset = JsonSerializer.Deserialize<PresetProfile>(json) ?? new PresetProfile();
                return preset;
            }
        }

        public void ReloadXmlPresets()
        {
            LoadXmlPresets();
        }

        private void LoadXmlPresets()
        {
            try
            {
                _xmlPresets = _xmlLoader.LoadAllPresets();
                
                if (_xmlPresets.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("No XML presets loaded, using built-in presets only");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load XML presets: {ex.Message}");
                _xmlPresets = new List<PresetProfile>();
            }
        }

        private static List<PresetProfile> CreateBuiltIn()
        {
            return new List<PresetProfile>
            {
                new PresetProfile
                {
                    Id = "instagram_story",
                    Name = "Instagram Story",
                    Category = "Social Media",
                    Icon = "📱",
                    Description = "Вертикальное видео 1080x1920",
                    Width = 1080,
                    Height = 1920,
                    VideoCodec = "libx264",
                    Bitrate = 15000,
                    CRF = null,
                    Format = "mp4",
                    IncludeAudio = true,
                    AudioCodec = "aac",
                    AudioBitrate = 192,
                    MaxDurationSeconds = 15 * 60,
                    ColorHex = "#E1306C"
                },
                new PresetProfile
                {
                    Id = "instagram_feed",
                    Name = "Instagram Feed",
                    Category = "Social Media",
                    Icon = "📱",
                    Description = "Квадрат 1080x1080",
                    Width = 1080,
                    Height = 1080,
                    VideoCodec = "libx264",
                    Bitrate = 12000,
                    Format = "mp4",
                    IncludeAudio = true,
                    AudioCodec = "aac",
                    AudioBitrate = 192,
                    ColorHex = "#C13584"
                },
                new PresetProfile
                {
                    Id = "instagram_reels",
                    Name = "Instagram Reels",
                    Category = "Social Media",
                    Icon = "📱",
                    Description = "Вертикальное 1080x1920, до 90 сек",
                    Width = 1080,
                    Height = 1920,
                    VideoCodec = "libx264",
                    Bitrate = 25000,
                    Format = "mp4",
                    IncludeAudio = true,
                    AudioCodec = "aac",
                    AudioBitrate = 192,
                    MaxDurationSeconds = 90,
                    ColorHex = "#FD1D1D"
                },
                new PresetProfile
                {
                    Id = "youtube_1080p",
                    Name = "YouTube 1080p",
                    Category = "Video Platforms",
                    Icon = "🎬",
                    Description = "1920x1080, H.264, 8Mbps",
                    Width = 1920,
                    Height = 1080,
                    VideoCodec = "libx264",
                    Bitrate = 8000,
                    Format = "mp4",
                    IncludeAudio = true,
                    AudioCodec = "aac",
                    AudioBitrate = 256,
                    ColorHex = "#FF0000"
                },
                new PresetProfile
                {
                    Id = "youtube_4k",
                    Name = "YouTube 4K",
                    Category = "Video Platforms",
                    Icon = "🎬",
                    Description = "3840x2160, HEVC, 45Mbps",
                    Width = 3840,
                    Height = 2160,
                    VideoCodec = "libx265",
                    Bitrate = 45000,
                    Format = "mp4",
                    IncludeAudio = true,
                    AudioCodec = "aac",
                    AudioBitrate = 384,
                    ColorHex = "#CC0000",
                    IsPro = true
                },
                new PresetProfile
                {
                    Id = "discord",
                    Name = "Discord",
                    Category = "Compression",
                    Icon = "💬",
                    Description = "1280x720, VP9, 2Mbps, максимум 25MB",
                    Width = 1280,
                    Height = 720,
                    VideoCodec = "libvpx-vp9",
                    Bitrate = 2000,
                    Format = "webm",
                    IncludeAudio = true,
                    AudioCodec = "libopus",
                    AudioBitrate = 128,
                    MaxFileSizeMB = 25,
                    ColorHex = "#5865F2"
                },
                new PresetProfile
                {
                    Id = "email",
                    Name = "Email",
                    Category = "Compression",
                    Icon = "📧",
                    Description = "854x480, H.264, 1Mbps, максимум 10MB",
                    Width = 854,
                    Height = 480,
                    VideoCodec = "libx264",
                    Bitrate = 1000,
                    Format = "mp4",
                    IncludeAudio = true,
                    AudioCodec = "aac",
                    AudioBitrate = 128,
                    MaxFileSizeMB = 10,
                    ColorHex = "#0078D4"
                },
                new PresetProfile
                {
                    Id = "archive_hq",
                    Name = "Archive HQ",
                    Category = "Compression",
                    Icon = "🖥️",
                    Description = "Оригинал. HEVC CRF 23",
                    Width = null,
                    Height = null,
                    VideoCodec = "libx265",
                    CRF = 23,
                    Format = "mkv",
                    IncludeAudio = true,
                    AudioCodec = "aac",
                    AudioBitrate = 256,
                    ColorHex = "#2D2D2D"
                },
                new PresetProfile
                {
                    Id = "quick_compress",
                    Name = "Quick Compress",
                    Category = "Compression",
                    Icon = "⚡",
                    Description = "70% от оригинала, H.264, CRF 28",
                    Width = null,
                    Height = null,
                    VideoCodec = "libx264",
                    CRF = 28,
                    Format = "mp4",
                    IncludeAudio = true,
                    AudioCodec = "aac",
                    AudioBitrate = 128,
                    ColorHex = "#FFB703"
                }
            };
        }
    }
}
