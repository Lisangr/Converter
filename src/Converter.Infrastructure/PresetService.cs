using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Converter.Application.Abstractions;
using Converter.Application.Models;

namespace Converter.Infrastructure;

/// <summary>
/// Сервис работы с пресетами.
/// </summary>
public class PresetService : IPresetService
{
    private readonly IPresetRepository _presetRepository;

    public PresetService(IPresetRepository presetRepository)
    {
        _presetRepository = presetRepository ?? throw new ArgumentNullException(nameof(presetRepository));
    }
    
    public async Task<IReadOnlyList<ConversionProfile>> GetAllPresetsAsync()
    {
        return await _presetRepository.GetPresetsAsync();
    }

    public async Task SavePresetAsync(ConversionProfile preset)
    {
        await _presetRepository.SavePresetAsync(preset);
    }

    public async Task DeletePresetAsync(string presetName)
    {
        await _presetRepository.DeletePresetAsync(presetName);
    }
}

/// <summary>
/// Интерфейс сервиса работы с пресетами.
/// </summary>
public interface IPresetService
{
    Task<IReadOnlyList<ConversionProfile>> GetAllPresetsAsync();
    Task SavePresetAsync(ConversionProfile preset);
    Task DeletePresetAsync(string presetName);
}

/// <summary>
/// Методы-расширения для IPresetService для работы с файловыми пресетами (JSON).
/// Используется в WinForms-UI для сохранения/загрузки одиночного пресета.
/// </summary>
public static class PresetServiceFileExtensions
{
    public static void SavePresetToFile(this IPresetService presetService, PresetProfile preset, string filePath)
    {
        if (presetService is null) throw new ArgumentNullException(nameof(presetService));
        if (preset is null) throw new ArgumentNullException(nameof(preset));
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path is required", nameof(filePath));

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(preset, options);
        File.WriteAllText(filePath, json);
    }

    public static PresetProfile LoadPresetFromFile(this IPresetService presetService, string filePath)
    {
        if (presetService is null) throw new ArgumentNullException(nameof(presetService));
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path is required", nameof(filePath));

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Preset file not found: {filePath}", filePath);
        }

        var json = File.ReadAllText(filePath);
        var preset = JsonSerializer.Deserialize<PresetProfile>(json);

        if (preset == null)
        {
            throw new InvalidOperationException("Не удалось загрузить пресет: неверный формат файла");
        }

        return preset;
    }
}
