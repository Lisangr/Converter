using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Converter.Application.Abstractions
{
    public interface IMainView
    {
        // Traditional synchronous events (for backward compatibility)
        event EventHandler AddFilesRequested;
        event EventHandler StartConversionRequested;
        event EventHandler CancelConversionRequested;
        event EventHandler<Converter.Models.ConversionProfile> PresetSelected;
        event EventHandler SettingsChanged;
        event EventHandler<string[]>? FilesDropped;
        event EventHandler? RemoveSelectedFilesRequested;
        event EventHandler? ClearAllFilesRequested;

        // Async events for operations that require asynchronous handling
        event Func<Task> AddFilesRequestedAsync;
        event Func<Task> StartConversionRequestedAsync;
        event Func<Task> CancelConversionRequestedAsync;
        event Func<string[], Task> FilesDroppedAsync;
        event Func<Task> RemoveSelectedFilesRequestedAsync;
        event Func<Task> ClearAllFilesRequestedAsync;

        // Properties
        string FfmpegPath { get; set; }
        string OutputFolder { get; set; }
        ObservableCollection<Converter.Models.ConversionProfile> AvailablePresets { get; set; }
        Converter.Models.ConversionProfile? SelectedPreset { get; set; }

        // Binding properties for MVVM/MVP - единый источник правды
        BindingList<ViewModels.QueueItemViewModel>? QueueItemsBinding { get; set; }
        bool IsBusy { get; set; }
        string StatusText { get; set; }

        // UI notifications
        void ShowError(string message);
        void ShowInfo(string message);

        // Progress reporting
        void UpdateCurrentProgress(int percent);
        void UpdateTotalProgress(int percent);

        void SetMainPresenter(object presenter);

        // Utility methods for async event invocation
        Task RaiseAddFilesRequestedAsync();
        Task RaiseStartConversionRequestedAsync();
        Task RaiseCancelConversionRequestedAsync();
        Task RaiseFilesDroppedAsync(string[] files);
        Task RaiseRemoveSelectedFilesRequestedAsync();
        Task RaiseClearAllFilesRequestedAsync();
    }
}