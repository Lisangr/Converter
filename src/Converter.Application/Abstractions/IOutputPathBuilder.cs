using System;
using Converter.Models;

namespace Converter.Application.Abstractions
{
    public interface IOutputPathBuilder
    {
        string BuildOutputPath(Converter.Models.QueueItem item, string outputDirectory, string fileExtension);
        string BuildOutputPath(Converter.Models.QueueItem item, Converter.Models.ConversionProfile profile);
        string GenerateUniqueFileName(string basePath);
    }
}