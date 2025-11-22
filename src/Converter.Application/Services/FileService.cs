using System;
using System.Threading.Tasks;
using Converter.Application.Abstractions;

namespace Converter.Application.Services;

/// <summary>
/// Сервис работы с файлами.
/// </summary>
public class FileService : IFileService
{
    public Task<bool> ExistsAsync(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            throw new ArgumentException("Path cannot be null or empty", nameof(path));
        }

        return Task.FromResult(System.IO.File.Exists(path));
    }

    public Task<long> GetSizeAsync(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            throw new ArgumentException("Path cannot be null or empty", nameof(path));
        }

        var fileInfo = new System.IO.FileInfo(path);
        return Task.FromResult(fileInfo.Exists ? fileInfo.Length : 0L);
    }

    public Task<string> ReadAllTextAsync(string path)
    {
        return System.IO.File.ReadAllTextAsync(path);
    }

    public Task WriteAllTextAsync(string path, string content)
    {
        return System.IO.File.WriteAllTextAsync(path, content);
    }
}

/// <summary>
/// Интерфейс сервиса работы с файлами.
/// </summary>
public interface IFileService
{
    Task<bool> ExistsAsync(string path);
    Task<long> GetSizeAsync(string path);
    Task<string> ReadAllTextAsync(string path);
    Task WriteAllTextAsync(string path, string content);
}