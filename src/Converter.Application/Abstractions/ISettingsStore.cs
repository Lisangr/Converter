using System.Threading;
using System.Threading.Tasks;
using Converter.Domain.Models;

namespace Converter.Application.Abstractions;

/// <summary>
/// Универсальное хранилище настроек приложения.
/// Обеспечивает типобезопасное управление различными типами настроек,
/// включая пользовательские настройки, настройки приложения и безопасное хранение.
/// Поддерживает асинхронные операции и множественные типы данных.
/// </summary>
public interface ISettingsStore
{
    // ===== ПОЛЬЗОВАТЕЛЬСКИЕ НАСТРОЙКИ =====
    
    /// <summary>
    /// Получает пользовательскую настройку указанного типа.
    /// Возвращает значение по умолчанию, если настройка не найдена.
    /// </summary>
    /// <typeparam name="T">Тип возвращаемого значения</typeparam>
    /// <param name="key">Ключ настройки</param>
    /// <param name="defaultValue">Значение по умолчанию</param>
    /// <param name="ct">Токен отмены операции</param>
    /// <returns>Значение настройки или значение по умолчанию</returns>
    Task<T?> GetSettingAsync<T>(string key, T? defaultValue = default, CancellationToken ct = default);
    
    /// <summary>
    /// Сохраняет пользовательскую настройку указанного типа.
    /// Автоматически сериализует данные в подходящий формат.
    /// </summary>
    /// <typeparam name="T">Тип сохраняемого значения</typeparam>
    /// <param name="key">Ключ настройки</param>
    /// <param name="value">Значение для сохранения</param>
    /// <param name="ct">Токен отмены операции</param>
    Task SetSettingAsync<T>(string key, T value, CancellationToken ct = default);
    
    // ===== НАСТРОЙКИ ПРИЛОЖЕНИЯ =====
    
    /// <summary>
    /// Получает настройки приложения (конфигурация).
    /// Включает системные настройки, пути к программам и другие конфигурационные параметры.
    /// </summary>
    /// <param name="ct">Токен отмены операции</param>
    /// <returns>Объект настроек приложения</returns>
    Task<AppSettings> GetAppSettingsAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Сохраняет настройки приложения.
    /// Обновляет конфигурационные параметры системы.
    /// </summary>
    /// <param name="settings">Настройки для сохранения</param>
    /// <param name="ct">Токен отмены операции</param>
    Task SaveAppSettingsAsync(AppSettings settings, CancellationToken ct = default);
    
    // ===== ПРЕДПОЧТЕНИЯ ПОЛЬЗОВАТЕЛЯ =====
    
    /// <summary>
    /// Получает пользовательские предпочтения.
    /// Включает UI настройки, темы, языки и другие пользовательские конфигурации.
    /// </summary>
    /// <param name="ct">Токен отмены операции</param>
    /// <returns>Объект предпочтений пользователя</returns>
    Task<UserPreferences> GetUserPreferencesAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Сохраняет пользовательские предпочтения.
    /// Обновляет персонализированные настройки интерфейса.
    /// </summary>
    /// <param name="preferences">Предпочтения для сохранения</param>
    /// <param name="ct">Токен отмены операции</param>
    Task SaveUserPreferencesAsync(UserPreferences preferences, CancellationToken ct = default);
    
    // ===== БЕЗОПАСНОЕ ХРАНИЛИЩЕ =====
    
    /// <summary>
    /// Получает защищенное значение из безопасного хранилища.
    /// Используется для хранения чувствительных данных (пароли, ключи API).
    /// </summary>
    /// <param name="key">Ключ защищенного значения</param>
    /// <param name="ct">Токен отмены операции</param>
    /// <returns>Защищенное значение или null</returns>
    Task<string?> GetSecureValueAsync(string key, CancellationToken ct = default);
    
    /// <summary>
    /// Сохраняет значение в безопасном хранилище.
    /// Автоматически шифрует данные перед сохранением.
    /// </summary>
    /// <param name="key">Ключ для сохранения</param>
    /// <param name="value">Значение для сохранения</param>
    /// <param name="ct">Токен отмены операции</param>
    Task SetSecureValueAsync(string key, string value, CancellationToken ct = default);
}