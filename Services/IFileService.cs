using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Converter.Services
{
    public interface IFileService
    {
        Task<Image> GetThumbnailAsync(string filePath, int width, int height, CancellationToken cancellationToken);
        Task<FileInfo> ProbeFileAsync(string filePath);
        string[] GetSupportedFileExtensions();
        bool IsFileSupported(string filePath);
        Image CreatePlaceholderThumbnail(int width, int height, string text);
    }
}