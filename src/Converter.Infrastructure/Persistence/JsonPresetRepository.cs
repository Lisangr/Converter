using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Converter.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Converter.Infrastructure.Persistence
{
    public class JsonPresetRepository : IPresetRepository, IDisposable
    {
        private readonly string _presetsPath;
        private readonly ILogger<JsonPresetRepository> _logger;
        private readonly JsonSerializerOptions _jsonOptions;
        private bool _disposed;
        private List<ConversionProfile>? _cachedPresets;

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

        public async Task<IReadOnlyList<ConversionProfile>> GetAllPresetsAsync()
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
                _cachedPresets = await JsonSerializer.DeserializeAsync<List<ConversionProfile>>(fs, _jsonOptions) 
                               ?? GetDefaultPresets();
                
                return _cachedPresets;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading presets from {Path}", _presetsPath);
                return GetDefaultPresets();
            }
        }

        public async Task SavePresetAsync(ConversionProfile preset)
        {
            if (preset == null) throw new ArgumentNullException(nameof(preset));

            var presets = new List<ConversionProfile>((await GetAllPresetsAsync())!);
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

            var presets = new List<ConversionProfile>((await GetAllPresetsAsync())!);
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

        private static List<ConversionProfile> GetDefaultPresets()
        {
            return new List<ConversionProfile>
            {
                new("MP4 (H.264, 1080p)", "libx264", "aac", "192k", 23),
                new("MP4 (H.265, 4K)", "libx265", "aac", "256k", 28),
                new("WebM (VP9, 720p)", "libvpx-vp9", "libopus", "128k", 30),
                new("MP3 (High Quality)", "copy", "libmp3lame", "320k", 0),
                new("AAC (High Quality)", "copy", "aac", "256k", 0)
            };
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _cachedPresets = null;
            }
        }
    }
}
