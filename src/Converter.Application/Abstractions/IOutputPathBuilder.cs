using System;
using Converter.Domain.Models;
using Converter.Models;

namespace Converter.Application.Abstractions
{
    public interface IOutputPathBuilder
    {
        string BuildOutputPath(QueueItem item, string outputDirectory, string fileExtension);
        string BuildOutputPath(QueueItem item, Models.ConversionProfile profile);
        string GenerateUniqueFileName(string basePath);
    }
}