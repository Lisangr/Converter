using Converter.Domain.Models;

namespace Converter.Application.Abstractions;

public interface IMainView
{
    event EventHandler? AddFilesRequested;
    event EventHandler? StartConversionRequested;
    event EventHandler? CancelRequested;
    event EventHandler? BrowseOutputFolderRequested;
    event EventHandler<ConversionProfile>? ProfileChanged;

    ConversionRequest? BuildConversionRequest(string inputFile);
    string? RequestInputFile();
    string? SelectOutputFolder();

    void BindProfiles(IEnumerable<ConversionProfile> profiles);
    void UpdateQueue(IEnumerable<QueueItem> items);
    void UpdateProgress(ConversionProgress progress);
    void SetBusyState(bool isBusy);
    void ShowError(string title, string message);
    void ShowInfo(string title, string message);
}
