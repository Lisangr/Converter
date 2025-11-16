/// <summary>
/// Провайдер для работы с пресетами конвертации.
/// Предоставляет:
/// - Управление коллекцией пресетов
/// - Загрузку и сохранение пресетов
/// - Работу с выбранным по умолчанию пресетом
/// - Абстракцию над конкретными настройками конвертации
/// </summary>
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Converter.Application.Abstractions;
using Converter.Models;
using Microsoft.Extensions.Logging;

namespace Converter.Application.Services
{
    public class ProfileProvider : IProfileProvider
    {
        private const string ProfilesDirectory = "Profiles";
        private const string DefaultProfileId = "default";
        
        private readonly ILogger<ProfileProvider> _logger;
        private readonly Dictionary<string, Converter.Models.ConversionProfile> _profiles = new();
        private string _defaultProfileId;

        public ProfileProvider(ILogger<ProfileProvider> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            InitializeDefaultProfiles();
            LoadProfilesFromDisk().GetAwaiter().GetResult();
        }

        public Task<IReadOnlyList<Converter.Models.ConversionProfile>> GetAllProfilesAsync()
        {
            return Task.FromResult<IReadOnlyList<Converter.Models.ConversionProfile>>(
                _profiles.Values.OrderBy(p => p.Name).ToList());
        }

        public Task<Converter.Models.ConversionProfile> GetProfileByIdAsync(string id)
        {
            if (string.IsNullOrEmpty(id)) 
                throw new ArgumentException("Profile ID cannot be null or empty", nameof(id));

            _profiles.TryGetValue(id, out var profile);
            return Task.FromResult(profile);
        }

        public Task<Converter.Models.ConversionProfile> GetDefaultProfileAsync()
        {
            if (!string.IsNullOrEmpty(_defaultProfileId) && _profiles.ContainsKey(_defaultProfileId))
            {
                return Task.FromResult(_profiles[_defaultProfileId]);
            }
            
            // Fall back to first available profile or create a default one
            var defaultProfile = _profiles.Values.FirstOrDefault() ?? CreateDefaultProfile();
            return Task.FromResult(defaultProfile);
        }

        public async Task SetDefaultProfileAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Profile ID cannot be null or empty", nameof(id));

            if (!_profiles.ContainsKey(id))
                throw new KeyNotFoundException($"Profile with ID '{id}' not found");

            _defaultProfileId = id;
            await SaveDefaultProfileIdAsync();
            _logger.LogInformation("Set default profile to {ProfileId}", id);
        }

        public async Task SaveProfileAsync(Converter.Models.ConversionProfile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (string.IsNullOrEmpty(profile.Id))
                profile.Id = Guid.NewGuid().ToString("N");

            _profiles[profile.Id] = profile;
            await SaveProfileToDiskAsync(profile);
            _logger.LogInformation("Saved profile {ProfileId}", profile.Id);
        }

        public async Task DeleteProfileAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Profile ID cannot be null or empty", nameof(id));

            if (_profiles.Remove(id, out _))
            {
                var filePath = GetProfileFilePath(id);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                
                _logger.LogInformation("Deleted profile {ProfileId}", id);
                
                // If we deleted the default profile, update it
                if (id == _defaultProfileId)
                {
                    var newDefault = _profiles.Values.FirstOrDefault();
                    if (newDefault != null)
                    {
                        await SetDefaultProfileAsync(newDefault.Id);
                    }
                }
            }
        }

        private void InitializeDefaultProfiles()
        {
            // Add some default profiles if none exist
            if (_profiles.Count == 0)
            {
                var defaultProfile = new Converter.Models.ConversionProfile
                {
                    Id = DefaultProfileId,
                    Name = "Default",
                    Category = "General",
                    VideoCodec = "libx264",
                    AudioCodec = "aac",
                    Bitrate = 4000,
                    AudioBitrate = 192,
                    Width = 1920,
                    Height = 1080,
                    Format = "mp4",
                    IsPro = false
                };

                _profiles[defaultProfile.Id] = defaultProfile;
                _defaultProfileId = defaultProfile.Id;
            }
        }

        private async Task LoadProfilesFromDisk()
        {
            try
            {
                if (!Directory.Exists(ProfilesDirectory))
                {
                    Directory.CreateDirectory(ProfilesDirectory);
                    return;
                }

                var profileFiles = Directory.GetFiles(ProfilesDirectory, "*.json");
                foreach (var file in profileFiles)
                {
                    try
                    {
                        var json = await File.ReadAllTextAsync(file);
                        var profile = JsonSerializer.Deserialize<Converter.Models.ConversionProfile>(json);
                        if (profile != null)
                        {
                            _profiles[profile.Id] = profile;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error loading profile from {FilePath}", file);
                    }
                }

                // Load default profile ID if not set
                if (string.IsNullOrEmpty(_defaultProfileId))
                {
                    var defaultProfilePath = Path.Combine(ProfilesDirectory, "default.profile");
                    if (File.Exists(defaultProfilePath))
                    {
                        _defaultProfileId = await File.ReadAllTextAsync(defaultProfilePath);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading profiles from disk");
            }
        }

        private async Task SaveProfileToDiskAsync(Converter.Models.ConversionProfile profile)
        {
            try
            {
                if (!Directory.Exists(ProfilesDirectory))
                {
                    Directory.CreateDirectory(ProfilesDirectory);
                }

                var filePath = GetProfileFilePath(profile.Id);
                var json = JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving profile {ProfileId} to disk", profile.Id);
                throw;
            }
        }

        private async Task SaveDefaultProfileIdAsync()
        {
            try
            {
                if (!Directory.Exists(ProfilesDirectory))
                {
                    Directory.CreateDirectory(ProfilesDirectory);
                }

                var defaultProfilePath = Path.Combine(ProfilesDirectory, "default.profile");
                await File.WriteAllTextAsync(defaultProfilePath, _defaultProfileId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving default profile ID");
            }
        }

        private static string GetProfileFilePath(string profileId)
        {
            return Path.Combine(ProfilesDirectory, $"{profileId}.json");
        }

        private static Converter.Models.ConversionProfile CreateDefaultProfile()
        {
            return new Converter.Models.ConversionProfile
            {
                Id = DefaultProfileId,
                Name = "Default",
                Category = "General",
                VideoCodec = "libx264",
                AudioCodec = "aac",
                Bitrate = 4000,
                AudioBitrate = 192,
                Width = 1920,
                Height = 1080,
                Format = "mp4",
                IsPro = false
            };
        }
    }
}
