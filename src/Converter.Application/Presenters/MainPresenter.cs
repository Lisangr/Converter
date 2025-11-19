using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Converter.Application.Abstractions;
using Converter.Application.ViewModels;
using Converter.Domain.Models;
using Converter.Extensions;
using Converter.Models;
using Microsoft.Extensions.Logging;

namespace Converter.Application.Presenters
{
    public sealed class MainPresenter : IDisposable
    {
        private readonly IMainView _view;
        private readonly MainViewModel _viewModel;
        private readonly IQueueRepository _queueRepository;
        private readonly IQueueProcessor _queueProcessor;
        private readonly IProfileProvider _profileProvider;
        private readonly IOutputPathBuilder _pathBuilder;
        private readonly IProgressReporter _progressReporter;
        private readonly IFilePicker _filePicker;
        private readonly ILogger<MainPresenter> _logger;
        private bool _disposed;
        private CancellationTokenSource _cancellationTokenSource;

        public MainPresenter(
            IMainView view,
            MainViewModel viewModel,
            IQueueRepository queueRepository,
            IQueueProcessor queueProcessor,
            IProfileProvider profileProvider,
            IOutputPathBuilder pathBuilder,
            IProgressReporter progressReporter,
            IFilePicker filePicker,
            ILogger<MainPresenter> logger)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _queueRepository = queueRepository ?? throw new ArgumentNullException(nameof(queueRepository));
            _queueProcessor = queueProcessor ?? throw new ArgumentNullException(nameof(queueProcessor));
            _profileProvider = profileProvider ?? throw new ArgumentNullException(nameof(profileProvider));
            _pathBuilder = pathBuilder ?? throw new ArgumentNullException(nameof(pathBuilder));
            _progressReporter = progressReporter ?? throw new ArgumentNullException(nameof(progressReporter));
            _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cancellationTokenSource = new CancellationTokenSource();

            // Subscribe to queue events
            _queueRepository.ItemAdded += OnItemAdded;
            _queueRepository.ItemUpdated += OnItemUpdated;
            _queueRepository.ItemRemoved += OnItemRemoved;
            
            // Subscribe to queue processor events for progress updates
            _queueProcessor.ItemStarted += OnItemStarted;
            _queueProcessor.ItemCompleted += OnItemCompleted;
            _queueProcessor.ItemFailed += OnItemFailed;
            _queueProcessor.ProgressChanged += OnProgressChanged;
            _queueProcessor.QueueCompleted += OnQueueCompleted;
            
            // Subscribe to view events via async adapters
            _view.AddFilesRequested += MakeAsyncEventHandler(OnAddFilesRequestedAsync, "adding files");
            _view.StartConversionRequested += MakeAsyncEventHandler(OnStartConversionRequestedAsync, "starting conversion");
            _view.CancelConversionRequested += MakeAsyncEventHandler(OnCancelConversionRequestedAsync, "canceling conversion");
            _view.PresetSelected += OnPresetSelected;
            _view.FilesDropped += MakeAsyncEventHandler(OnFilesDroppedAsync, "adding dropped files");
            _view.RemoveSelectedFilesRequested += MakeAsyncEventHandler(OnRemoveSelectedFilesRequestedAsync, "removing selected files");
            _view.ClearAllFilesRequested += MakeAsyncEventHandler(OnClearAllFilesRequestedAsync, "clearing all files");
            _view.SettingsChanged += OnSettingsChanged;
        }

        public async Task InitializeAsync()
        {
            _logger.LogInformation("Initializing MainPresenter");

            try
            {
                _view.IsBusy = true;
                _view.StatusText = "Initializing application...";
                
                // Load settings and presets in parallel
                await Task.WhenAll(
                    LoadSettingsAsync(),
                    LoadPresetsAsync()
                );
                
                // Initialize UI bindings
                _view.QueueItemsBinding = _viewModel.QueueItems;
                
                // Load initial queue
                await LoadQueueAsync();
                
                _view.StatusText = "Ready";
                _logger.LogInformation("MainPresenter initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing MainPresenter");
                _view.ShowError($"Failed to start application: {ex.Message}");
                throw; // Re-throw to allow the application to handle the error
            }
            finally
            {
                _view.IsBusy = false;
            }
        }

        private async Task LoadSettingsAsync()
        {
            // Placeholder for settings loading
            await Task.CompletedTask;
        }

        private async Task LoadPresetsAsync()
        {
            // Load profiles from provider and push to view
            var profiles = await _profileProvider.GetAllProfilesAsync();
            _view.AvailablePresets = new System.Collections.ObjectModel.ObservableCollection<Converter.Models.ConversionProfile>(profiles);

            _viewModel.Presets.Clear();
            foreach (var profile in profiles)
            {
                _viewModel.Presets.Add(profile);
            }

            var defaultProfile = await _profileProvider.GetDefaultProfileAsync();
            _view.SelectedPreset = defaultProfile;
            _viewModel.SelectedPreset = defaultProfile;
        }

        private void OnPresetSelected(object? sender, Converter.Models.ConversionProfile profile)
        {
            if (profile == null) return;
            _logger.LogInformation("Preset selected: {Name}", profile.Name);
            _viewModel.SelectedPreset = profile;
            _view.ShowInfo($"Preset selected: {profile.Name}");
        }

        private void OnSettingsChanged(object? sender, EventArgs e)
        {
            _logger.LogInformation("Settings changed");
        }

        private async Task LoadQueueAsync()
        {
            try
            {
                _logger.LogInformation("Loading queue");
                var items = await _queueRepository.GetAllAsync();
                var list = items.ToList();

                // Заполняем ViewModel
                _viewModel.QueueItems.Clear();
                foreach (var item in list)
                {
                    _viewModel.QueueItems.Add(QueueItemViewModel.FromModel(item));
                }
                _logger.LogInformation("Loaded {Count} items into the queue", items.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading queue");
                _view.ShowError($"Failed to load queue: {ex.Message}");
            }
        }

        private async Task OnAddFilesRequestedAsync(object? sender, EventArgs e)
        {
            try
            {
                _view.IsBusy = true;
                _view.StatusText = "Adding files...";
                _logger.LogInformation("Adding files to queue");
                
                var filePaths = _filePicker.PickFiles(
                    "Select files to convert", 
                    "Video Files|*.mp4;*.avi;*.mkv;*.mov;*.wmv|All Files|*.*");
                
                if (filePaths == null || !filePaths.Any())
                {
                    _logger.LogInformation("No files selected");
                    _view.StatusText = "No files selected";
                    return;
                }

                var addedCount = 0;
                foreach (var filePath in filePaths)
                {
                    try
                    {
                        if (!File.Exists(filePath)) continue;
                        if (_viewModel.QueueItems.Any(item => item.FilePath == filePath)) 
                        {
                            _logger.LogInformation("File already in queue: {FilePath}", filePath);
                            continue;
                        }

                        var fileInfo = new FileInfo(filePath);
                        var outputDir = !string.IsNullOrWhiteSpace(_view.OutputFolder)
                            ? _view.OutputFolder
                            : Path.GetDirectoryName(filePath) ?? string.Empty;
                        var item = new QueueItem
                        {
                            Id = Guid.NewGuid(),
                            FilePath = filePath,
                            FileSizeBytes = fileInfo.Length,
                            OutputDirectory = outputDir,
                            Status = ConversionStatus.Pending,
                            AddedAt = DateTime.UtcNow
                        };
                        
                        await _queueRepository.AddAsync(item);
                        addedCount++;
                        _logger.LogDebug("Added file to queue: {FilePath}", filePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error adding file to queue: {FilePath}", filePath);
                        _view.ShowError($"Error adding file '{Path.GetFileName(filePath)}': {ex.Message}");
                    }
                }

                if (addedCount > 0)
                {
                    _view.StatusText = $"Added {addedCount} file(s) to queue";
                    _view.ShowInfo($"Added {addedCount} file(s) to the queue");
                    await LoadQueueAsync();
                }
                else
                {
                    _view.StatusText = "No new files were added to the queue";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnAddFilesRequested");
                _view.ShowError($"Failed to add files: {ex.Message}");
            }
            finally
            {
                _view.IsBusy = false;
            }
        }

        private async Task OnStartConversionRequestedAsync(object? sender, EventArgs e)
        {
            try
            {
                _logger.LogInformation("Starting conversion");
                _view.IsBusy = true;

                EnsureProcessingCancellationToken();
                
                _logger.LogInformation("Queue items count before start: {Count}", _viewModel.QueueItems.Count);
                foreach (var item in _viewModel.QueueItems)
                {
                    _logger.LogInformation("Queue item: {FileName} - {Status} - {Progress}%", 
                        item.FileName, item.Status, item.Progress);
                }

                await _queueProcessor.StartProcessingAsync(_cancellationTokenSource.Token);

                _view.ShowInfo("Conversion started");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting conversion");
                _view.ShowError($"Failed to start conversion: {ex.Message}");
            }
            finally
            {
                _view.IsBusy = false;
            }
        }

        private async Task OnCancelConversionRequestedAsync(object? sender, EventArgs e)
        {
            try
            {
                _logger.LogInformation("Canceling conversion");
                _view.IsBusy = true;

                _cancellationTokenSource.Cancel();
                await _queueProcessor.StopProcessingAsync();
                ResetProcessingCancellationToken();

                _view.ShowInfo("Conversion cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error canceling conversion");
                _view.ShowError($"Failed to cancel conversion: {ex.Message}");
            }
            finally
            {
                _view.IsBusy = false;
            }
        }

        private void InvokeOnUiThread(Action action)
        {
            if (_view is Control control)
            {
                control.InvokeIfRequired(action);
            }
            else
            {
                action();
            }
        }

        private void OnItemAdded(object? sender, QueueItem item)
        {
            InvokeOnUiThread(() =>
            {
                // ViewModel
                _viewModel.QueueItems.Add(QueueItemViewModel.FromModel(item));
                _view.StatusText = $"Added {item.FileName} to queue";
            });
        }

        private void OnItemUpdated(object? sender, QueueItem item)
        {
            InvokeOnUiThread(() =>
            {
                var vm = _viewModel.QueueItems.FirstOrDefault(q => q.Id == item.Id);
                vm?.UpdateFromModel(item);
                _view.StatusText = $"Updated {item.FileName} - {item.Status}";
            });
        }

        private void OnItemRemoved(object? sender, Guid itemId)
        {
            InvokeOnUiThread(() =>
            {
                var vm = _viewModel.QueueItems.FirstOrDefault(q => q.Id == itemId);
                if (vm != null)
                {
                    _viewModel.QueueItems.Remove(vm);
                }
                _view.StatusText = "Item removed from queue";
            });
        }

        private void OnItemStarted(object? sender, QueueItem item)
        {
            InvokeOnUiThread(() =>
            {
                item.Status = ConversionStatus.Processing;
                item.StartedAt = DateTime.UtcNow;
                var vm = _viewModel.QueueItems.FirstOrDefault(q => q.Id == item.Id);
                if (vm != null)
                {
                    vm.Status = item.Status;
                    vm.Progress = 0; // Reset progress when starting
                }
                _view.StatusText = $"Processing {item.FileName}...";
            });
        }

        private void OnItemCompleted(object? sender, QueueItem item)
        {
            InvokeOnUiThread(() =>
            {
                item.Status = ConversionStatus.Completed;
                item.CompletedAt = DateTime.UtcNow;
                item.Progress = 100;
                var vm = _viewModel.QueueItems.FirstOrDefault(q => q.Id == item.Id);
                if (vm != null)
                {
                    vm.Status = item.Status;
                    vm.Progress = 100;
                    vm.OutputFileSizeBytes = item.OutputFileSizeBytes;
                }
                _view.StatusText = $"Completed: {item.FileName}";
            });
        }

        private void OnItemFailed(object? sender, QueueItem item)
        {
            InvokeOnUiThread(() =>
            {
                item.Status = ConversionStatus.Failed;
                var vm = _viewModel.QueueItems.FirstOrDefault(q => q.Id == item.Id);
                if (vm != null)
                {
                    vm.Status = item.Status;
                    vm.ErrorMessage = item.ErrorMessage;
                }
                _view.ShowError($"Failed to process {item.FileName}: {item.ErrorMessage}");
            });
        }

        private void OnProgressChanged(object? sender, QueueProgressEventArgs e)
        {
            InvokeOnUiThread(() =>
            {
                _logger.LogDebug("Progress changed for item {ItemId}: {Progress}%", e.Item.Id, e.Progress);
                
                // Обновляем ViewModel
                var vm = _viewModel.QueueItems.FirstOrDefault(q => q.Id == e.Item.Id);
                if (vm != null)
                {
                    _logger.LogDebug("Updating ViewModel {ItemId}: Progress={Progress}, Status={Status}", 
                        vm.Id, e.Progress, e.Item.Status);
                    vm.Progress = e.Progress;
                    vm.Status = e.Item.Status;
                    vm.ErrorMessage = e.Item.ErrorMessage;
                }
                else
                {
                    _logger.LogWarning("ViewModel not found for item {ItemId}", e.Item.Id);
                }

                // Обновляем прогресс текущего элемента
                _view.UpdateCurrentProgress(e.Progress);

                // Считаем суммарный прогресс по очереди
                if (_viewModel.QueueItems.Any())
                {
                    var total = _viewModel.QueueItems.Average(x => x.Progress);
                    _view.UpdateTotalProgress((int)total);
                    _logger.LogDebug("Total progress updated: {Total}%", (int)total);
                }

                if (!string.IsNullOrEmpty(e.Status))
                {
                    _view.StatusText = $"{e.Status} ({e.Progress}%)";
                }
                else
                {
                    _view.StatusText = $"Processing {e.Item.FileName} - {e.Progress}%";
                }
            });
        }

        private void OnQueueCompleted(object? sender, EventArgs e)
        {
            InvokeOnUiThread(() =>
            {
                _view.StatusText = "Queue processing completed";
                _view.IsBusy = false;
                _view.ShowInfo("All items have been processed");
				_view.UpdateCurrentProgress(0);
				_view.UpdateTotalProgress(100);
            });
        }

        private async Task OnFilesDroppedAsync(object? sender, string[] files)
        {
            var addedCount = 0;
            foreach (var file in files)
            {
                if (!File.Exists(file)) continue;
                if (_viewModel.QueueItems.Any(item => item.FilePath == file)) continue;
                var outputDir = !string.IsNullOrWhiteSpace(_view.OutputFolder)
                    ? _view.OutputFolder
                    : Path.GetDirectoryName(file) ?? string.Empty;
                var item = new QueueItem
                {
                    Id = Guid.NewGuid(),
                    FilePath = file,
                    FileSizeBytes = new FileInfo(file).Length,
                    OutputDirectory = outputDir,
                    Status = ConversionStatus.Pending,
                    AddedAt = DateTime.UtcNow
                };
                
                await _queueRepository.AddAsync(item);
                addedCount++;
            }
            
            if (addedCount > 0)
            {
                _view.StatusText = $"Добавлено файлов: {addedCount}";
                await LoadQueueAsync();
            }
        }

        private async Task OnRemoveSelectedFilesRequestedAsync(object? sender, EventArgs e)
        {
            var selectedItems = _viewModel.QueueItems
                .Where(item => item.IsSelected)
                .ToList();

            if (selectedItems.Count == 0)
            {
                _view.ShowInfo("No files selected for removal");
                return;
            }

            try
            {
                _view.IsBusy = true;
                _view.StatusText = $"Removing {selectedItems.Count} file(s)...";
                _logger.LogInformation("Removing {Count} selected files from queue", selectedItems.Count);

                foreach (var item in selectedItems)
                {
                    try
                    {
                        await _queueRepository.RemoveAsync(item.Id);
                        _logger.LogDebug("Removed file from queue: {FilePath}", item.FilePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error removing file from queue: {FilePath}", item.FilePath);
                        _view.ShowError($"Error removing '{item.FileName}': {ex.Message}");
                    }
                }

                _view.StatusText = $"Removed {selectedItems.Count} file(s) from queue";
                _view.ShowInfo($"Removed {selectedItems.Count} file(s) from the queue");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnRemoveSelectedFilesRequested");
                _view.ShowError($"Failed to remove files: {ex.Message}");
            }
            finally
            {
                _view.IsBusy = false;
            }
        }

        private async Task OnClearAllFilesRequestedAsync(object? sender, EventArgs e)
        {
            if (_viewModel.QueueItems.Count == 0)
            {
                _view.ShowInfo("Queue is already empty");
                return;
            }

            try
            {
                _view.IsBusy = true;
                _view.StatusText = "Clearing all files from queue...";
                _logger.LogInformation("Clearing all files from queue");

                var allItems = _viewModel.QueueItems.ToList();
                foreach (var item in allItems)
                {
                    try
                    {
                        await _queueRepository.RemoveAsync(item.Id);
                        _logger.LogDebug("Removed file from queue: {FilePath}", item.FilePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error removing file from queue: {FilePath}", item.FilePath);
                        _view.ShowError($"Error removing '{item.FileName}': {ex.Message}");
                    }
                }

                _view.StatusText = "Queue cleared";
                _view.ShowInfo("All files have been removed from the queue");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnClearAllFilesRequested");
                _view.ShowError($"Failed to clear queue: {ex.Message}");
            }
            finally
            {
                _view.IsBusy = false;
            }
        }

        private EventHandler MakeAsyncEventHandler(Func<object?, EventArgs, Task> handler, string operation)
        {
            return (sender, args) =>
            {
                _ = HandleViewEventAsync(() => handler(sender, args), operation);
            };
        }

        private EventHandler<string[]> MakeAsyncEventHandler(Func<object?, string[], Task> handler, string operation)
        {
            return (sender, files) =>
            {
                _ = HandleViewEventAsync(() => handler(sender, files), operation);
            };
        }

        private async Task HandleViewEventAsync(Func<Task> action, string operation)
        {
            try
            {
                await action().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error while {Operation}", operation);
                _view.ShowError($"An unexpected error occurred while {operation}: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                // Unsubscribe from events
                if (_queueRepository != null)
                {
                    _queueRepository.ItemAdded -= OnItemAdded;
                    _queueRepository.ItemUpdated -= OnItemUpdated;
                    _queueRepository.ItemRemoved -= OnItemRemoved;
                }

                if (_queueProcessor != null)
                {
                    _queueProcessor.ItemStarted -= OnItemStarted;
                    _queueProcessor.ItemCompleted -= OnItemCompleted;
                    _queueProcessor.ItemFailed -= OnItemFailed;
                    _queueProcessor.ProgressChanged -= OnProgressChanged;
                    _queueProcessor.QueueCompleted -= OnQueueCompleted;
                    (_queueProcessor as IDisposable)?.Dispose();
                }

                _cancellationTokenSource?.Dispose();
                _disposed = true;
            }
        }

        private void EnsureProcessingCancellationToken()
        {
            if (_cancellationTokenSource == null || _cancellationTokenSource.IsCancellationRequested)
            {
                ResetProcessingCancellationToken();
            }
        }

        private void ResetProcessingCancellationToken()
        {
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
        }
    }
}

