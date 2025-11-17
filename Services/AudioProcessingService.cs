using System;
using System.Collections.Generic;
using System.Linq;
using Converter.Domain.Models;
using Xabe.FFmpeg;

namespace Converter.Services
{
    public class AudioProcessingService
    {
        /// <summary>
        /// Собирает строку FFmpeg фильтров на основе выбранных опций обработки аудио
        /// </summary>
        public static string? BuildAudioFilterString(AudioProcessingOptions? options)
        {
            if (options == null)
            {
                return null;
            }

            var filters = new List<string>();

            if (options.NormalizeVolume)
            {
                filters.Add(GetVolumeNormalizationFilter(options.NormalizationMode));
            }

            if (options.NoiseReduction)
            {
                filters.Add(GetNoiseReductionFilter(options.NoiseReductionStrength));
            }

            if (options.UseEqualizer && options.EqualizerPreset != EqualizerPreset.None)
            {
                var eqFilter = GetEqualizerFilter(options.EqualizerPreset, options.CustomEQBands);
                if (!string.IsNullOrWhiteSpace(eqFilter))
                {
                    filters.Add(eqFilter);
                }
            }

            if (options.FadeInDuration > 0)
            {
                filters.Add($"afade=t=in:d={options.FadeInDuration}");
            }

            if (options.FadeOutDuration > 0 && options.TotalDuration > 0)
            {
                var startTime = Math.Max(0, options.TotalDuration - options.FadeOutDuration);
                filters.Add($"afade=t=out:st={startTime}:d={options.FadeOutDuration}");
            }

            return filters.Count > 0 ? string.Join(",", filters) : null;
        }

        private static string GetVolumeNormalizationFilter(VolumeNormalizationMode mode)
        {
            return mode switch
            {
                VolumeNormalizationMode.Peak => "loudnorm=I=-16:TP=-1.5:LRA=11",
                VolumeNormalizationMode.RMS => "loudnorm=I=-23:TP=-2:LRA=7",
                VolumeNormalizationMode.Spotify => "loudnorm=I=-14:TP=-1:LRA=11",
                VolumeNormalizationMode.Custom => "loudnorm=I=-16:TP=-1.5:LRA=11:measured_I=-20:measured_LRA=1:measured_TP=-2:measured_thresh=-30",
                _ => "loudnorm"
            };
        }

        private static string GetNoiseReductionFilter(NoiseReductionStrength strength)
        {
            return strength switch
            {
                NoiseReductionStrength.Light => "afftdn=nr=10:nf=-20",
                NoiseReductionStrength.Medium => "afftdn=nr=20:nf=-25",
                NoiseReductionStrength.Strong => "afftdn=nr=30:nf=-30",
                NoiseReductionStrength.VeryStrong => "afftdn=nr=50:nf=-35",
                _ => "afftdn=nr=12:nf=-25"
            };
        }

        private static string? GetEqualizerFilter(EqualizerPreset preset, Dictionary<int, double>? customBands)
        {
            if (preset == EqualizerPreset.Custom && customBands != null && customBands.Count > 0)
            {
                return BuildCustomEqualizer(customBands);
            }

            return preset switch
            {
                EqualizerPreset.Bass => "equalizer=f=100:t=q:w=1:g=8,equalizer=f=200:t=q:w=1:g=5",
                EqualizerPreset.Treble => "equalizer=f=5000:t=q:w=1:g=8,equalizer=f=10000:t=q:w=1:g=6",
                EqualizerPreset.Pop => "equalizer=f=100:t=q:w=1:g=-1,equalizer=f=1000:t=q:w=1:g=2,equalizer=f=3000:t=q:w=1:g=4",
                EqualizerPreset.Rock => "equalizer=f=100:t=q:w=1:g=5,equalizer=f=500:t=q:w=1:g=2,equalizer=f=3000:t=q:w=1:g=3",
                EqualizerPreset.Classical => "equalizer=f=100:t=q:w=1:g=3,equalizer=f=250:t=q:w=1:g=2,equalizer=f=8000:t=q:w=1:g=4",
                EqualizerPreset.Jazz => "equalizer=f=100:t=q:w=1:g=4,equalizer=f=500:t=q:w=1:g=-2,equalizer=f=5000:t=q:w=1:g=3",
                EqualizerPreset.Vocal => "equalizer=f=500:t=q:w=1:g=-2,equalizer=f=1000:t=q:w=1:g=3,equalizer=f=3000:t=q:w=1:g=5",
                EqualizerPreset.Cinema => "equalizer=f=100:t=q:w=1:g=6,equalizer=f=8000:t=q:w=1:g=-2",
                _ => null
            };
        }

        private static string BuildCustomEqualizer(Dictionary<int, double> bands)
        {
            return string.Join(",", bands.Select(band => $"equalizer=f={band.Key}:t=q:w=1:g={band.Value}"));
        }

        public static IConversion ApplyAudioProcessing(IConversion conversion, AudioProcessingOptions? options, string? filterString = null)
        {
            if (options == null)
            {
                return conversion;
            }

            var filters = filterString ?? BuildAudioFilterString(options);
            if (!string.IsNullOrWhiteSpace(filters))
            {
                conversion.AddParameter($"-af \"{filters}\"");
            }

            return conversion;
        }
    }
}
