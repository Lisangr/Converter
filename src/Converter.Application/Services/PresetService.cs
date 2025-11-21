using Converter.Application.Abstractions;
using Converter.Application.Models;

namespace Converter.Application.Services;

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