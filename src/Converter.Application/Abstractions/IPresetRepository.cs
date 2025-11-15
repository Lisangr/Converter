namespace Converter.Application.Abstractions;

public interface IPresetRepository
{
    Task<IReadOnlyList<ConversionProfile>> GetAllPresetsAsync();
    Task SavePresetAsync(ConversionProfile preset);
    Task DeletePresetAsync(string presetName);
}
