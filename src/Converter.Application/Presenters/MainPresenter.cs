using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Converter.Application.Abstractions;
using Converter.Extensions;
using Converter.Models;
using Microsoft.Extensions.Logging;

namespace Converter.Application.Presenters
{
    public sealed class MainPresenter : IDisposable
    {
        private readonly IMainView _view;
        private readonly IQueueRepository _queueRepository;
        private readonly IQueueProcessor _queueProcessor;
        private readonly IProfileProvider _profileProvider;
        private readonly IOutputPathBuilder _pathBuilder;
        private readonly IProgressReporter _progressReporter;
        private readonly ILogger<MainPresenter> _logger;
        private bool _disposed;
        private CancellationTokenSource _cancellationTokenSource;

        public MainPresenter(
            IMainView view,
            IQueueRepository queueRepository,
            IQueueProcessor queueProcessor,
            IProfileProvider profileProvider,
            IOutputPathBuilder pathBuilder,
            IProgressReporter progressReporter,
            ILogger<MainPresenter> logger)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _queueRepository = queueRepository ?? throw new ArgumentNullException(nameof(queueRepository));
            _queueProcessor = queueProcessor ?? throw new ArgumentNullException(nameof(queueProcessor));
            _profileProvider = profileProvider ?? throw new ArgumentNullException(nameof(profileProvider));
            _pathBuilder = pathBuilder ?? throw new ArgumentNullException(nameof(pathBuilder));
            _progressReporter = progressReporter ?? throw new ArgumentNullException(nameof(progressReporter));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cancellationTokenSource = new CancellationTokenSource();

            // Subscribe to queue events
            _queueRepository.ItemAdded += OnItemAdded;
            _queueRepository.ItemUpdated += OnItemUpdated;
            _queueRepository.ItemRemoved += OnItemRemoved;
            _queueProcessor.ItemStarted += OnItemStarted;
            _queueProcessor.ItemCompleted += OnItemCompleted;
            _queueProcessor.ItemFailed += OnItemFailed;
            _queueProcessor.ProgressChanged += OnProgressChanged;
            _queueProcessor.QueueCompleted += OnQueueCompleted;
        }

        public async Task InitializeAsync()
        {
            _logger.LogInformation("Initializing MainPresenter");

            try
            {
                await LoadSettingsAsync();
                await LoadPresetsAsync();
                SubscribeToViewEvents();
                await LoadQueueAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing MainPresenter");
                _view.ShowError($"Failed to start application: {ex.Message}");
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
            var defaultProfile = await _profileProvider.GetDefaultProfileAsync();
            _view.SelectedPreset = defaultProfile;
        }

        private void SubscribeToViewEvents()
        {
            _view.AddFilesRequested += OnAddFilesRequested;
            _view.StartConversionRequested += OnStartConversionRequested;
            _view.CancelConversionRequested += OnCancelConversionRequested;
            _view.PresetSelected += OnPresetSelected;
            _view.SettingsChanged += OnSettingsChanged;
        }

        private void OnPresetSelected(object? sender, Converter.Models.ConversionProfile profile)
        {
            if (profile == null) return;
            _logger.LogInformation("Preset selected: {Name}", profile.Name);
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
                _view.UpdateQueue(items.ToList());
                _logger.LogInformation("Loaded {Count} items into the queue", items.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading queue");
                _view.ShowError($"Failed to load queue: {ex.Message}");
            }
        }

        private async void OnAddFilesRequested(object? sender, EventArgs e)
        {
            try
            {
                _logger.LogInformation("Adding files to queue");
                var filePaths = _view.ShowOpenFileDialog(
                    "Select files to convert", 
                    "Video Files|*.mp4;*.avi;*.mkv;*.mov;*.wmv|All Files|*.*");
                
                if (filePaths == null || !filePaths.Any())
                {
                    _logger.LogInformation("No files selected");
                    return;
                }

                var items = new List<QueueItem>();
                foreach (var filePath in filePaths)
                {
                    try
                    {
                        var fileInfo = new FileInfo(filePath);
                        var item = new QueueItem
                        {
                            Id = Guid.NewGuid(),
                            FilePath = filePath,
                            FileSizeBytes = fileInfo.Length,
                            Status = ConversionStatus.Pending,
                            AddedAt = DateTime.UtcNow
                        };
                        
                        items.Add(item);
                        _logger.LogDebug("Added file to queue: {FilePath}", filePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error adding file to queue: {FilePath}", filePath);
                        _view.ShowError($"Error adding file '{Path.GetFileName(filePath)}': {ex.Message}");
                    }
                }

                if (items.Any())
                {
                    await _queueRepository.AddRangeAsync(items);
                    _view.ShowInfo($"Added {items.Count} file(s) to the queue");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnAddFilesRequested");
                _view.ShowError($"Failed to add files: {ex.Message}");
            }
        }

        private async void OnStartConversionRequested(object? sender, EventArgs e)
        {
            try
            {
                _logger.LogInformation("Starting conversion");
                _view.SetBusy(true);

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
                _view.SetBusy(false);
            }
        }

        private async void OnCancelConversionRequested(object? sender, EventArgs e)
        {
            try
            {
                _logger.LogInformation("Canceling conversion");
                _view.SetBusy(true);

                await _queueProcessor.StopProcessingAsync();

                _view.ShowInfo("Conversion cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error canceling conversion");
                _view.ShowError($"Failed to cancel conversion: {ex.Message}");
            }
            finally
            {
                _view.SetBusy(false);
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

        private void OnItemAdded(object sender, QueueItem item)
        {
            InvokeOnUiThread(() =>
            {
                _view.AddQueueItem(item);
                _view.SetStatusText($"Added {item.FileName} to queue");
            });
        }

        private void OnItemUpdated(object sender, QueueItem item)
        {
            InvokeOnUiThread(() =>
            {
                _view.UpdateQueueItem(item);
                _view.SetStatusText($"Updated {item.FileName} - {item.Status}");
            });
        }

        private void OnItemRemoved(object sender, Guid itemId)
        {
            InvokeOnUiThread(() =>
            {
                _view.RemoveQueueItem(itemId);
                _view.SetStatusText("Item removed from queue");
            });
        }

        private void OnItemStarted(object? sender, QueueItem item)
        {
            InvokeOnUiThread(() =>
            {
                item.Status = ConversionStatus.Processing;
                _view.UpdateQueueItem(item);
                _view.SetStatusText($"Processing {item.FileName}...");
            });
        }

        private void OnItemCompleted(object? sender, QueueItem item)
        {
            InvokeOnUiThread(() =>
            {
                item.Status = ConversionStatus.Completed;
                _view.UpdateQueueItem(item);
                _view.SetStatusText($"Completed: {item.FileName}");
            });
        }

        private void OnItemFailed(object? sender, QueueItem item)
        {
            InvokeOnUiThread(() =>
            {
                item.Status = ConversionStatus.Failed;
                _view.UpdateQueueItem(item);
                _view.ShowError($"Failed to process {item.FileName}: {item.ErrorMessage}");
            });
        }

        private void OnProgressChanged(object sender, QueueProgressEventArgs e)
        {
            InvokeOnUiThread(() =>
            {
                e.Item.Progress = e.Progress;
                _view.UpdateQueueItemProgress(e.Item.Id, e.Progress);

                if (!string.IsNullOrEmpty(e.Status))
                {
                    _view.SetStatusText($"{e.Status} ({e.Progress}%)");
                }
                else
                {
                    _view.SetStatusText($"Processing {e.Item.FileName} - {e.Progress}%");
                }
            });
        }

        private void OnQueueCompleted(object sender, EventArgs e)
        {
            InvokeOnUiThread(() =>
            {
                _view.SetStatusText("Queue processing completed");
                _view.SetBusy(false);
                _view.ShowInfo("All items have been processed");
            });
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
    }
}

