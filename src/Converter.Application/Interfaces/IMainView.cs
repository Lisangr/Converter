using Converter.Application.ViewModels;
using Converter.Domain.Models;

namespace Converter.Application.Interfaces;

public interface IMainView
{
    event EventHandler? ViewLoaded;
    event EventHandler? AddFilesRequested;
    event EventHandler? StartConversionRequested;
    event EventHandler<Guid>? CancelConversionRequested;
    event EventHandler<Guid>? RemoveItemRequested;

    IReadOnlyList<string> SelectedInputFiles { get; }
    string? OutputDirectory { get; }
    ConversionProfile? SelectedProfile { get; }

    void SetAvailableProfiles(IReadOnlyList<ConversionProfile> profiles);
    void DisplayQueueItems(IReadOnlyList<QueueItemViewModel> items);
    void UpdateProgress(Guid queueItemId, ConversionProgress progress);
    void UpdateStatus(Guid queueItemId, string statusText);
    void DisplayThumbnail(Guid queueItemId, Stream thumbnailStream);
    void SetBusy(bool isBusy);
    void ShowError(string message);
    void ShowInfo(string message);
}
