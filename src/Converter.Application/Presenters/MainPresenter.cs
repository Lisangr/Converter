using Converter.Application.Interfaces;
using Converter.Application.ViewModels;
using Converter.Domain.Models;
using Microsoft.Extensions.Logging;

namespace Converter.Application.Presenters;

public sealed class MainPresenter : IAsyncDisposable
{
    private readonly IMainView _view;
    private readonly IQueueService _queueService;
    private readonly INotificationGateway _notifications;
    private readonly IThumbnailProvider _thumbnailProvider;
    private readonly ISettingsStore _settingsStore;
    private readonly IPresetRepository _presetRepository;
    private readonly ILogger<MainPresenter> _logger;
    private readonly SynchronizationContext _uiContext;
    private readonly CancellationTokenSource _lifetimeCts = new();
    private Task _pipeline = Task.CompletedTask;

    public MainPresenter(
        IMainView view,
        IQueueService queueService,
        INotificationGateway notifications,
        IThumbnailProvider thumbnailProvider,
        ISettingsStore settingsStore,
        IPresetRepository presetRepository,
        ILogger<MainPresenter> logger)
    {
        _view = view;
        _queueService = queueService;
        _notifications = notifications;
        _thumbnailProvider = thumbnailProvider;
        _settingsStore = settingsStore;
        _presetRepository = presetRepository;
        _logger = logger;
        _uiContext = SynchronizationContext.Current ?? new SynchronizationContext();

        _view.ViewLoaded += (_, _) => Enqueue(LoadAsync);
        _view.AddFilesRequested += (_, _) => Enqueue(_ => RefreshQueueSnapshotAsync());
        _view.StartConversionRequested += (_, _) => Enqueue(StartConversionAsync);
        _view.CancelConversionRequested += (_, id) => Enqueue(ct => CancelAsync(id));
        _view.RemoveItemRequested += (_, id) => Enqueue(ct => RemoveAsync(id));

        _queueService.ItemQueued += (_, item) => Enqueue(_ => OnItemQueuedAsync(item));
        _queueService.ProgressChanged += (_, payload) => Enqueue(_ => OnProgressAsync(payload.Item, payload.Progress));
        _queueService.ItemCompleted += (_, payload) => Enqueue(_ => OnCompletedAsync(payload.Item, payload.Result));
    }

    private void Enqueue(Func<CancellationToken, Task> action)
    {
        _pipeline = _pipeline.ContinueWith(async t =>
        {
            try
            {
                await action(_lifetimeCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled presenter error");
                await RunOnUiAsync(() =>
                {
                    _view.ShowError(ex.Message);
                    return Task.CompletedTask;
                }).ConfigureAwait(false);
            }
        }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default).Unwrap();
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var profiles = new List<ConversionProfile>();
        profiles.AddRange(await _presetRepository.GetBuiltInProfilesAsync(cancellationToken).ConfigureAwait(false));
        profiles.AddRange(await _settingsStore.GetProfilesAsync(cancellationToken).ConfigureAwait(false));
        await RunOnUiAsync(() =>
        {
            _view.SetAvailableProfiles(profiles);
            return Task.CompletedTask;
        }).ConfigureAwait(false);

        var lastFolder = await _settingsStore.GetLastOutputDirectoryAsync(cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(lastFolder))
        {
            await RunOnUiAsync(() =>
            {
                _view.ShowInfo($"Last output directory: {lastFolder}");
                return Task.CompletedTask;
            }).ConfigureAwait(false);
        }

        await RefreshQueueSnapshotAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task RefreshQueueSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await _queueService.SnapshotAsync().ConfigureAwait(false);
        var models = snapshot.Select(item => new QueueItemViewModel(item.Id, item.Request.InputPath, item.Request.OutputDirectory, "Queued")).ToList();
        await RunOnUiAsync(() =>
        {
            _view.DisplayQueueItems(models);
            return Task.CompletedTask;
        }).ConfigureAwait(false);
    }

    private async Task StartConversionAsync(CancellationToken cancellationToken)
    {
        if (_view.SelectedProfile is null)
        {
            await RunOnUiAsync(() =>
            {
                _view.ShowError("Please select a preset");
                return Task.CompletedTask;
            }).ConfigureAwait(false);
            return;
        }

        if (string.IsNullOrWhiteSpace(_view.OutputDirectory))
        {
            await RunOnUiAsync(() =>
            {
                _view.ShowError("Output directory is required");
                return Task.CompletedTask;
            }).ConfigureAwait(false);
            return;
        }

        var files = _view.SelectedInputFiles;
        if (files.Count == 0)
        {
            await RunOnUiAsync(() =>
            {
                _view.ShowError("Please add files to process");
                return Task.CompletedTask;
            }).ConfigureAwait(false);
            return;
        }

        await _settingsStore.SetLastOutputDirectoryAsync(_view.OutputDirectory!, cancellationToken).ConfigureAwait(false);
        foreach (var file in files)
        {
            var request = new ConversionRequest(file, _view.OutputDirectory!, _view.SelectedProfile, new Dictionary<string, string>(), _lifetimeCts.Token);
            await _queueService.EnqueueAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task OnItemQueuedAsync(QueueItem item)
    {
        await RunOnUiAsync(() =>
        {
            _view.UpdateStatus(item.Id, "Queued");
            return Task.CompletedTask;
        }).ConfigureAwait(false);

        try
        {
            await using var thumbnail = await _thumbnailProvider.GetThumbnailAsync(new ThumbnailRequest(item.Request.InputPath, TimeSpan.FromSeconds(5), 320, 180), _lifetimeCts.Token).ConfigureAwait(false);
            await RunOnUiAsync(() =>
            {
                _view.DisplayThumbnail(item.Id, thumbnail);
                return Task.CompletedTask;
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Thumbnail failed for {Path}", item.Request.InputPath);
        }
    }

    private Task OnProgressAsync(QueueItem item, ConversionProgress progress)
    {
        return RunOnUiAsync(() =>
        {
            _view.UpdateProgress(item.Id, progress);
            _view.UpdateStatus(item.Id, progress.Stage);
            return Task.CompletedTask;
        });
    }

    private async Task OnCompletedAsync(QueueItem item, ConversionResult result)
    {
        switch (result)
        {
            case ConversionResult.Success success:
                await RunOnUiAsync(() =>
                {
                    _view.UpdateStatus(item.Id, "Completed");
                    return Task.CompletedTask;
                }).ConfigureAwait(false);
                await _notifications.ShowSuccessAsync("Conversion complete", success.OutputPath, _lifetimeCts.Token).ConfigureAwait(false);
                break;
            case ConversionResult.Failure failure:
                await RunOnUiAsync(() =>
                {
                    _view.UpdateStatus(item.Id, "Failed");
                    _view.ShowError(failure.Reason);
                    return Task.CompletedTask;
                }).ConfigureAwait(false);
                await _notifications.ShowErrorAsync("Conversion failed", failure.Reason, _lifetimeCts.Token).ConfigureAwait(false);
                break;
        }
    }

    private Task CancelAsync(Guid queueItemId)
    {
        return _queueService.CancelAsync(queueItemId);
    }

    private Task RemoveAsync(Guid queueItemId)
    {
        return Task.CompletedTask; // UI removes item locally after completion; queue removal handled elsewhere.
    }

    private Task RunOnUiAsync(Func<Task> action)
    {
        var tcs = new TaskCompletionSource<object?>();
        _uiContext.Post(async _ =>
        {
            try
            {
                await action().ConfigureAwait(false);
                tcs.TrySetResult(null);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        }, null);
        return tcs.Task;
    }

    public async ValueTask DisposeAsync()
    {
        _lifetimeCts.Cancel();
        await _pipeline.ConfigureAwait(false);
        _lifetimeCts.Dispose();
    }
}
