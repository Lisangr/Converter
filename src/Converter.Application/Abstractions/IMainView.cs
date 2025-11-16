using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Converter.Application.Abstractions;

public interface IMainView
{
    // Events
    event EventHandler AddFilesRequested;
    event EventHandler StartConversionRequested;
    event EventHandler CancelConversionRequested;
    event EventHandler<ConversionProfile> PresetSelected;
    event EventHandler SettingsChanged;

    // Properties
    string FfmpegPath { get; set; }
    string OutputFolder { get; set; }
    ObservableCollection<ConversionProfile> AvailablePresets { get; set; }
    ConversionProfile? SelectedPreset { get; set; }
    
    // Methods
    void SetQueueItems(IEnumerable<QueueItemDto> items);
    void UpdateQueueItem(QueueItemDto item);
    void SetGlobalProgress(int percent, string status);
    void ShowError(string message);
    void ShowInfo(string message);
    void UpdatePresetControls(ConversionProfile preset);
    void SetBusy(bool isBusy);
    
    // File dialogs
    string? ShowOpenFileDialog(string title, string filter);
    string? ShowFolderBrowserDialog(string description);
    IEnumerable<string> ShowOpenMultipleFilesDialog(string title, string filter);
}

public sealed record QueueItemDto(
    Guid Id, 
    string FilePath, 
    long FileSizeBytes, 
    TimeSpan Duration, 
    int Progress, 
    string Status, 
    bool IsStarred, 
    int Priority, 
    string? OutputPath, 
    string? ErrorMessage
);
