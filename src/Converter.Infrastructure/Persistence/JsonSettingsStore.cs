using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Converter.Application.Abstractions;
using Converter.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Converter.Infrastructure.Persistence;

public class SettingsOptions
{
    public string? SettingsPath { get; set; }
}

public class JsonSettingsStore : ISettingsStore
{
    private readonly string _settingsPath;
    private readonly ILogger<JsonSettingsStore> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _fileLock = new(1, 1);
    private Dictionary<string, JsonElement>? _settingsCache;

    public JsonSettingsStore(
        IOptions<SettingsOptions> options,
        ILogger<JsonSettingsStore> logger)
    {
        _settingsPath = options.Value.SettingsPath 
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Converter",
                "settings.json");
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Migrate old settings if they exist
        _ = MigrateOldSettingsAsync();
    }

    private async Task<Dictionary<string, JsonElement>> ReadSettingsFromFileAsync(CancellationToken ct)
    {
        if (!File.Exists(_settingsPath))
            return new Dictionary<string, JsonElement>();

        var json = await File.ReadAllTextAsync(_settingsPath, ct);
        return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, _jsonOptions) 
               ?? new Dictionary<string, JsonElement>();
    }

    private async Task<Dictionary<string, JsonElement>> GetSettingsAsync(CancellationToken ct)
    {
        if (_settingsCache != null) 
            return _settingsCache;

        await _fileLock.WaitAsync(ct);
        try
        {
            // Double-check cache after acquiring lock
            if (_settingsCache != null) 
                return _settingsCache;

            _settingsCache = await ReadSettingsFromFileAsync(ct);
            return _settingsCache;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<T?> GetSettingAsync<T>(string key, T? defaultValue = default, CancellationToken ct = default)
    {
        try
        {
            var settings = await GetSettingsAsync(ct);
            
            if (!settings.TryGetValue(key, out var value))
                return defaultValue;
                
            return value.Deserialize<T>(_jsonOptions) ?? defaultValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading setting {Key}", key);
            return defaultValue;
        }
    }

    public async Task SetSettingAsync<T>(string key, T value, CancellationToken ct = default)
    {
        await _fileLock.WaitAsync(ct);
        try
        {
            // Read current settings from file to avoid overwriting concurrent changes
            var settings = await ReadSettingsFromFileAsync(ct);
            
            settings[key] = JsonSerializer.SerializeToElement(value, _jsonOptions);

            var newJson = JsonSerializer.Serialize(settings, _jsonOptions);
            await File.WriteAllTextAsync(_settingsPath, newJson, ct);

            // Invalidate cache
            _settingsCache = null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing setting {Key}", key);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<AppSettings> GetAppSettingsAsync(CancellationToken ct = default)
    {
        return await GetSettingAsync("app", new AppSettings(), ct) ?? new AppSettings();
    }

    public async Task SaveAppSettingsAsync(AppSettings settings, CancellationToken ct = default)
    {
        await SetSettingAsync("app", settings, ct);
    }

    public async Task<UserPreferences> GetUserPreferencesAsync(CancellationToken ct = default)
    {
        return await GetSettingAsync("user", new UserPreferences(), ct) ?? new UserPreferences();
    }

    public async Task SaveUserPreferencesAsync(UserPreferences preferences, CancellationToken ct = default)
    {
        await SetSettingAsync("user", preferences, ct);
    }

    public async Task<string?> GetSecureValueAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var safeKey = string.Join("_", key.Split(Path.GetInvalidFileNameChars()));
            var filePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Converter",
                "Secure",
                $"{safeKey}.dat");

            if (!File.Exists(filePath))
                return null;

            var encrypted = await File.ReadAllBytesAsync(filePath, ct);
            var decrypted = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decrypted);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve secure value for key: {Key}", key);
            return null;
        }
    }

    public async Task SetSecureValueAsync(string key, string value, CancellationToken ct = default)
    {
        try
        {
            var safeKey = string.Join("_", key.Split(Path.GetInvalidFileNameChars()));
            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Converter",
                "Secure");
            
            Directory.CreateDirectory(directory);
            
            var filePath = Path.Combine(directory, $"{safeKey}.dat");
            var encrypted = ProtectedData.Protect(
                Encoding.UTF8.GetBytes(value),
                null,
                DataProtectionScope.CurrentUser);
                
            await File.WriteAllBytesAsync(filePath, encrypted, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store secure value for key: {Key}", key);
            throw;
        }
    }

    private async Task MigrateOldSettingsAsync()
    {
        try
        {
            var oldPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "VideoConverter",
                "settings.json");
                
            if (File.Exists(oldPath))
            {
                _logger.LogInformation("Migrating old settings file from {OldPath}", oldPath);
                var oldSettings = await File.ReadAllTextAsync(oldPath);
                var oldData = JsonSerializer.Deserialize<OldSettingsDto>(oldSettings, _jsonOptions);
                
                if (oldData != null)
                {
                    if (!string.IsNullOrEmpty(oldData.FfmpegPath))
                        await SetSettingAsync("ffmpegPath", oldData.FfmpegPath);
                        
                    if (!string.IsNullOrEmpty(oldData.ThemeName))
                        await SetSettingAsync("theme", new { Name = oldData.ThemeName });
                        
                    // Delete old settings file after migration
                    File.Delete(oldPath);
                    _logger.LogInformation("Old settings migrated successfully.");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error migrating old settings");
        }
    }

    private class OldSettingsDto
    {
        public string? FfmpegPath { get; set; }
        public string? ThemeName { get; set; }
    }
}