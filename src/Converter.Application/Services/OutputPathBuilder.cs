/// <summary>
/// Построитель путей для выходных файлов конвертации.
/// Обеспечивает:
/// - Гибкое формирование путей к выходным файлам
/// - Поддержку различных стратегий именования
/// - Автоматическое разрешение конфликтов имён
/// - Независимость от конкретной файловой системы
/// </summary>
using System;
using System.IO;
using System.Text.RegularExpressions;
using Converter.Application.Abstractions;
using Converter.Domain.Models;
using Converter.Models;
using Microsoft.Extensions.Logging;

namespace Converter.Application.Services
{
    public class OutputPathBuilder : IOutputPathBuilder
    {
        private static readonly Regex InvalidCharsRegex = new("[\\/:*?\"<>|]");
        private readonly ILogger<OutputPathBuilder> _logger;

        public OutputPathBuilder(ILogger<OutputPathBuilder> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string BuildOutputPath(QueueItem item, string outputDirectory, string fileExtension)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (string.IsNullOrEmpty(outputDirectory)) 
                throw new ArgumentException("Output directory cannot be null or empty", nameof(outputDirectory));
            if (string.IsNullOrEmpty(fileExtension))
                throw new ArgumentException("File extension cannot be null or empty", nameof(fileExtension));

            // Ensure the extension starts with a dot
            if (!fileExtension.StartsWith("."))
            {
                fileExtension = $".{fileExtension}";
            }

            var fileName = Path.GetFileNameWithoutExtension(item.FilePath);
            var safeFileName = SanitizeFileName(fileName);
            var outputPath = Path.Combine(outputDirectory, $"{safeFileName}{fileExtension}");

            // If file exists, append a number to make it unique
            if (File.Exists(outputPath))
            {
                outputPath = GenerateUniqueFileName(outputPath);
            }

            _logger.LogDebug("Built output path: {OutputPath}", outputPath);
            return outputPath;
        }

        public string BuildOutputPath(QueueItem item, Models.ConversionProfile profile)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (profile == null) throw new ArgumentNullException(nameof(profile));

            var outputDir = Path.GetDirectoryName(item.FilePath) ?? string.Empty;
            var extension = Path.GetExtension(item.FilePath);

            return BuildOutputPath(item, outputDir, extension);
        }

        public string GenerateUniqueFileName(string basePath)
        {
            if (string.IsNullOrEmpty(basePath))
                throw new ArgumentException("Base path cannot be null or empty", nameof(basePath));

            var directory = Path.GetDirectoryName(basePath) ?? string.Empty;
            var fileName = Path.GetFileNameWithoutExtension(basePath);
            var extension = Path.GetExtension(basePath);
            
            var counter = 1;
            var newPath = basePath;
            
            while (File.Exists(newPath))
            {
                newPath = Path.Combine(directory, $"{fileName} ({counter++}){extension}");
            }

            _logger.LogDebug("Generated unique file name: {NewPath}", newPath);
            return newPath;
        }

        private static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return string.Empty;

            // Replace invalid characters with underscore
            return InvalidCharsRegex.Replace(fileName, "_");
        }
    }
}
