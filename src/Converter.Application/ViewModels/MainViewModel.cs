using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Converter.Application.Models;

namespace Converter.Application.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private string _ffmpegPath = string.Empty;
        private string _outputFolder = string.Empty;
        private bool _isBusy;
        private string _statusText = string.Empty;
        private ConversionProfile? _selectedPreset;

        public event PropertyChangedEventHandler? PropertyChanged;

        public MainViewModel()
        {
            QueueItems = new System.ComponentModel.BindingList<QueueItemViewModel>();
            Presets = new ObservableCollection<ConversionProfile>();
        }

        public System.ComponentModel.BindingList<QueueItemViewModel> QueueItems { get; }

        public ObservableCollection<ConversionProfile> Presets { get; }

        public string FfmpegPath
        {
            get => _ffmpegPath;
            set
            {
                if (_ffmpegPath != value)
                {
                    _ffmpegPath = value ?? string.Empty;
                    OnPropertyChanged(nameof(FfmpegPath));
                }
            }
        }

        public string OutputFolder
        {
            get => _outputFolder;
            set
            {
                if (_outputFolder != value)
                {
                    _outputFolder = value ?? string.Empty;
                    OnPropertyChanged(nameof(OutputFolder));
                }
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    OnPropertyChanged(nameof(IsBusy));
                }
            }
        }

        public string StatusText
        {
            get => _statusText;
            set
            {
                if (_statusText != value)
                {
                    _statusText = value ?? string.Empty;
                    OnPropertyChanged(nameof(StatusText));
                }
            }
        }

        public ConversionProfile? SelectedPreset
        {
            get => _selectedPreset;
            set
            {
                if (!Equals(_selectedPreset, value))
                {
                    _selectedPreset = value;
                    OnPropertyChanged(nameof(SelectedPreset));
                }
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
