using System.Threading.Tasks;
using Converter.Application.Abstractions;
using Converter.Application.Presenters;
using Converter.Domain.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Converter.Application.Tests;

public sealed class MainPresenterTests
{
    [Fact]
    public async Task InitializeAsync_BindsProfilesFromPresetsWhenStoreEmpty()
    {
        var view = new FakeView();
        var queue = new Mock<IQueueService>();
        var notification = new Mock<INotificationGateway>();
        var settings = new Mock<ISettingsStore>();
        settings.Setup(s => s.GetProfilesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<ConversionProfile>());
        var presetProfiles = new[] { new ConversionProfile("Default", "mp4", "libx264", "aac") };
        var presets = new Mock<IPresetRepository>();
        presets.Setup(p => p.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(presetProfiles);
        var thumbnails = new Mock<IThumbnailProvider>();
        thumbnails.Setup(t => t.DisposeAsync()).Returns(ValueTask.CompletedTask);
        queue.Setup(q => q.DisposeAsync()).Returns(ValueTask.CompletedTask);
        notification.Setup(n => n.DisposeAsync()).Returns(ValueTask.CompletedTask);

        var presenter = new MainPresenter(
            view,
            queue.Object,
            notification.Object,
            settings.Object,
            presets.Object,
            thumbnails.Object,
            NullLogger<MainPresenter>.Instance);

        await presenter.InitializeAsync(CancellationToken.None);

        Assert.Single(view.BoundProfiles);
    }

    [Fact]
    public async Task AddFilesRequested_EnqueuesRequest()
    {
        var view = new FakeView
        {
            InputFile = "video.mkv",
            RequestBuilder = path => new ConversionRequest(path, "out", new ConversionProfile("p", "mp4", "libx264", "aac"))
        };
        var queue = new Mock<IQueueService>();
        var enqueueTcs = new TaskCompletionSource();
        queue.Setup(q => q.EnqueueAsync(It.IsAny<IEnumerable<ConversionRequest>>(), It.IsAny<CancellationToken>()))
            .Callback(() => enqueueTcs.TrySetResult())
            .Returns(Task.CompletedTask);
        queue.Setup(q => q.DisposeAsync()).Returns(ValueTask.CompletedTask);
        var notification = new Mock<INotificationGateway>();
        notification.Setup(n => n.DisposeAsync()).Returns(ValueTask.CompletedTask);
        var settings = new Mock<ISettingsStore>();
        settings.Setup(s => s.GetProfilesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<ConversionProfile>());
        var presets = new Mock<IPresetRepository>();
        presets.Setup(p => p.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<ConversionProfile>());
        var thumbnails = new Mock<IThumbnailProvider>();
        thumbnails.Setup(t => t.DisposeAsync()).Returns(ValueTask.CompletedTask);

        var presenter = new MainPresenter(
            view,
            queue.Object,
            notification.Object,
            settings.Object,
            presets.Object,
            thumbnails.Object,
            NullLogger<MainPresenter>.Instance);

        view.TriggerAddFiles();

        await enqueueTcs.Task.WaitAsync(TimeSpan.FromSeconds(1));
        queue.Verify(q => q.EnqueueAsync(It.Is<IEnumerable<ConversionRequest>>(r => r.Any()), It.IsAny<CancellationToken>()), Times.Once);
    }

    private sealed class FakeView : IMainView
    {
        public event EventHandler? AddFilesRequested;
        public event EventHandler? StartConversionRequested;
        public event EventHandler? CancelRequested;
        public event EventHandler? BrowseOutputFolderRequested;
        public event EventHandler<ConversionProfile>? ProfileChanged;

        public List<ConversionProfile> BoundProfiles { get; } = new();
        public string? InputFile { get; set; }
        public Func<string, ConversionRequest?>? RequestBuilder { get; set; }

        public ConversionRequest? BuildConversionRequest(string inputFile) => RequestBuilder?.Invoke(inputFile);
        public string? RequestInputFile() => InputFile;
        public string? SelectOutputFolder() => null;
        public void BindProfiles(IEnumerable<ConversionProfile> profiles) => BoundProfiles.AddRange(profiles);
        public void UpdateQueue(IEnumerable<QueueItem> items) { }
        public void UpdateProgress(ConversionProgress progress) { }
        public void SetBusyState(bool isBusy) { }
        public void ShowError(string title, string message) { }
        public void ShowInfo(string title, string message) { }

        public void TriggerAddFiles() => AddFilesRequested?.Invoke(this, EventArgs.Empty);
    }
}
