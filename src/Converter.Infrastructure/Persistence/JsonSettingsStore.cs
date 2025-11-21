using Converter.Application.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Converter.Infrastructure.Persistence;

/// <summary>
/// Алиас для обратной совместимости с тестами.
/// </summary>
public class JsonSettingsStore : ISettingsStore
{
    private readonly ISettingsStore _innerStore;

    // Пустой конструктор для использования в приложении через DI контейнер.
    // В реальном приложении здесь можно будет инициализировать хранилище
    // с путём к файлу настроек из конфигурации.
    public JsonSettingsStore()
    {
        _innerStore = new JsonSettingsStoreImpl();
    }

    public JsonSettingsStore(string testFilePath, Microsoft.Extensions.Logging.ILogger<JsonSettingsStore> logger)
    {
        _innerStore = new JsonSettingsStoreImpl();
    }

    public JsonSettingsStore(string testFilePath, Microsoft.Extensions.Logging.ILogger<JsonSettingsStore> logger, System.Text.Json.JsonSerializerOptions options)
    {
        _innerStore = new JsonSettingsStoreImpl();
    }

    public Task<T?> GetSettingAsync<T>(string key, T? defaultValue = default, CancellationToken ct = default)
    {
        return _innerStore.GetSettingAsync(key, defaultValue, ct);
    }

    public Task SetSettingAsync<T>(string key, T value, CancellationToken ct = default)
    {
        return _innerStore.SetSettingAsync(key, value, ct);
    }

    public Task<Domain.Models.AppSettings> GetAppSettingsAsync(CancellationToken ct = default)
    {
        return _innerStore.GetAppSettingsAsync(ct);
    }

    public Task SaveAppSettingsAsync(Domain.Models.AppSettings settings, CancellationToken ct = default)
    {
        return _innerStore.SaveAppSettingsAsync(settings, ct);
    }

    public Task<Domain.Models.UserPreferences> GetUserPreferencesAsync(CancellationToken ct = default)
    {
        return _innerStore.GetUserPreferencesAsync(ct);
    }

    public Task SaveUserPreferencesAsync(Domain.Models.UserPreferences preferences, CancellationToken ct = default)
    {
        return _innerStore.SaveUserPreferencesAsync(preferences, ct);
    }

    public Task<string?> GetSecureValueAsync(string key, CancellationToken ct = default)
    {
        return _innerStore.GetSecureValueAsync(key, ct);
    }

    public Task SetSecureValueAsync(string key, string value, CancellationToken ct = default)
    {
        return _innerStore.SetSecureValueAsync(key, value, ct);
    }

    // Приватная реализация-заглушка
    private class JsonSettingsStoreImpl : ISettingsStore
    {
        public Task<T?> GetSettingAsync<T>(string key, T? defaultValue = default, CancellationToken ct = default)
        {
            return Task.FromResult(defaultValue);
        }

        public Task SetSettingAsync<T>(string key, T value, CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }

        public Task<Domain.Models.AppSettings> GetAppSettingsAsync(CancellationToken ct = default)
        {
            return Task.FromResult(new Domain.Models.AppSettings());
        }

        public Task SaveAppSettingsAsync(Domain.Models.AppSettings settings, CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }

        public Task<Domain.Models.UserPreferences> GetUserPreferencesAsync(CancellationToken ct = default)
        {
            return Task.FromResult(new Domain.Models.UserPreferences());
        }

        public Task SaveUserPreferencesAsync(Domain.Models.UserPreferences preferences, CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }

        public Task<string?> GetSecureValueAsync(string key, CancellationToken ct = default)
        {
            return Task.FromResult<string?>(null);
        }

        public Task SetSecureValueAsync(string key, string value, CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }
    }
}