using System.Collections.ObjectModel;
using System.ComponentModel;
using Converter.Models;

namespace Converter.Application.ViewModels
{
    public class MainViewModel
    {
        public BindingList<QueueItemViewModel> QueueItems { get; } = new();

        public ObservableCollection<ConversionProfile> Presets { get; } = new();

        public ConversionProfile? SelectedPreset { get; set; }

        public string FfmpegPath { get; set; } = string.Empty;
        public string OutputFolder { get; set; } = string.Empty;

        public bool IsBusy { get; set; }
        public string StatusText { get; set; } = string.Empty;
    }
}
