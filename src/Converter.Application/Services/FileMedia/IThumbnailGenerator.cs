using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace Converter.Application.Services.FileMedia;

public interface IThumbnailGenerator
{
    Task<string> GenerateAsync(
        string inputFile,
        string outputDirectory,
        TimeSpan position,
        int width,
        int height,
        Image? overlay,
        CancellationToken cancellationToken = default);
}
