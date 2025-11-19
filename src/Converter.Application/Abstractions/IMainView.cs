using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

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
        event EventHandler<string[]>? FilesDropped;
        event EventHandler? RemoveSelectedFilesRequested;
        event EventHandler? ClearAllFilesRequested;

        // Properties
        string FfmpegPath { get; set; }
        string OutputFolder { get; set; }
        ObservableCollection<Converter.Models.ConversionProfile> AvailablePresets { get; set; }
        Converter.Models.ConversionProfile? SelectedPreset { get; set; }

        // Binding properties for MVVM/MVP
        BindingList<Converter.Application.ViewModels.QueueItemViewModel>? QueueItemsBinding { get; set; }
        bool IsBusy { get; set; }
        string StatusText { get; set; }

        // UI notifications
        void ShowError(string message);
        void ShowInfo(string message);

        // Progress reporting
        void UpdateCurrentProgress(int percent);
        void UpdateTotalProgress(int percent);
    }
}