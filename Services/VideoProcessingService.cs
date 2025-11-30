using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xabe.FFmpeg;
using Xabe.FFmpeg; // Keeping Xabe.FFmpeg as a fallback, assuming Models might be integrated

namespace Converter.Services
{
    public class VideoProcessingService : IVideoProcessingService, IDisposable
    {
        private string? _trimmedVideoTempPath;
        private string? _croppedVideoTempPath;

        public VideoProcessingService()
        {
            // Configure FFmpeg (assuming it's already configured globally or here)
            // Xabe.FFmpeg.FFmpeg.Set);// This line may be needed if not set globally
        }

        public async Task<IMediaInfo> GetMediaInfoAsync(string filePath)
        {
            return await FFmpeg.GetMediaInfo(filePath);
        }

        public async Task<string> TrimVideoAsync(string inputPath, TimeSpan startTime, TimeSpan endTime, Action<int> onProgress)
        {
            CleanupTemporaryFiles(); // Cleanup previous temp files before creating new ones
            var tempPath = Path.Combine(Path.GetTempPath(), $"preview_trim_{Guid.NewGuid()}.mp4");
            var duration = endTime - startTime;

            var conversion = FFmpeg.Conversions.New();
            conversion.AddParameter($"-ss {startTime} -i \"{inputPath}\" -t {duration} -c copy");
            conversion.SetOutput(tempPath);
            
            conversion.OnProgress += (sender, args) => onProgress?.Invoke(args.Percent);
            await conversion.Start();

            _trimmedVideoTempPath = tempPath;
            return tempPath;
        }

        public async Task<string> CropVideoAsync(string inputPath, Size cropSize, Point cropPoint, Action<int> onProgress)
        {
            // Do not cleanup _trimmedVideoTempPath here, as it might be the input
            if (!string.IsNullOrEmpty(_croppedVideoTempPath) && File.Exists(_croppedVideoTempPath))
            {
                try { File.Delete(_croppedVideoTempPath); } catch { /* Log or ignore */ }
            }

            var tempPath = Path.Combine(Path.GetTempPath(), $"preview_crop_{Guid.NewGuid()}.mp4");

            var conversion = FFmpeg.Conversions.New();
            conversion.AddParameter($"-i \"{inputPath}\" -vf \"crop={cropSize.Width}:{cropSize.Height}:{cropPoint.X}:{cropPoint.Y}\" -c:a copy");
            conversion.SetOutput(tempPath);
            
            conversion.OnProgress += (sender, args) => onProgress?.Invoke(args.Percent);
            await conversion.Start();

            _croppedVideoTempPath = tempPath;
            return tempPath;
        }

        public async Task ExportVideoAsync(string inputPath, string outputPath, string? videoFilterGraph, Action<int> onProgress)
        {
            var conversion = FFmpeg.Conversions.New();
            conversion.AddParameter($"-i \"{inputPath}\"");

            // Apply video filters if provided (brightness/contrast/gamma/vignette/etc.)
            if (!string.IsNullOrWhiteSpace(videoFilterGraph))
            {
                conversion.AddParameter($"-vf \"{videoFilterGraph}\"");
            }

            // For now, simple export without other effects
            conversion.AddParameter("-c:v libx264 -preset medium -crf 23");
            conversion.AddParameter("-c:a copy");
            conversion.SetOutput(outputPath);

            conversion.OnProgress += (sender, args) => onProgress?.Invoke(args.Percent);
            await conversion.Start();
        }

        public void CleanupTemporaryFiles()
        {
            if (!string.IsNullOrEmpty(_trimmedVideoTempPath) && File.Exists(_trimmedVideoTempPath))
            {
                try { File.Delete(_trimmedVideoTempPath); } catch (Exception ex) { Console.WriteLine($"Error deleting trimmed temp file: {ex.Message}"); }
                _trimmedVideoTempPath = null;
            }
            if (!string.IsNullOrEmpty(_croppedVideoTempPath) && File.Exists(_croppedVideoTempPath))
            {
                try { File.Delete(_croppedVideoTempPath); } catch (Exception ex) { Console.WriteLine($"Error deleting cropped temp file: {ex.Message}"); }
                _croppedVideoTempPath = null;
            }
        }

        public void Dispose()
        {
            CleanupTemporaryFiles();
        }
    }
}