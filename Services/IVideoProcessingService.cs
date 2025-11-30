using System;
using System.Drawing;
using System.Threading.Tasks;
using Xabe.FFmpeg; // Keeping Xabe.FFmpeg as a fallback, assuming Models might be integrated

namespace Converter.Services
{
    public interface IVideoProcessingService
    {
        Task<IMediaInfo> GetMediaInfoAsync(string filePath);
        Task<string> TrimVideoAsync(string inputPath, TimeSpan startTime, TimeSpan endTime, Action<int> onProgress);
        Task<string> CropVideoAsync(string inputPath, Size cropSize, Point cropPoint, Action<int> onProgress);
        Task ExportVideoAsync(string inputPath, string outputPath, string? videoFilterGraph, Action<int> onProgress);
        void CleanupTemporaryFiles();
    }
}