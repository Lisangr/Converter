using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Converter.Application.Models;

namespace Converter.Services
{
    public class XmlPresetLoader
    {
        private readonly string _presetsDirectory;

        public XmlPresetLoader(string? presetsDirectory = null)
        {
            if (!string.IsNullOrEmpty(presetsDirectory))
            {
                _presetsDirectory = presetsDirectory;
            }
            else
            {
                // Пробуем несколько вариантов пути
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var candidates = new[]
                {
                    Path.Combine(baseDir, "Presets"),
                    Path.Combine(baseDir, "..", "Presets"),
                    Path.Combine(baseDir, "..", "..", "Presets"),
                    Path.Combine(baseDir, "..", "..", "..", "Presets"),
                    Path.Combine(Path.GetDirectoryName(baseDir), "Presets")
                };
                
                foreach (var candidate in candidates)
                {
                    var fullPath = Path.GetFullPath(candidate);
                    if (Directory.Exists(fullPath))
                    {
                        _presetsDirectory = fullPath;
                        break;
                    }
                }
                
                // Если ничего не найдено, используем последний вариант
                if (string.IsNullOrEmpty(_presetsDirectory))
                {
                    _presetsDirectory = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "Presets"));
                }
            }
            
            // Создаем директорию если не существует
            if (!Directory.Exists(_presetsDirectory))
            {
                try
                {
                    Directory.CreateDirectory(_presetsDirectory);
                    System.Diagnostics.Debug.WriteLine($"Created presets directory: {_presetsDirectory}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to create presets directory: {ex.Message}");
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"XmlPresetLoader initialized with path: {_presetsDirectory}");
        }

        public List<PresetProfile> LoadAllPresets()
        {
            var presets = new List<PresetProfile>();

            System.Diagnostics.Debug.WriteLine($"Presets directory: {_presetsDirectory}");
            System.Diagnostics.Debug.WriteLine($"Directory exists: {Directory.Exists(_presetsDirectory)}");

            if (!Directory.Exists(_presetsDirectory))
            {
                Directory.CreateDirectory(_presetsDirectory);
                System.Diagnostics.Debug.WriteLine($"Created presets directory: {_presetsDirectory}");
                return presets;
            }

            var xmlFiles = Directory.GetFiles(_presetsDirectory, "*.xml", SearchOption.TopDirectoryOnly);
            System.Diagnostics.Debug.WriteLine($"Found XML files: {xmlFiles.Length}");
            
            foreach (var file in xmlFiles)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"Loading presets from: {file}");
                    var filePresets = LoadFromFile(file);
                    presets.AddRange(filePresets);
                    System.Diagnostics.Debug.WriteLine($"Loaded {filePresets.Count} presets from {Path.GetFileName(file)}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading presets from {file}: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                }
            }

            System.Diagnostics.Debug.WriteLine($"Total XML presets loaded: {presets.Count}");
            
            // Если пресеты не найдены, создадим тестовые
            if (presets.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("No presets found, creating fallback presets");
                presets.AddRange(CreateFallbackPresets());
            }
            
            return presets;
        }
        
        private List<PresetProfile> CreateFallbackPresets()
        {
            return new List<PresetProfile>
            {
                new PresetProfile
                {
                    Id = "fallback_youtube_1080p",
                    Name = "YouTube 1080p (Fallback)",
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
                    ColorHex = "#FF0000",
                    IsPro = false
                },
                new PresetProfile
                {
                    Id = "fallback_instagram_story",
                    Name = "Instagram Story (Fallback)",
                    Category = "Social Media",
                    Icon = "📱",
                    Description = "1080x1920, H.264, 5Mbps",
                    Width = 1080,
                    Height = 1920,
                    VideoCodec = "libx264",
                    Bitrate = 5000,
                    Format = "mp4",
                    IncludeAudio = true,
                    AudioCodec = "aac",
                    AudioBitrate = 128,
                    ColorHex = "#E4405F",
                    IsPro = false
                }
            };
        }

        public List<PresetProfile> LoadFromFile(string filePath)
        {
            var presets = new List<PresetProfile>();

            try
            {
                var doc = XDocument.Load(filePath);
                
                foreach (var categoryElement in doc.Descendants("Category"))
                {
                    var categoryName = categoryElement.Attribute("Name")?.Value ?? "Unknown";
                    
                    foreach (var presetElement in categoryElement.Descendants("Preset"))
                    {
                        var preset = ParsePresetElement(presetElement, categoryName);
                        if (preset != null)
                        {
                            presets.Add(preset);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load presets from {filePath}: {ex.Message}", ex);
            }

            return presets;
        }

        private PresetProfile? ParsePresetElement(XElement presetElement, string category)
        {
            try
            {
                var preset = new PresetProfile
                {
                    Id = presetElement.Attribute("Id")?.Value ?? Guid.NewGuid().ToString(),
                    Name = presetElement.Attribute("Name")?.Value ?? "Unknown",
                    Category = category,
                    Icon = presetElement.Attribute("Icon")?.Value ?? "⚙️",
                    Description = presetElement.Attribute("Description")?.Value ?? ""
                };

                // Parse Video settings
                var videoElement = presetElement.Element("Video");
                if (videoElement != null)
                {
                    preset.Width = ParseNullableInt(videoElement.Attribute("Width")?.Value);
                    preset.Height = ParseNullableInt(videoElement.Attribute("Height")?.Value);
                    preset.VideoCodec = videoElement.Attribute("Codec")?.Value ?? "";
                    preset.Bitrate = ParseNullableInt(videoElement.Attribute("Bitrate")?.Value);
                    preset.CRF = ParseNullableInt(videoElement.Attribute("CRF")?.Value);
                    preset.Format = videoElement.Attribute("Format")?.Value ?? "";
                }

                // Parse Audio settings
                var audioElement = presetElement.Element("Audio");
                if (audioElement != null)
                {
                    preset.IncludeAudio = ParseBool(audioElement.Attribute("Enabled")?.Value, true);
                    preset.AudioCodec = audioElement.Attribute("Codec")?.Value ?? "";
                    preset.AudioBitrate = ParseNullableInt(audioElement.Attribute("Bitrate")?.Value);
                }

                // Parse Constraints
                var constraintsElement = presetElement.Element("Constraints");
                if (constraintsElement != null)
                {
                    preset.MaxFileSizeMB = ParseNullableLong(constraintsElement.Attribute("MaxFileSizeMB")?.Value);
                    preset.MaxDurationSeconds = ParseNullableInt(constraintsElement.Attribute("MaxDurationSeconds")?.Value);
                }

                // Parse UI settings
                var uiElement = presetElement.Element("UI");
                if (uiElement != null)
                {
                    preset.ColorHex = uiElement.Attribute("ColorHex")?.Value;
                    preset.IsPro = ParseBool(uiElement.Attribute("IsPro")?.Value, false);
                }

                return preset;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing preset element: {ex.Message}");
                return null;
            }
        }

        private static int? ParseNullableInt(string? value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Equals("null", StringComparison.OrdinalIgnoreCase))
                return null;
            
            if (int.TryParse(value, out int result))
                return result;
            
            return null;
        }

        private static long? ParseNullableLong(string? value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Equals("null", StringComparison.OrdinalIgnoreCase))
                return null;
            
            if (long.TryParse(value, out long result))
                return result;
            
            return null;
        }

        private static bool ParseBool(string? value, bool defaultValue)
        {
            if (string.IsNullOrWhiteSpace(value))
                return defaultValue;
            
            return bool.TryParse(value, out bool result) ? result : defaultValue;
        }

        public void SavePresetToFile(PresetProfile preset, string filePath)
        {
            try
            {
                var doc = new XDocument(
                    new XElement("Presets",
                        new XElement("Category",
                            new XAttribute("Name", preset.Category),
                            new XElement("Preset",
                                new XAttribute("Id", preset.Id),
                                new XAttribute("Name", preset.Name),
                                new XAttribute("Icon", preset.Icon),
                                new XAttribute("Description", preset.Description),
                                new XElement("Video",
                                    preset.Width.HasValue ? new XAttribute("Width", preset.Width.Value) : null,
                                    preset.Height.HasValue ? new XAttribute("Height", preset.Height.Value) : null,
                                    !string.IsNullOrEmpty(preset.VideoCodec) ? new XAttribute("Codec", preset.VideoCodec) : null,
                                    preset.Bitrate.HasValue ? new XAttribute("Bitrate", preset.Bitrate.Value) : null,
                                    preset.CRF.HasValue ? new XAttribute("CRF", preset.CRF.Value) : null,
                                    !string.IsNullOrEmpty(preset.Format) ? new XAttribute("Format", preset.Format) : null
                                ),
                                new XElement("Audio",
                                    new XAttribute("Enabled", preset.IncludeAudio),
                                    !string.IsNullOrEmpty(preset.AudioCodec) ? new XAttribute("Codec", preset.AudioCodec) : null,
                                    preset.AudioBitrate.HasValue ? new XAttribute("Bitrate", preset.AudioBitrate.Value) : null
                                ),
                                new XElement("Constraints",
                                    preset.MaxFileSizeMB.HasValue ? new XAttribute("MaxFileSizeMB", preset.MaxFileSizeMB.Value) : null,
                                    preset.MaxDurationSeconds.HasValue ? new XAttribute("MaxDurationSeconds", preset.MaxDurationSeconds.Value) : null
                                ),
                                new XElement("UI",
                                    !string.IsNullOrEmpty(preset.ColorHex) ? new XAttribute("ColorHex", preset.ColorHex) : null,
                                    new XAttribute("IsPro", preset.IsPro)
                                )
                            )
                        )
                    )
                );

                doc.Save(filePath);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save preset to {filePath}: {ex.Message}", ex);
            }
        }
    }
}
