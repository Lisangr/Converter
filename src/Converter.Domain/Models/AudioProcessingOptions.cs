using System.Collections.Generic;

namespace Converter.Domain.Models
{
    public enum VolumeNormalizationMode
    {
        None,
        Peak,
        RMS,
        Spotify,
        Custom
    }

    public enum NoiseReductionStrength
    {
        None,
        Light,
        Medium,
        Strong,
        VeryStrong
    }

    public enum EqualizerPreset
    {
        None,
        Custom,
        Bass,
        Treble,
        Pop,
        Rock,
        Classical,
        Jazz,
        Vocal,
        Cinema
    }

    public sealed class AudioProcessingOptions
    {
        public bool NormalizeVolume { get; set; }
        public VolumeNormalizationMode NormalizationMode { get; set; } = VolumeNormalizationMode.Peak;
        public bool NoiseReduction { get; set; }
        public NoiseReductionStrength NoiseReductionStrength { get; set; } = NoiseReductionStrength.Medium;
        public bool UseEqualizer { get; set; }
        public EqualizerPreset EqualizerPreset { get; set; } = EqualizerPreset.None;
        public Dictionary<int, double> CustomEQBands { get; set; } = new();
        public double FadeInDuration { get; set; }
        public double FadeOutDuration { get; set; }
        public double TotalDuration { get; set; }

        public bool HasAudioEffects => NormalizeVolume || NoiseReduction || FadeInDuration > 0 || FadeOutDuration > 0 || UseEqualizer;

        public AudioProcessingOptions Clone()
        {
            return new AudioProcessingOptions
            {
                NormalizeVolume = NormalizeVolume,
                NormalizationMode = NormalizationMode,
                NoiseReduction = NoiseReduction,
                NoiseReductionStrength = NoiseReductionStrength,
                UseEqualizer = UseEqualizer,
                EqualizerPreset = EqualizerPreset,
                CustomEQBands = new Dictionary<int, double>(CustomEQBands),
                FadeInDuration = FadeInDuration,
                FadeOutDuration = FadeOutDuration,
                TotalDuration = TotalDuration
            };
        }
    }
}
