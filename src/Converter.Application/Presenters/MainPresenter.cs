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
    private bool _initialized;
    private bool _eventsSubscribed;

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
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        if (_initialized)
        {
            return;
        }

        EnsureEventSubscriptions();
        _view.SetBusy(true);
        try
        {
            ct.ThrowIfCancellationRequested();

            var ffmpegPath = await _settingsStore.GetFfmpegPathAsync().ConfigureAwait(false);
            if (!string.IsNullOrEmpty(ffmpegPath))
            {
                _view.FfmpegPath = ffmpegPath;
            }

            var presets = await _presetRepository.GetAllPresetsAsync().ConfigureAwait(false);
            _view.AvailablePresets = new ObservableCollection<ConversionProfile>(presets);
            if (_view.SelectedPreset == null && _view.AvailablePresets.Any())
            {
                _view.SelectedPreset = _view.AvailablePresets.First();
            }

            var snapshot = _queue.Snapshot();
            _view.SetQueueItems(snapshot.Select(ToDto));

            _initialized = true;
            _logger.LogInformation("MainPresenter initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing MainPresenter");
            _view.ShowError($"Failed to initialize application: {ex.Message}");
            throw;
        }
        finally
        {
            _view.SetBusy(false);
        }
    }

    private void EnsureEventSubscriptions()
    {
        if (_eventsSubscribed)
        {
            return;
        }

        _view.AddFilesRequested += OnAddFilesRequestedAsync;
        _view.StartConversionRequested += OnStartRequestedAsync;
        _view.CancelConversionRequested += OnCancelRequestedAsync;
        _view.PresetSelected += OnPresetSelected;
        _view.SettingsChanged += OnSettingsChangedAsync;

        _queue.ItemChanged += OnQueueItemChanged;
        _queue.QueueCompleted += OnQueueCompleted;

        _eventsSubscribed = true;
    }

    private async Task OnAddFilesRequestedAsync(object? sender, EventArgs e)
    {
        try
        {
            var files = _view.ShowOpenMultipleFilesDialog(
                "Select media files",
                "Media Files|*.mp4;*.avi;*.mkv;*.mov;*.wmv;*.mp3;*.wav;*.aac|All Files|*.*")
                .ToArray();

            if (files.Length == 0)
            {
                return;
            }

            var preset = _view.SelectedPreset ?? _view.AvailablePresets.FirstOrDefault();
            if (preset == null)
            {
                _view.ShowError("Please select a preset before adding files.");
                return;
            }

            var outputFolder = _view.OutputFolder;
            if (string.IsNullOrWhiteSpace(outputFolder))
            {
                outputFolder = _view.ShowFolderBrowserDialog("Select output folder");
                if (string.IsNullOrWhiteSpace(outputFolder))
                {
                    return;
                }
                _view.OutputFolder = outputFolder;
            }

            var presetSnapshot = preset;
            var preparedItems = await Task.Run(() =>
                BuildQueueItems(files, outputFolder!, presetSnapshot));

            if (preparedItems.Count == 0)
            {
                return;
            }

            _queue.EnqueueMany(preparedItems);
            foreach (var item in preparedItems)
            {
                _logger.LogInformation("Added file to queue: {File}", item.FilePath);
            }

            _notifications.Info("Files added", $"Added {preparedItems.Count} file(s) to the queue");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding files to queue");
            _view.ShowError($"Failed to add files: {ex.Message}");
        }
    }

    private static List<QueueItemModel> BuildQueueItems(IEnumerable<string> files, string outputFolder, ConversionProfile preset)
    {
        var items = new List<QueueItemModel>();
        foreach (var file in files)
        {
            var fileInfo = new FileInfo(file);
            var fileSize = fileInfo.Exists ? fileInfo.Length : 0;
            var outputPath = Path.Combine(outputFolder, Path.ChangeExtension(Path.GetFileName(file), ".mp4"));

            items.Add(new QueueItemModel
            {
                Id = Guid.NewGuid(),
                FilePath = file,
                FileSizeBytes = fileSize,
                Status = QueueItemStatuses.Pending,
                Priority = 3,
                OutputPath = outputPath,
                Duration = TimeSpan.Zero,
                Progress = 0,
                IsStarred = false,
                ErrorMessage = null,
                Profile = preset,
                EnqueuedAtUtc = DateTime.UtcNow
            });
        }

        return items;
    }

    private async Task OnStartRequestedAsync(object? sender, EventArgs e)
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

    private Task OnCancelRequestedAsync(object? sender, EventArgs e)
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

        return Task.CompletedTask;
    }

    private void OnQueueItemChanged(object? sender, QueueItemSnapshot item)
    {
        _view.UpdateQueueItem(ToDto(item));

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

    private async Task OnSettingsChangedAsync(object? sender, EventArgs e)
    {
        try
        {
            if (!string.IsNullOrEmpty(_view.FfmpegPath))
            {
                await _settingsStore.SetFfmpegPathAsync(_view.FfmpegPath).ConfigureAwait(false);
                _logger.LogInformation("FFmpeg path updated: {Path}", _view.FfmpegPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving settings");
            _view.ShowError($"Failed to save settings: {ex.Message}");
        }
    }

    private static QueueItemDto ToDto(QueueItemSnapshot item) => new(
        item.Id,
        item.FilePath,
        item.FileSizeBytes,
        item.Duration,
        item.Progress,
        item.Status,
        item.IsStarred,
        item.Priority,
        item.OutputPath,
        item.ErrorMessage);

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _cts?.Dispose();

            if (_eventsSubscribed)
            {
                _view.AddFilesRequested -= OnAddFilesRequestedAsync;
                _view.StartConversionRequested -= OnStartRequestedAsync;
                _view.CancelConversionRequested -= OnCancelRequestedAsync;
                _view.PresetSelected -= OnPresetSelected;
                _view.SettingsChanged -= OnSettingsChangedAsync;

                _queue.ItemChanged -= OnQueueItemChanged;
                _queue.QueueCompleted -= OnQueueCompleted;
            }

            _logger.LogInformation("MainPresenter disposed");
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(MainPresenter));
        }
    }

}
