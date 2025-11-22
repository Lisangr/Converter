using System.Threading;
using System.Threading.Tasks;

namespace Converter.Application.Abstractions;

/// <summary>
/// Application-уровневый сервис настроек окружения конвертации.
/// Инкапсулирует пути FFmpeg, папку вывода и шаблон именования
/// без знания о конкретной инфраструктуре.
/// </summary>
public interface IConversionSettingsService
{
    /// <summary>Текущие настройки конвертации.</summary>
    ConversionSettings Current { get; }

    /// <summary>Загружает настройки из хранилища.</summary>
    Task LoadAsync(CancellationToken ct = default);

    /// <summary>Сохраняет текущие настройки в хранилище.</summary>
    Task SaveAsync(CancellationToken ct = default);
}

/// <summary>
/// DTO для настроек конвертации, используемых UI и презентером.
/// </summary>
public sealed class ConversionSettings
{
    public string? FfmpegPath { get; set; }
    public string? OutputFolder { get; set; }
    public string? NamingPattern { get; set; }
}
