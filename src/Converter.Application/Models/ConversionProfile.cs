namespace Converter.Application.Models;

/// <summary>
/// Профиль конвертации видео с полным набором настроек.
/// Наследует от PresetProfile и добавляет специфичные для конвертации свойства.
/// </summary>
public class ConversionProfile : PresetProfile
{
    // Дополнительные свойства конвертации, если нужны
    // В основном наследует от PresetProfile

    public ConversionProfile()
    {
    }

    public ConversionProfile(
        string name,
        string videoCodec,
        string audioCodec,
        string? audioBitrateK,
        int crf)
    {
        Name = name;
        VideoCodec = videoCodec;
        AudioCodec = audioCodec;
        AudioBitrate = ParseAudioBitrateK(audioBitrateK);
        CRF = crf;
    }

    private static int? ParseAudioBitrateK(string? audioBitrateK)
    {
        if (string.IsNullOrWhiteSpace(audioBitrateK)) return null;
        var trimmed = audioBitrateK.Trim();
        if (trimmed.EndsWith("k", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[..^1];
        }

        return int.TryParse(trimmed, out var kbps) ? kbps : null;
    }
}