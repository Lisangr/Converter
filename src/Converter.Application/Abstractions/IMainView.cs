using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Converter.Application.Abstractions
{
    public interface IMainView
    {
        // Events
        event EventHandler AddFilesRequested;
        event EventHandler StartConversionRequested;
        event EventHandler CancelConversionRequested;
        event EventHandler<Converter.Models.ConversionProfile> PresetSelected;
        event EventHandler SettingsChanged;

        // Properties
        string FfmpegPath { get; set; }
        string OutputFolder { get; set; }
        ObservableCollection<Converter.Models.ConversionProfile> AvailablePresets { get; set; }
        Converter.Models.ConversionProfile? SelectedPreset { get; set; }
        
        // Methods
        void AddQueueItem(Converter.Models.QueueItem item);
        void UpdateQueueItem(Converter.Models.QueueItem item);
        void UpdateQueueItemProgress(Guid itemId, int progress);
        void RemoveQueueItem(Guid itemId);
        void UpdateQueue(IEnumerable<Converter.Models.QueueItem> items);
        void SetStatusText(string status);
        void SetBusy(bool isBusy);
        void ShowError(string message);
        void ShowInfo(string message);
        
        // File dialogs
        string[] ShowOpenFileDialog(string title, string filter);
        string? ShowFolderBrowserDialog(string description);
        IEnumerable<string> ShowOpenMultipleFilesDialog(string title, string filter);
    }
}