using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Converter.Application.Models;
using Xabe.FFmpeg;

namespace Converter.Services
{
    public class EstimationService
    {
        public class VideoInfo
        {
            public int Width { get; set; }
            public int Height { get; set; }
            public TimeSpan Duration { get; set; }
            public long SizeBytes { get; set; }
            public double Fps { get; set; }
            public int SourceAudioBitrateKbps { get; set; }
        }

        private readonly ConcurrentDictionary<string, VideoInfo> _cache = new();

        // Encoding speed factors (relative to realtime)
        private readonly Dictionary<string, double> _codecSpeedFactors = new()
        {
            { "libx264", 1.0 },
            { "libx265", 0.3 },
            { "libvpx-vp9", 0.2 },
            { "libsvtav1", 0.4 },
            { "copy", 50.0 }
        };

        public async Task<ConversionEstimate> EstimateConversion(
            string inputFilePath,
            int targetBitrateKbps,
            int? targetWidth,
            int? targetHeight,
            string videoCodec,
            bool includeAudio,
            int? audioBitrateKbps,
            int? crf = null,
            bool audioCopy = false,
            CancellationToken ct = default)
        {
            var info = await AnalyzeVideo(inputFilePath, ct);
            var durationSec = info.Duration.TotalSeconds;
            if (durationSec <= 0) durationSec = 1;

            // current total bitrate (kbps)
            double currentTotalKbps = (info.SizeBytes * 8.0) / durationSec / 1000.0;
            // estimate current video bitrate by subtracting audio if available
            double currentVideoKbps = Math.Max(50, currentTotalKbps - info.SourceAudioBitrateKbps);

            // output size
            var audioKbps = includeAudio ? (audioCopy ? info.SourceAudioBitrateKbps : (audioBitrateKbps ?? info.SourceAudioBitrateKbps)) : 0;

            // Derive target video bitrate using content-aware model when targetBitrateKbps not explicitly set (<=0)
            int videoKbps = targetBitrateKbps;
            if (videoKbps <= 0)
            {
                // bits-per-pixel-per-frame model from source, adjusted by CRF and codec efficiency
                double fps = info.Fps > 0 ? info.Fps : 30.0;
                double srcPixels = Math.Max(1, info.Width * info.Height);
                double bppf_in = (currentVideoKbps * 1000.0) / (fps * srcPixels); // bits per pixel per frame
                // clamp to reasonable range (bits/pixel/frame)
                bppf_in = Math.Max(0.008, Math.Min(0.25, bppf_in));

                // CRF adjustment: every ~6 CRF doubles/halves bitrate
                double crfAdj = 1.0;
                if (crf.HasValue)
                {
                    int targetCrf = crf.Value;
                    int refCrf = 23;
                    crfAdj = Math.Pow(2.0, (refCrf - targetCrf) / 6.0);
                }

                // codec efficiency
                double eff = videoCodec.StartsWith("libx265", StringComparison.OrdinalIgnoreCase) ? 0.6
                    : videoCodec.StartsWith("libvpx-vp9", StringComparison.OrdinalIgnoreCase) ? 0.7
                    : videoCodec.StartsWith("libsvtav1", StringComparison.OrdinalIgnoreCase) ? 0.5
                    : 1.0;

                int tgtW = targetWidth ?? info.Width;
                int tgtH = targetHeight ?? info.Height;
                double tgtPixels = Math.Max(1, tgtW * tgtH);

                // sublinear pixel scaling to reflect additional savings when strongly downscaling
                double r = tgtPixels / srcPixels; // 0..1 for downscale
                double alpha = 0.85; // sublinear
                double scaledPixels = srcPixels * Math.Pow(Math.Max(1e-6, r), alpha);

                double bppf_target = Math.Max(0.003, Math.Min(0.20, bppf_in * crfAdj * eff));

                // Low-complexity content correction: if source bppf is very low, reduce target bppf further
                if (bppf_in < 0.05) bppf_target *= 0.75;   // quite simple scene
                if (bppf_in < 0.03) bppf_target *= 0.70;   // very simple scene
                if (bppf_in < 0.02) bppf_target *= 0.65;   // extremely simple (screen/low detail)

                // Dynamic cap for high CRF and strong downscale to avoid 2x+ overestimation
                double bppfCap = 0.02;
                if (crf.HasValue && crf.Value >= 28)
                {
                    bppfCap = (r <= 0.6) ? 0.006 : 0.010;
                }
                else if (crf.HasValue && crf.Value >= 23)
                {
                    bppfCap = 0.015;
                }
                bppf_target = Math.Min(bppf_target, bppfCap);

                // High-fps downscale dampening: typical encoders reach lower bppf at 50/60 fps when strongly downscaled
                if (info.Fps >= 50 && r <= 0.5)
                {
                    double hfFactor = r <= 0.35 ? 0.65 : 0.8;
                    bppf_target *= hfFactor;
                }

                double kbpsD = (bppf_target * fps * scaledPixels) / 1000.0; // back to kbps
                videoKbps = (int)Math.Max(100, Math.Min(50000, kbpsD));
            }

            long estBytes = CalculateOutputSize(videoKbps, audioKbps, durationSec);
            // Account for typical container overhead reductions (our model already operates on bitrates)
            // Subtract ~3% to better match mp4 +faststart muxing overhead behavior seen in practice
            estBytes = (long)Math.Max(0, estBytes * 0.97);

            // time estimate
            var estTime = EstimateConversionTime(info, videoCodec, targetWidth, targetHeight, durationSec, crf);

            var estimate = new ConversionEstimate
            {
                InputFileSizeBytes = info.SizeBytes,
                EstimatedOutputSizeBytes = estBytes,
                EstimatedDuration = estTime,
                CompressionRatio = Math.Min(1.0, Math.Max(0.0, estBytes / (double)Math.Max(1, info.SizeBytes))),
                SpaceSavedBytes = Math.Max(0, info.SizeBytes - estBytes)
            };

            return estimate;
        }

        private async Task<VideoInfo> AnalyzeVideo(string path, CancellationToken ct)
        {
            if (_cache.TryGetValue(path, out var cached))
                return cached;

            var fi = new FileInfo(path);
            var media = await FFmpeg.GetMediaInfo(path);
            ct.ThrowIfCancellationRequested();
            var v = media.VideoStreams?.FirstOrDefault();
            var a = media.AudioStreams?.FirstOrDefault();

            var info = new VideoInfo
            {
                Width = v?.Width ?? 0,
                Height = v?.Height ?? 0,
                Duration = media.Duration,
                SizeBytes = fi.Exists ? fi.Length : 0,
                Fps = v?.Framerate ?? 0,
                SourceAudioBitrateKbps = a != null && a.Bitrate > 0 ? (int)(a.Bitrate / 1000) : 0
            };
            _cache[path] = info;
            return info;
        }

        private long CalculateOutputSize(int videoBitrateKbps, int audioBitrateKbps, double durationSec)
        {
            long videoBits = (long)(videoBitrateKbps * 1000.0 * durationSec);
            long audioBits = (long)(audioBitrateKbps * 1000.0 * durationSec);
            long totalBytes = (videoBits + audioBits) / 8;
            return Math.Max(0, totalBytes);
        }

        private TimeSpan EstimateConversionTime(VideoInfo original, string codec, int? targetWidth, int? targetHeight, double durationSec, int? crf)
        {
            // Base machine-dependent factors (conservative default)
            double baseSpeed = codec.StartsWith("libx265", StringComparison.OrdinalIgnoreCase) ? 1.0
                : codec.StartsWith("libvpx-vp9", StringComparison.OrdinalIgnoreCase) ? 0.8
                : codec.StartsWith("libsvtav1", StringComparison.OrdinalIgnoreCase) ? 1.2
                : codec.StartsWith("copy", StringComparison.OrdinalIgnoreCase) ? 50.0
                : 3.5; // libx264

            double scalingFactor = 1.0;
            if (targetWidth.HasValue && targetHeight.HasValue && original.Width > 0 && original.Height > 0)
            {
                int originalPixels = original.Width * original.Height;
                int targetPixels = targetWidth.Value * targetHeight.Value;
                scalingFactor = Math.Max(0.2, (double)targetPixels / Math.Max(1, originalPixels));
            }

            // CRF speed: higher CRF tends to be faster
            double crfSpeedAdj = 1.0;
            if (crf.HasValue)
            {
                crfSpeedAdj = Math.Pow(2.0, (crf.Value - 23) / 6.0); // CRF 29 ~ 2x faster than 23
                crfSpeedAdj = Math.Max(0.5, Math.Min(3.0, crfSpeedAdj));
            }

            // Resolution adjustment: lower resolutions are faster roughly sqrt ratio
            double resAdj = 1.0 / Math.Sqrt(scalingFactor);
            double speed = Math.Max(0.5, baseSpeed * resAdj * crfSpeedAdj);

            double seconds = durationSec / speed;
            seconds = Math.Max(seconds, Math.Min(durationSec * 0.02, 5));
            return TimeSpan.FromSeconds(seconds);
        }
    }
}
