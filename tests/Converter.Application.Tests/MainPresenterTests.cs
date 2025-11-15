using Converter.Application.Interfaces;
using Converter.Application.Presenters;
using Converter.Domain.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Converter.Application.Tests;

public class MainPresenterTests
{
    [Fact]
    public async Task StartConversion_ValidatesAndEnqueues()
    {
        var view = new FakeView();
        var queue = new FakeQueueService();
        var notifications = new FakeNotificationGateway();
        var thumbnail = new FakeThumbnailProvider();
        var settings = new InMemorySettingsStore();
        var presets = new FakePresetRepository();
        var presenter = new MainPresenter(view, queue, notifications, thumbnail, settings, presets, NullLogger<MainPresenter>.Instance);

        var profile = presets.Profile;
        view.Profile = profile;
        view.OutputDir = "C:/exports";
        view.InputFiles.Add("video.mp4");

        view.RaiseStartRequested();

        var request = await queue.EnqueueSignal.Task.WaitAsync(TimeSpan.FromSeconds(1));
        Assert.Equal("video.mp4", request.InputPath);
        await presenter.DisposeAsync();
    }

    private sealed class FakeView : IMainView
    {
        public List<string> InputFiles { get; } = new();
        public string? OutputDir { get; set; }
        public ConversionProfile? Profile { get; set; }

        public event EventHandler? ViewLoaded;
        public event EventHandler? AddFilesRequested;
        public event EventHandler? StartConversionRequested;
        public event EventHandler<Guid>? CancelConversionRequested;
        public event EventHandler<Guid>? RemoveItemRequested;

        public IReadOnlyList<string> SelectedInputFiles => InputFiles;
        public string? OutputDirectory => OutputDir;
        public ConversionProfile? SelectedProfile => Profile;

        public void SetAvailableProfiles(IReadOnlyList<ConversionProfile> profiles)
        {
        }

        public void DisplayQueueItems(IReadOnlyList<Application.ViewModels.QueueItemViewModel> items)
        {
        }

        public void UpdateProgress(Guid queueItemId, ConversionProgress progress)
        {
        }

        public void UpdateStatus(Guid queueItemId, string statusText)
        {
        }

        public void DisplayThumbnail(Guid queueItemId, Stream thumbnailStream)
        {
        }

        public void SetBusy(bool isBusy)
        {
        }

        public void ShowError(string message)
        {
        }

        public void ShowInfo(string message)
        {
        }

        public void RaiseStartRequested() => StartConversionRequested?.Invoke(this, EventArgs.Empty);
    }

    private sealed class FakeQueueService : IQueueService
    {
        public TaskCompletionSource<ConversionRequest> EnqueueSignal { get; } = new();
        public event EventHandler<QueueItem>? ItemQueued;
        public event EventHandler<(QueueItem Item, ConversionProgress Progress)>? ProgressChanged;
        public event EventHandler<(QueueItem Item, ConversionResult Result)>? ItemCompleted;

        public Task EnqueueAsync(ConversionRequest request, CancellationToken cancellationToken)
        {
            EnqueueSignal.TrySetResult(request);
            return Task.CompletedTask;
        }

        public Task CancelAsync(Guid queueItemId) => Task.CompletedTask;
        public Task<IReadOnlyList<QueueItem>> SnapshotAsync() => Task.FromResult<IReadOnlyList<QueueItem>>(Array.Empty<QueueItem>());
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class FakeNotificationGateway : INotificationGateway
    {
        public Task ShowSuccessAsync(string title, string message, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task ShowWarningAsync(string title, string message, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task ShowErrorAsync(string title, string message, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeThumbnailProvider : IThumbnailProvider
    {
        public Task<Stream> GetThumbnailAsync(ThumbnailRequest request, CancellationToken cancellationToken)
            => Task.FromResult<Stream>(new MemoryStream(new byte[10]));

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class InMemorySettingsStore : ISettingsStore
    {
        public string? Directory;
        public Task<string?> GetLastOutputDirectoryAsync(CancellationToken cancellationToken) => Task.FromResult(Directory);
        public Task SetLastOutputDirectoryAsync(string path, CancellationToken cancellationToken)
        {
            Directory = path;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ConversionProfile>> GetProfilesAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<ConversionProfile>>(Array.Empty<ConversionProfile>());

        public Task SaveProfileAsync(ConversionProfile profile, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakePresetRepository : IPresetRepository
    {
        public ConversionProfile Profile { get; } = new("Default", "mp4", "libx264", "aac", 2000, 128, new Dictionary<string, string>());
        public Task<IReadOnlyList<ConversionProfile>> GetBuiltInProfilesAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<ConversionProfile>>(new[] { Profile });
    }
}
