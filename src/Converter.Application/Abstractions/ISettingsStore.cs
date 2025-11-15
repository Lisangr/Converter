namespace Converter.Application.Abstractions;

public interface ISettingsStore
{
    Task<string?> GetFfmpegPathAsync();
    Task SetFfmpegPathAsync(string path);
}
