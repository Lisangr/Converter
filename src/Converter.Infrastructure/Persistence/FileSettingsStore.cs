using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Converter.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Converter.Infrastructure.Persistence
{
    public class FileSettingsStore : ISettingsStore, IDisposable
    {
        private readonly string _settingsPath;
        private readonly ILogger<FileSettingsStore> _logger;
        private readonly JsonSerializerOptions _jsonOptions;
        private bool _disposed;

        public FileSettingsStore(ILogger<FileSettingsStore> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appData, "VideoConverter");
            Directory.CreateDirectory(appFolder);
            _settingsPath = Path.Combine(appFolder, "settings.json");
            _jsonOptions = new JsonSerializerOptions { WriteIndented = true, PropertyNameCaseInsensitive = true };
            
            _logger.LogInformation("Settings file path: {Path}", _settingsPath);
        }

        public async Task<string?> GetFfmpegPathAsync()
        {
            try
            {
                if (!File.Exists(_settingsPath))
                    return null;

                await using var fs = File.OpenRead(_settingsPath);
                var settings = await JsonSerializer.DeserializeAsync<SettingsDto>(fs, _jsonOptions);
                return settings?.FfmpegPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading settings from {Path}", _settingsPath);
                return null;
            }
        }

        public async Task SetFfmpegPathAsync(string path)
        {
            try
            {
                SettingsDto settings;
                
                if (File.Exists(_settingsPath))
                {
                    await using (var fs = File.OpenRead(_settingsPath))
                    {
                        settings = await JsonSerializer.DeserializeAsync<SettingsDto>(fs, _jsonOptions) ?? new SettingsDto();
                    }
                }
                else
                {
                    settings = new SettingsDto();
                }

                settings.FfmpegPath = path;

                await using (var fs = File.Create(_settingsPath))
                {
                    await JsonSerializer.SerializeAsync(fs, settings, _jsonOptions);
                }
                
                _logger.LogInformation("FFmpeg path updated to: {Path}", path);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving FFmpeg path to {Path}", _settingsPath);
                throw;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }

        private class SettingsDto
        {
            public string? FfmpegPath { get; set; }
        }
    }
}
