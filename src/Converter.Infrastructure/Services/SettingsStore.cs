using System.Text.Json;
using Converter.Application.Interfaces;
using Converter.Domain.Models;
using Microsoft.Extensions.Logging;

namespace Converter.Infrastructure.Services;

public sealed class SettingsStore : ISettingsStore
{
    private readonly ILogger<SettingsStore> _logger;
    private readonly string _settingsPath;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    private sealed record SettingsPayload(string? LastOutputDirectory, List<ConversionProfile> Profiles);

    public SettingsStore(ILogger<SettingsStore> logger)
    {
        _logger = logger;
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var folder = Path.Combine(appData, "Converter", "Settings");
        Directory.CreateDirectory(folder);
        _settingsPath = Path.Combine(folder, "settings.json");
    }

    public async Task<string?> GetLastOutputDirectoryAsync(CancellationToken cancellationToken)
    {
        var payload = await ReadAsync(cancellationToken).ConfigureAwait(false);
        return payload.LastOutputDirectory;
    }

    public async Task SetLastOutputDirectoryAsync(string path, CancellationToken cancellationToken)
    {
        var payload = await ReadAsync(cancellationToken).ConfigureAwait(false);
        var updated = payload with { LastOutputDirectory = path };
        await WriteAsync(updated, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ConversionProfile>> GetProfilesAsync(CancellationToken cancellationToken)
    {
        var payload = await ReadAsync(cancellationToken).ConfigureAwait(false);
        return payload.Profiles;
    }

    public async Task SaveProfileAsync(ConversionProfile profile, CancellationToken cancellationToken)
    {
        var payload = await ReadAsync(cancellationToken).ConfigureAwait(false);
        payload.Profiles.RemoveAll(p => p.Name == profile.Name);
        payload.Profiles.Add(profile);
        await WriteAsync(payload, cancellationToken).ConfigureAwait(false);
    }

    private async Task<SettingsPayload> ReadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_settingsPath))
        {
            return new SettingsPayload(null, new List<ConversionProfile>());
        }

        await using var stream = File.OpenRead(_settingsPath);
        var payload = await JsonSerializer.DeserializeAsync<SettingsPayload>(stream, _jsonOptions, cancellationToken).ConfigureAwait(false);
        return payload ?? new SettingsPayload(null, new List<ConversionProfile>());
    }

    private async Task WriteAsync(SettingsPayload payload, CancellationToken cancellationToken)
    {
        await using var stream = File.Create(_settingsPath);
        await JsonSerializer.SerializeAsync(stream, payload, _jsonOptions, cancellationToken).ConfigureAwait(false);
    }
}
