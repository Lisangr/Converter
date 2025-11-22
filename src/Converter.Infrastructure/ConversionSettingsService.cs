using System;
using System.Threading;
using System.Threading.Tasks;
using Converter.Application.Abstractions;

namespace Converter.Infrastructure;

/// <summary>
/// Реализация IConversionSettingsService на базе ISettingsStore.
/// Отвечает за загрузку и сохранение путей FFmpeg, папки вывода
/// и шаблона именования файлов.
/// </summary>
public sealed class ConversionSettingsService : IConversionSettingsService
{
    private readonly ISettingsStore _settingsStore;
    private readonly ConversionSettings _current = new();

    public ConversionSettingsService(ISettingsStore settingsStore)
    {
        _settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
    }

    public ConversionSettings Current => _current;

    public async Task LoadAsync(CancellationToken ct = default)
    {
        var app = await _settingsStore.GetAppSettingsAsync(ct).ConfigureAwait(false);
        var prefs = await _settingsStore.GetUserPreferencesAsync(ct).ConfigureAwait(false);

        _current.FfmpegPath = app.FfmpegPath ?? string.Empty;

        _current.OutputFolder =
            !string.IsNullOrWhiteSpace(prefs.LastUsedOutputFolder)
                ? prefs.LastUsedOutputFolder
                : (app.DefaultOutputFolder ?? string.Empty);

        var naming = await _settingsStore
            .GetSettingAsync("Conversion.NamingPattern", "{original}_converted", ct)
            .ConfigureAwait(false);
        _current.NamingPattern = string.IsNullOrWhiteSpace(naming)
            ? "{original}_converted"
            : naming;
    }

    public async Task SaveAsync(CancellationToken ct = default)
    {
        var app = await _settingsStore.GetAppSettingsAsync(ct).ConfigureAwait(false);
        var prefs = await _settingsStore.GetUserPreferencesAsync(ct).ConfigureAwait(false);

        app.FfmpegPath = string.IsNullOrWhiteSpace(_current.FfmpegPath)
            ? null
            : _current.FfmpegPath;

        prefs.LastUsedOutputFolder = string.IsNullOrWhiteSpace(_current.OutputFolder)
            ? null
            : _current.OutputFolder;

        await _settingsStore.SaveAppSettingsAsync(app, ct).ConfigureAwait(false);
        await _settingsStore.SaveUserPreferencesAsync(prefs, ct).ConfigureAwait(false);

        var naming = string.IsNullOrWhiteSpace(_current.NamingPattern)
            ? "{original}_converted"
            : _current.NamingPattern;

        await _settingsStore
            .SetSettingAsync("Conversion.NamingPattern", naming, ct)
            .ConfigureAwait(false);
    }
}
