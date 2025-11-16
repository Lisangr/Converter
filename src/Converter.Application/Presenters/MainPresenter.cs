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
using Converter.Application.DTOs;
using Converter.Domain.Models;

namespace Converter.Application.Presenters;

public sealed class MainPresenter : IDisposable
{
    private readonly IMainView _view;
    private readonly IQueueService _queueService;
    private readonly IConversionOrchestrator _orchestrator;
    private readonly INotificationGateway _notifications;
    private readonly ISettingsStore _settingsStore;
    private readonly IPresetRepository _presetRepository;
    private readonly ILogger<MainPresenter> _logger;
    private CancellationTokenSource? _cts;
    private bool _disposed;

public MainPresenter(
    IMainView view,
    IQueueService queueService,
    IConversionOrchestrator orchestrator,
    INotificationGateway notifications,
    ISettingsStore settingsStore,
    IPresetRepository presetRepository,
    ILogger<MainPresenter> logger)
{
    _view = view ?? throw new ArgumentNullException(nameof(view));
    _queueService = queueService ?? throw new ArgumentNullException(nameof(queueService));
    _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
    _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
    _settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
    _presetRepository = presetRepository ?? throw new ArgumentNullException(nameof(presetRepository));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    // Subscribe to queue events
    _queueService.ItemChanged += OnQueueItemChanged;
    _queueService.QueueCompleted += OnQueueCompleted;
}

    public async Task StartAsync()
    {
        try
        {
            // Load settings and presets asynchronously
            var ffmpegTask = _settingsStore.GetFfmpegPathAsync();
            var presetsTask = _presetRepository.GetAllPresetsAsync();

            await Task.WhenAll(ffmpegTask, presetsTask);

            var ffmpegPath = await ffmpegTask;
            if (!string.IsNullOrEmpty(ffmpegPath))
            {
                _view.FfmpegPath = ffmpegPath;
            }

            var presets = await presetsTask;
            _view.AvailablePresets = new ObservableCollection<ConversionProfile>(presets);

            // Subscribe to view events
            _view.AddFilesRequested += OnAddFilesRequested;
            _view.StartConversionRequested += OnStartRequested;
            _view.CancelConversionRequested += OnCancelRequested;
            _view.PresetSelected += OnPresetSelected;
            _view.SettingsChanged += OnSettingsChanged;

            _logger.LogInformation("MainPresenter initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing MainPresenter");
            _view.ShowError($"Failed to initialize application: {ex.Message}");
            throw; // Re-throw to allow proper error handling in the calling code
        }
    }

    private async Task OnAddFilesRequested(object? sender, EventArgs e)
    {
        try
        {
            var files = _view.ShowOpenMultipleFilesDialog("Select media files", 
                "Media Files|*.mp4;*.avi;*.mkv;*.mov;*.wmv;*.mp3;*.wav;*.aac|All Files|*.*");
            
            if (files == null || !files.Any()) 
            {
                await Task.CompletedTask;
                return;
            }

            var outputFolder = _view.OutputFolder;
            if (string.IsNullOrEmpty(outputFolder))
            {
                outputFolder = _view.ShowFolderBrowserDialog("Select output folder");
                if (string.IsNullOrEmpty(outputFolder)) 
                {
                    await Task.CompletedTask;
                    return;
                }
                _view.OutputFolder = outputFolder;
            }

            var items = files.Select(file => 
            {
                var fileInfo = new FileInfo(file);
                return new QueueItem
                {
                    Id = Guid.NewGuid(),
                    FilePath = file,
                    FileSizeBytes = fileInfo.Length,
                    Status = ConversionStatus.Pending,
                    Priority = 3,
                    OutputPath = Path.Combine(outputFolder, Path.ChangeExtension(fileInfo.Name, ".mp4")),
                    Duration = TimeSpan.Zero,
                    Progress = 0,
                    IsStarred = false,
                    ErrorMessage = null,
                    AddedAt = DateTime.Now
                };
            }).ToList();

            _queueService.EnqueueMany(items);
            _logger.LogInformation("Added {Count} files to queue", items.Count);
            _notifications.Info("Files added", $"Added {items.Count} file(s) to the queue");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding files to queue");
            _view.ShowError($"Failed to add files: {ex.Message}");
        }
    }

    private async Task OnStartRequested(object? sender, EventArgs e)
    {
        try
        {
            _cts = new CancellationTokenSource();
            await _queueService.StartAsync(_cts.Token);
            _view.SetGlobalProgress(0, "Conversion started...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting conversion");
            _view.ShowError($"Failed to start conversion: {ex.Message}");
        }
        finally
        {
            _view.SetBusy(false);
            _cts?.Dispose();
            _cts = null;
        }
    }

    private async Task OnCancelRequested(object? sender, EventArgs e)
    {
        try
        {
            _cts?.Cancel();
            _queueService.Stop();
            _view.SetGlobalProgress(0, "Operation cancelled");
            _view.ShowInfo("Conversion cancelled by user");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling operation");
            _view.ShowError($"Failed to cancel operation: {ex.Message}");
        }
    }

    private void OnQueueItemChanged(QueueItem item)
    {
        try
        {
            var items = _queueService.GetQueueItemDtos();
            _view.SetQueueItems(items);

            if (item.Status == ConversionStatus.Completed)
            {
                _notifications.Info("Conversion complete", $"Completed: {Path.GetFileName(item.FilePath)}");
            }
            else if (item.Status == ConversionStatus.Failed && !string.IsNullOrEmpty(item.ErrorMessage))
            {
                _notifications.Error("Conversion failed", $"Failed: {Path.GetFileName(item.FilePath)}\n{item.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling queue item changed event");
            _view.ShowError($"Error updating queue: {ex.Message}");
        }
    }

    private void OnQueueCompleted(object? sender, EventArgs e)
    {
        _view.SetGlobalProgress(100, "All conversions completed");
        _notifications.Info("All done", "All conversions have been completed");
    }

    private Task OnPresetSelected(object? sender, ConversionProfile preset)
    {
        try
        {
            if (preset == null) return Task.CompletedTask;
            _view.UpdatePresetControls(preset);
            _logger.LogInformation("Preset selected: {PresetName}", preset.Name);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying preset");
            _view.ShowError($"Failed to apply preset: {ex.Message}");
            return Task.FromException(ex);
        }
    }

    private async Task OnSettingsChanged(object? sender, EventArgs e)
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
            
            // Unsubscribe from view events
            if (_view != null)
            {
                _view.AddFilesRequested -= OnAddFilesRequested;
                _view.StartConversionRequested -= OnStartRequested;
                _view.CancelConversionRequested -= OnCancelRequested;
                _view.PresetSelected -= OnPresetSelected;
                _view.SettingsChanged -= OnSettingsChanged;
            }

            // Unsubscribe from queue events
            if (_queueService != null)
            {
                _queueService.ItemChanged -= OnQueueItemChanged;
                _queueService.QueueCompleted -= OnQueueCompleted;
                (_queueService as IDisposable)?.Dispose();
            }

            _logger.LogInformation("MainPresenter disposed");
        }
    }

}
