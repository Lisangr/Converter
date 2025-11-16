using System.Threading;
using System.Threading.Tasks;
using Converter.Domain.Models;

namespace Converter.Application.Abstractions;

// 1. Define a comprehensive settings interface
public interface ISettingsStore
{
    // User settings
    Task<T?> GetSettingAsync<T>(string key, T? defaultValue = default, CancellationToken ct = default);
    Task SetSettingAsync<T>(string key, T value, CancellationToken ct = default);
    
    // Application settings
    Task<AppSettings> GetAppSettingsAsync(CancellationToken ct = default);
    Task SaveAppSettingsAsync(AppSettings settings, CancellationToken ct = default);
    
    // User preferences
    Task<UserPreferences> GetUserPreferencesAsync(CancellationToken ct = default);
    Task SaveUserPreferencesAsync(UserPreferences preferences, CancellationToken ct = default);
    
    // Secure storage for sensitive data
    Task<string?> GetSecureValueAsync(string key, CancellationToken ct = default);
    Task SetSecureValueAsync(string key, string value, CancellationToken ct = default);
}