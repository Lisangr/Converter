using System;
using System.Threading;
using System.Threading.Tasks;

namespace Converter.Application.Services.FileMedia;

public class ThumbnailService
{
    private readonly IThumbnailGenerator _generator;

    public ThumbnailService(IThumbnailGenerator generator)
    {
        _generator = generator ?? throw new ArgumentNullException(nameof(generator));
    }

    public Task<string> GenerateThumbnailAsync(
        string inputFile,
        string outputDirectory,
        int width = 320,
        int height = 180,
        CancellationToken cancellationToken = default)
    {
        return _generator.GenerateAsync(
            inputFile,
            outputDirectory,
            TimeSpan.Zero,
            width,
            height,
            null,
            cancellationToken);
    }
}
