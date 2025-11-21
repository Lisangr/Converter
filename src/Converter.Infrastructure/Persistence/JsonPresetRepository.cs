using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Converter.Application.Abstractions;
using Converter.Models;
using Microsoft.Extensions.Logging;

namespace Converter.Infrastructure.Persistence
{
    public class JsonPresetRepository : IPresetRepository, IDisposable
    {
        private readonly string _presetsPath;
        private readonly ILogger<JsonPresetRepository> _logger;
        private readonly JsonSerializerOptions _jsonOptions;
        private bool _disposed;
        private List<Converter.Models.ConversionProfile>? _cachedPresets;

        public JsonPresetRepository(ILogger<JsonPresetRepository> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appData, "VideoConverter");
            Directory.CreateDirectory(appFolder);
            _presetsPath = Path.Combine(appFolder, "presets.json");
            _jsonOptions = new JsonSerializerOptions { 
                WriteIndented = true, 
                PropertyNameCaseInsensitive = true 
            };
            
            _logger.LogInformation("Presets file path: {Path}", _presetsPath);
        }

        public async Task<IReadOnlyList<Converter.Models.ConversionProfile>> GetAllPresetsAsync()
        {
            if (_cachedPresets != null)
                return _cachedPresets;

            try
            {
                if (!File.Exists(_presetsPath))
                {
                    _cachedPresets = GetDefaultPresets();
                    await SavePresetsAsync();
                    return _cachedPresets;
                }

                await using var fs = File.OpenRead(_presetsPath);
                _cachedPresets = await JsonSerializer.DeserializeAsync<List<Converter.Models.ConversionProfile>>(fs, _jsonOptions) 
                               ?? GetDefaultPresets();
                
                return _cachedPresets;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading presets from {Path}", _presetsPath);
                return GetDefaultPresets();
            }
        }

        public async Task SavePresetAsync(Converter.Models.ConversionProfile preset)
        {
            if (preset == null) throw new ArgumentNullException(nameof(preset));

            var presets = new List<Converter.Models.ConversionProfile>((await GetAllPresetsAsync())!);
            var existingIndex = presets.FindIndex(p => p.Name == preset.Name);
            
            if (existingIndex >= 0)
                presets[existingIndex] = preset;
            else
                presets.Add(preset);

            _cachedPresets = presets;
            await SavePresetsAsync();
            
            _logger.LogInformation("Preset saved: {PresetName}", preset.Name);
        }

        public async Task DeletePresetAsync(string presetName)
        {
            if (string.IsNullOrWhiteSpace(presetName))
                throw new ArgumentException("Preset name cannot be empty", nameof(presetName));

            var presets = new List<Converter.Models.ConversionProfile>((await GetAllPresetsAsync())!);
            var count = presets.RemoveAll(p => p.Name == presetName);
            
            if (count > 0)
            {
                _cachedPresets = presets;
                await SavePresetsAsync();
                _logger.LogInformation("Preset deleted: {PresetName}", presetName);
            }
        }

        private async Task SavePresetsAsync()
        {
            try
            {
                await using var fs = File.Create(_presetsPath);
                await JsonSerializer.SerializeAsync(fs, _cachedPresets, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving presets to {Path}", _presetsPath);
                throw;
            }
        }

        private static List<Converter.Models.ConversionProfile> GetDefaultPresets()
{
    return new List<Converter.Models.ConversionProfile>
    {
        new() 
        {
            Id = Guid.NewGuid().ToString(),
            Name = "MP4 (H.264, 1080p)",
            VideoCodec = "libx264",
            AudioCodec = "aac",
            AudioBitrate = 192,
            CRF = 23,
            Width = 1920,
            Height = 1080,
            Format = "mp4"
        },
        new() 
        {
            Id = Guid.NewGuid().ToString(),
            Name = "MP4 (H.265, 4K)",
            VideoCodec = "libx265",
            AudioCodec = "aac",
            AudioBitrate = 256,
            CRF = 28,
            Width = 3840,
            Height = 2160,
            Format = "mp4"
        },
        new() 
        {
            Id = Guid.NewGuid().ToString(),
            Name = "WebM (VP9, 720p)",
            VideoCodec = "libvpx-vp9",
            AudioCodec = "libopus",
            AudioBitrate = 128,
            CRF = 30,
            Width = 1280,
            Height = 720,
            Format = "webm"
        },
        new() 
        {
            Id = Guid.NewGuid().ToString(),
            Name = "MP3 (High Quality)",
            VideoCodec = null,
            AudioCodec = "libmp3lame",
            AudioBitrate = 320,
            Format = "mp3"
        },
        new() 
        {
            Id = Guid.NewGuid().ToString(),
            Name = "AAC (High Quality)",
            VideoCodec = null,
            AudioCodec = "aac",
            AudioBitrate = 256,
            Format = "m4a"
        }
    };
}
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _cachedPresets = null;
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        ~JsonPresetRepository()
        {
            Dispose(disposing: false);
        }
    }
}
