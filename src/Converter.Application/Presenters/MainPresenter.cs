using Converter.Application.Abstractions;
using Converter.Domain.Models;
using Microsoft.Extensions.Logging;

namespace Converter.Application.Presenters;

public sealed class MainPresenter : IAsyncDisposable
{
    private readonly IMainView _view;
    private readonly IQueueService _queueService;
    private readonly INotificationGateway _notificationGateway;
    private readonly ISettingsStore _settingsStore;
    private readonly IPresetRepository _presetRepository;
    private readonly IThumbnailProvider _thumbnailProvider;
    private readonly ILogger<MainPresenter> _logger;
    private readonly List<ConversionProfile> _profiles = new();
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly SynchronizationContext? _uiContext = SynchronizationContext.Current;
    private readonly EventHandler<IReadOnlyCollection<QueueItem>> _queueChangedHandler;
    private readonly EventHandler<ConversionProgress> _progressHandler;
    private readonly EventHandler<(QueueItem Item, QueueItemStatus Status)> _itemStatusHandler;
    private CancellationTokenSource? _processingCts;

    public MainPresenter(
        IMainView view,
        IQueueService queueService,
        INotificationGateway notificationGateway,
        ISettingsStore settingsStore,
        IPresetRepository presetRepository,
        IThumbnailProvider thumbnailProvider,
        ILogger<MainPresenter> logger)
    {
        _view = view;
        _queueService = queueService;
        _notificationGateway = notificationGateway;
        _settingsStore = settingsStore;
        _presetRepository = presetRepository;
        _thumbnailProvider = thumbnailProvider;
        _logger = logger;

        _view.AddFilesRequested += OnAddFilesRequested;
        _view.StartConversionRequested += OnStartConversionRequested;
        _view.CancelRequested += OnCancelRequested;
        _view.BrowseOutputFolderRequested += OnBrowseOutputFolderRequested;
        _view.ProfileChanged += OnProfileChanged;

        _queueChangedHandler = (_, items) => Dispatch(() => _view.UpdateQueue(items));
        _progressHandler = (_, progress) => Dispatch(() => _view.UpdateProgress(progress));
        _itemStatusHandler = async (_, tuple) => await HandleItemStatusChangedAsync(tuple).ConfigureAwait(false);

        _queueService.QueueChanged += _queueChangedHandler;
        _queueService.ProgressChanged += _progressHandler;
        _queueService.ItemStatusChanged += _itemStatusHandler;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_profiles.Count > 0)
            {
                return;
            }

            var stored = await _settingsStore.GetProfilesAsync(cancellationToken).ConfigureAwait(false);
            if (stored.Count == 0)
            {
                stored = await _presetRepository.LoadAsync(cancellationToken).ConfigureAwait(false);
            }

            _profiles.AddRange(stored);
            Dispatch(() => _view.BindProfiles(_profiles));
        }
        finally
        {
            _gate.Release();
        }
    }

    private async void OnAddFilesRequested(object? sender, EventArgs e)
    {
        try
        {
            var file = _view.RequestInputFile();
            if (string.IsNullOrWhiteSpace(file))
            {
                return;
            }

            var request = _view.BuildConversionRequest(file!);
            if (request is null)
            {
                _view.ShowError("Queue", "Unable to build conversion request for selected file.");
                return;
            }

            try
            {
                using var _ = await _thumbnailProvider.GetAsync(request.InputPath, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to prefetch thumbnail for {File}", request.InputPath);
            }

            await _queueService.EnqueueAsync(new[] { request }, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add files to queue");
            await _notificationGateway.ShowErrorAsync("Queue", ex.Message, CancellationToken.None).ConfigureAwait(false);
        }
    }

    private async void OnStartConversionRequested(object? sender, EventArgs e)
    {
        if (_processingCts is not null)
        {
            _view.ShowInfo("Conversion", "A conversion is already running.");
            return;
        }

        _processingCts = new CancellationTokenSource();
        _view.SetBusyState(true);
        try
        {
            await _queueService.StartAsync(_processingCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _view.ShowInfo("Conversion", "Conversion cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Conversion failed");
            await _notificationGateway.ShowErrorAsync("Conversion", ex.Message, CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            _processingCts.Dispose();
            _processingCts = null;
            _view.SetBusyState(false);
        }
    }

    private async void OnCancelRequested(object? sender, EventArgs e)
    {
        if (_processingCts is null)
        {
            return;
        }

        _processingCts.Cancel();
        await _queueService.CancelAsync().ConfigureAwait(false);
    }

    private void OnBrowseOutputFolderRequested(object? sender, EventArgs e)
    {
        _ = _view.SelectOutputFolder();
    }

    private async void OnProfileChanged(object? sender, ConversionProfile profile)
    {
        try
        {
            await _settingsStore.SaveProfilesAsync(_profiles, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist profiles");
        }
    }

    private async Task HandleItemStatusChangedAsync((QueueItem Item, QueueItemStatus Status) tuple)
    {
        var (item, status) = tuple;
        switch (status)
        {
            case QueueItemStatus.Completed:
                await _notificationGateway.ShowInfoAsync("Conversion completed", item.Request.InputPath, CancellationToken.None).ConfigureAwait(false);
                break;
            case QueueItemStatus.Failed:
                await _notificationGateway.ShowErrorAsync("Conversion failed", item.Request.InputPath, CancellationToken.None).ConfigureAwait(false);
                break;
        }
    }

    private void Dispatch(Action action)
    {
        if (_uiContext is null || SynchronizationContext.Current == _uiContext)
        {
            action();
        }
        else
        {
            _uiContext.Post(_ => action(), null);
        }
    }

    public async ValueTask DisposeAsync()
    {
        _view.AddFilesRequested -= OnAddFilesRequested;
        _view.StartConversionRequested -= OnStartConversionRequested;
        _view.CancelRequested -= OnCancelRequested;
        _view.BrowseOutputFolderRequested -= OnBrowseOutputFolderRequested;
        _view.ProfileChanged -= OnProfileChanged;

        _queueService.QueueChanged -= _queueChangedHandler;
        _queueService.ProgressChanged -= _progressHandler;
        _queueService.ItemStatusChanged -= _itemStatusHandler;

        await _queueService.DisposeAsync().ConfigureAwait(false);
        await _notificationGateway.DisposeAsync().ConfigureAwait(false);
        await _thumbnailProvider.DisposeAsync().ConfigureAwait(false);
        _gate.Dispose();
        _processingCts?.Dispose();
    }
}
