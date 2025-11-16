namespace Converter.Application.Abstractions;

public interface IPresetRepository
{
    Task<IReadOnlyList<Converter.Models.ConversionProfile>> GetAllPresetsAsync();
    Task SavePresetAsync(Converter.Models.ConversionProfile preset);
    Task DeletePresetAsync(string presetName);
}
