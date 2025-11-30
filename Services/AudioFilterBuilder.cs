using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Converter.Domain.Models;

namespace Converter.Services
{
    public static class AudioFilterBuilder
    {
        public static string? BuildAudioFilters(AudioProcessingOptions options, double totalDuration)
        {
            if (options == null || !options.HasAudioEffects)
            {
                return null;
            }

            var filters = new List<string>();

            // 1. Noise Reduction
            if (options.NoiseReduction && options.NoiseReductionStrength != NoiseReductionStrength.None)
            {
                string nrFilter = "";
                switch (options.NoiseReductionStrength)
                {
                    case NoiseReductionStrength.Light:
                        nrFilter = "afftdn=nr=10:nf=-25"; // Example values for light NR
                        break;
                    case NoiseReductionStrength.Medium:
                        nrFilter = "afftdn=nr=20:nf=-30"; // Example values for medium NR
                        break;
                    case NoiseReductionStrength.Strong:
                        nrFilter = "afftdn=nr=50:nf=-40"; // Example values for strong NR
                        break;
                }
                if (!string.IsNullOrEmpty(nrFilter))
                {
                    filters.Add(nrFilter);
                }
            }

            // 2. Equalizer
            if (options.UseEqualizer)
            {
                string eqFilter = "";
                switch (options.EqualizerPreset)
                {
                    case EqualizerPreset.None:
                        break; // No EQ
                    case EqualizerPreset.Custom:
                        if (options.CustomEQBands != null && options.CustomEQBands.Any())
                        {
                            // Build custom EQ string: equalizer=f=frequency:width_type=o:width=bandwidth:g=gain
                            // Example: equalizer=f=100:width_type=o:width=2:g=-3,equalizer=f=1000:width_type=o:width=2:g=2
                            var eqParts = new List<string>();
                            foreach (var band in options.CustomEQBands.OrderBy(b => b.Key))
                            {
                                // Gain is usually in dB. FFmpeg uses it directly. Width type 'o' for octaves.
                                // Let's assume a default width and try to map gain reasonably.
                                // FFmpeg EQ gain is typically specified as 'g', default width is 0.707 (Q=1.414)
                                // A Q value of 2 means bandwidth is 1/2 octave.
                                eqParts.Add($"equalizer=f={band.Key}:width_type=o:width=2:g={band.Value}");
                            }
                            eqFilter = string.Join(",", eqParts);
                        }
                        break;
                    case EqualizerPreset.Bass:
                        // Example: Boost low frequencies, e.g., 100Hz and 200Hz
                        eqFilter = "equalizer=f=100:width_type=o:width=2:g=4,equalizer=f=200:width_type=o:width=2:g=3";
                        break;
                    case EqualizerPreset.Vocal:
                        // Example: Boost mid-frequencies around vocal range, e.g., 1kHz, 3kHz
                        eqFilter = "equalizer=f=1000:width_type=o:width=1.5:g=2,equalizer=f=3000:width_type=o:width=1.5:g=3";
                        break;
                    case EqualizerPreset.Treble:
                        // Example: Boost high frequencies, e.g., 5kHz, 10kHz
                        eqFilter = "equalizer=f=5000:width_type=o:width=2:g=3,equalizer=f=10000:width_type=o:width=2:g=4";
                        break;
                    case EqualizerPreset.Rock:
                        // Example: Balanced boost for rock music
                        eqFilter = "equalizer=f=80:width_type=o:width=2:g=2,equalizer=f=1000:width_type=o:width=1.5:g=1,equalizer=f=8000:width_type=o:width=2:g=3";
                        break;
                    case EqualizerPreset.Pop:
                        // Example: Boosted highs and lows for pop music
                        eqFilter = "equalizer=f=120:width_type=o:width=2:g=3,equalizer=f=2500:width_type=o:width=1.5:g=1,equalizer=f=12000:width_type=o:width=2:g=4";
                        break;
                }
                if (!string.IsNullOrEmpty(eqFilter))
                {
                    filters.Add(eqFilter);
                }
            }

            // 3. Normalization
            if (options.NormalizeVolume && options.NormalizationMode != VolumeNormalizationMode.None)
            {
                string normFilter = "";
                switch (options.NormalizationMode)
                {
                    case VolumeNormalizationMode.Peak:
                        // Peak-style loudness normalization
                        normFilter = "loudnorm=I=-16:TP=-1.5:LRA=11:print_format=summary";
                        break;
                    case VolumeNormalizationMode.RMS:
                        // RMS-style target (e.g. YouTube-like)
                        normFilter = "loudnorm=I=-23:TP=-2:LRA=7:print_format=summary";
                        break;
                    case VolumeNormalizationMode.Spotify:
                        // Spotify reference loudness
                        normFilter = "loudnorm=I=-14:TP=-1:LRA=11:print_format=summary";
                        break;
                    case VolumeNormalizationMode.Custom:
                        // Custom mode falls back to a generic loudnorm; detailed tuning handled elsewhere
                        normFilter = "loudnorm=I=-16:TP=-1.5:LRA=11:print_format=summary";
                        break;
                }
                if (!string.IsNullOrEmpty(normFilter))
                {
                    filters.Add(normFilter);
                }
            }

            // 4. Fading
            // Fading requires knowing the total duration of the video.
            // Fade in: afade=t=in:ss=0:d=duration
            // Fade out: afade=t=out:st=start_time:d=duration
            // Start time for fade out is totalDuration - fadeOutDuration

            if (options.FadeInDuration > 0)
            {
                filters.Add($"afade=t=in:ss=0:d={options.FadeInDuration}");
            }
            if (options.FadeOutDuration > 0 && totalDuration > 0)
            {
                double startTime = Math.Max(0, totalDuration - options.FadeOutDuration);
                filters.Add($"afade=t=out:st={startTime}:d={options.FadeOutDuration}");
            }

            return filters.Any() ? string.Join(",", filters) : null;
        }
    }
}
