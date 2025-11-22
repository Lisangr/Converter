using Converter.Application.Abstractions;
using Converter.Application.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Converter.Services;

namespace Converter.Infrastructure.Persistence;

/// <summary>
/// Фасад для обратной совместимости с тестами.
/// Предоставляет старые названия методов через новые интерфейсы.
/// </summary>
public class JsonPresetRepository : IPresetRepository
{
    private readonly IPresetRepository _innerRepository;

    public JsonPresetRepository(string testFilePath, Microsoft.Extensions.Logging.ILogger<JsonPresetRepository> logger)
    {
        // В реальной реализации здесь будет логика инициализации
        _innerRepository = new JsonPresetRepositoryImpl();
    }

    public async Task<IReadOnlyList<ConversionProfile>> GetPresetsAsync(CancellationToken ct = default)
    {
        return await _innerRepository.GetPresetsAsync(ct);
    }

    public async Task<ConversionProfile?> GetPresetAsync(string id, CancellationToken ct = default)
    {
        return await _innerRepository.GetPresetAsync(id, ct);
    }

    public async Task SavePresetAsync(ConversionProfile preset, CancellationToken ct = default)
    {
        await _innerRepository.SavePresetAsync(preset, ct);
    }

    public async Task DeletePresetAsync(string id, CancellationToken ct = default)
    {
        await _innerRepository.DeletePresetAsync(id, ct);
    }

    // Приватная реализация для заглушки
    private class JsonPresetRepositoryImpl : IPresetRepository
    {
        public Task<IReadOnlyList<ConversionProfile>> GetPresetsAsync(CancellationToken ct = default)
        {
            // Используем существующий XmlPresetLoader для загрузки пресетов из XML-файлов
            var loader = new XmlPresetLoader();
            var presets = loader.LoadAllPresets();

            // Маппим PresetProfile в ConversionProfile (наследник) с копированием всех основных свойств
            var result = presets
                .Select(p => new ConversionProfile
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Category = p.Category,
                    VideoCodec = p.VideoCodec,
                    Bitrate = p.Bitrate,
                    Width = p.Width,
                    Height = p.Height,
                    CRF = p.CRF,
                    Format = p.Format,
                    AudioCodec = p.AudioCodec,
                    AudioBitrate = p.AudioBitrate,
                    IncludeAudio = p.IncludeAudio,
                    MaxFileSizeMB = p.MaxFileSizeMB,
                    MaxDurationSeconds = p.MaxDurationSeconds,
                    Icon = p.Icon,
                    ColorHex = p.ColorHex,
                    IsPro = p.IsPro
                })
                .ToList();

            return Task.FromResult<IReadOnlyList<ConversionProfile>>(result);
        }

        public Task<ConversionProfile?> GetPresetAsync(string id, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return Task.FromResult<ConversionProfile?>(null);
            }

            var loader = new XmlPresetLoader();
            var presets = loader.LoadAllPresets();

            var match = presets.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));
            if (match == null)
            {
                return Task.FromResult<ConversionProfile?>(null);
            }

            var result = new ConversionProfile
            {
                Id = match.Id,
                Name = match.Name,
                Description = match.Description,
                Category = match.Category,
                VideoCodec = match.VideoCodec,
                Bitrate = match.Bitrate,
                Width = match.Width,
                Height = match.Height,
                CRF = match.CRF,
                Format = match.Format,
                AudioCodec = match.AudioCodec,
                AudioBitrate = match.AudioBitrate,
                IncludeAudio = match.IncludeAudio,
                MaxFileSizeMB = match.MaxFileSizeMB,
                MaxDurationSeconds = match.MaxDurationSeconds,
                Icon = match.Icon,
                ColorHex = match.ColorHex,
                IsPro = match.IsPro
            };

            return Task.FromResult<ConversionProfile?>(result);
        }

        public Task SavePresetAsync(ConversionProfile preset, CancellationToken ct = default)
        {
            // Текущая реализация UI использует отдельные методы сохранения пресета в файл,
            // поэтому здесь оставляем заглушку, чтобы не ломать тесты и существующее поведение.
            return Task.CompletedTask;
        }

        public Task DeletePresetAsync(string id, CancellationToken ct = default)
        {
            // Удаление пресетов пока не реализовано для XML-хранилища.
            return Task.CompletedTask;
        }
    }
}