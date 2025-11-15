using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using Converter.Application.Abstractions;
using Converter.Application.Builders;

namespace Converter.Application.Presenters;

public sealed class MainPresenter : IDisposable
{
    private readonly IMainView _view;
    private readonly IQueueService _queue;
    private readonly IConversionOrchestrator _orchestrator;
    private readonly INotificationGateway _notifications;
    private readonly ISettingsStore _settingsStore;
    private readonly IPresetRepository _presetRepository;
    private readonly ILogger<MainPresenter> _logger;
    private CancellationTokenSource? _cts;
    private bool _disposed;

    public MainPresenter(
        IMainView view,
        IQueueService queue,
        IConversionOrchestrator orchestrator,
        INotificationGateway notifications,
        ISettingsStore settingsStore,
        IPresetRepository presetRepository,
        ILogger<MainPresenter> logger)
    {
        _view = view ?? throw new ArgumentNullException(nameof(view));
        _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        _settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
        _presetRepository = presetRepository ?? throw new ArgumentNullException(nameof(presetRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        InitializeAsync().ConfigureAwait(false);
    }

    private async Task InitializeAsync()
    {
        try
        {
            // Load settings and presets
            var ffmpegPath = await _settingsStore.GetFfmpegPathAsync();
            if (!string.IsNullOrEmpty(ffmpegPath))
            {
                _view.FfmpegPath = ffmpegPath;
            }

            var presets = await _presetRepository.GetAllPresetsAsync();
            _view.AvailablePresets = new ObservableCollection<ConversionProfile>(presets);

            // Subscribe to view events
            _view.AddFilesRequested += OnAddFilesRequested;
            _view.StartConversionRequested += OnStartRequested;
            _view.CancelConversionRequested += OnCancelRequested;
            _view.PresetSelected += OnPresetSelected;
            _view.SettingsChanged += OnSettingsChanged;

            // Subscribe to queue events
            _queue.ItemChanged += OnQueueItemChanged;
            _queue.QueueCompleted += OnQueueCompleted;

            _logger.LogInformation("MainPresenter initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing MainPresenter");
            _view.ShowError($"Failed to initialize application: {ex.Message}");
        }
    }

    private async void OnAddFilesRequested(object? sender, EventArgs e)
    {
        try
        {
            var files = _view.ShowOpenMultipleFilesDialog("Select media files", 
                "Media Files|*.mp4;*.avi;*.mkv;*.mov;*.wmv;*.mp3;*.wav;*.aac|All Files|*.*");
            
            if (files == null || !files.Any()) return;

            var outputFolder = _view.OutputFolder;
            if (string.IsNullOrEmpty(outputFolder))
            {
                outputFolder = _view.ShowFolderBrowserDialog("Select output folder");
                if (string.IsNullOrEmpty(outputFolder)) return;
                _view.OutputFolder = outputFolder;
            }

            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                var item = new QueueItemModel
                {
                    Id = Guid.NewGuid(),
                    FilePath = file,
                    FileSizeBytes = fileInfo.Length,
                    Status = "Pending",
                    Priority = 3, // Normal priority
                    OutputPath = Path.Combine(outputFolder, Path.ChangeExtension(fileInfo.Name, ".mp4")),
                    Duration = TimeSpan.Zero, // Will be updated later
                    Progress = 0,
                    IsStarred = false,
                    ErrorMessage = null
                };
                
                _queue.Enqueue(item);
                _logger.LogInformation("Added file to queue: {File}", file);
            }

            _notifications.Info("Files added", $"Added {files.Count()} file(s) to the queue");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding files to queue");
            _view.ShowError($"Failed to add files: {ex.Message}");
        }
    }

    private async void OnStartRequested(object? sender, EventArgs e)
    {
        try
        {
            _view.SetBusy(true);
            _cts = new CancellationTokenSource();
            await _queue.StartAsync(_cts.Token);
        }
        catch (OperationCanceledException)
        {
            _view.ShowInfo("Operation was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting conversion queue");
            _view.ShowError($"Failed to start conversion: {ex.Message}");
        }
        finally
        {
            _view.SetBusy(false);
            _cts?.Dispose();
            _cts = null;
        }
    }

    private void OnCancelRequested(object? sender, EventArgs e)
    {
        try
        {
            _cts?.Cancel();
            _queue.Stop();
            _view.SetGlobalProgress(0, "Operation cancelled");
            _view.ShowInfo("Conversion cancelled by user");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling operation");
            _view.ShowError($"Failed to cancel operation: {ex.Message}");
        }
    }

    private void OnQueueItemChanged(object? sender, QueueItemModel item)
    {
        _view.UpdateQueueItem(new QueueItemDto(
            item.Id,
            item.FilePath,
            item.FileSizeBytes,
            item.Duration,
            item.Progress,
            item.Status,
            item.IsStarred,
            item.Priority,
            item.OutputPath,
            item.ErrorMessage
        ));
        
        if (item.Status == "Completed")
        {
            _notifications.Info("Conversion complete", $"Completed: {Path.GetFileName(item.FilePath)}");
        }
        else if (item.Status == "Failed" && !string.IsNullOrEmpty(item.ErrorMessage))
        {
            _notifications.Error("Conversion failed", $"Failed: {Path.GetFileName(item.FilePath)}\n{item.ErrorMessage}");
        }
    }

    private void OnQueueCompleted(object? sender, EventArgs e)
    {
        _view.SetGlobalProgress(100, "All conversions completed");
        _notifications.Info("All done", "All conversions have been completed");
    }

    private void OnPresetSelected(object? sender, ConversionProfile preset)
    {
        try
        {
            if (preset == null) return;
            _view.UpdatePresetControls(preset);
            _logger.LogInformation("Preset selected: {PresetName}", preset.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying preset");
            _view.ShowError($"Failed to apply preset: {ex.Message}");
        }
    }

    private async void OnSettingsChanged(object? sender, EventArgs e)
    {
        try
        {
            if (!string.IsNullOrEmpty(_view.FfmpegPath))
            {
                await _settingsStore.SetFfmpegPathAsync(_view.FfmpegPath);
                _logger.LogInformation("FFmpeg path updated: {Path}", _view.FfmpegPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving settings");
            _view.ShowError($"Failed to save settings: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _cts?.Dispose();
            
            // Unsubscribe from events
            if (_view != null)
            {
                _view.AddFilesRequested -= OnAddFilesRequested;
                _view.StartConversionRequested -= OnStartRequested;
                _view.CancelConversionRequested -= OnCancelRequested;
                _view.PresetSelected -= OnPresetSelected;
                _view.SettingsChanged -= OnSettingsChanged;
            }

            if (_queue != null)
            {
                _queue.ItemChanged -= OnQueueItemChanged;
                _queue.QueueCompleted -= OnQueueCompleted;
            }

            _logger.LogInformation("MainPresenter disposed");
        }
    }

}
