using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Converter.Application.DTOs;

namespace Converter.Application.Abstractions;

public interface IMainView
{
    // Events
    event Func<object, EventArgs, Task> AddFilesRequested;
    event Func<object, EventArgs, Task> StartConversionRequested;
    event Func<object, EventArgs, Task> CancelConversionRequested;
    event Func<object, ConversionProfile, Task> PresetSelected;
    event Func<object, EventArgs, Task> SettingsChanged;

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
