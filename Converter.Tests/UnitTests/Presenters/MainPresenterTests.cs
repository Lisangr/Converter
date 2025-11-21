using Xunit;
using Converter.Application.Presenters;
using Converter.Application.ViewModels;
using Converter.Application.Abstractions;
using Converter.Domain.Models;
using Converter.Application.Models;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Converter.Tests.UnitTests.Presenters;

public class MainPresenterTests
{
    [Fact]
    public async Task Initialize_ShouldWireUpView()
    {
        var view = new Mock<IMainView>();
        view.SetupAllProperties();
        var vm = new MainViewModel();
        var queueRepository = new Mock<IQueueRepository>();
        queueRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<QueueItem> { new() { FilePath = "a.mp4" } });
        var queueProcessor = new Mock<IQueueProcessor>();
        var profileProvider = new Mock<IProfileProvider>();
        var defaultProfile = new ConversionProfile("Default", "h264", "aac", "128k", 23) { Id = "default" };
        profileProvider.Setup(p => p.GetAllProfilesAsync()).ReturnsAsync(new List<ConversionProfile> { defaultProfile });
        profileProvider.Setup(p => p.GetDefaultProfileAsync()).ReturnsAsync(defaultProfile);
        var pathBuilder = new Mock<IOutputPathBuilder>();
        var progressReporter = new Mock<IProgressReporter>();
        var filePicker = new Mock<IFilePicker>();

        var presenter = new MainPresenter(view.Object, vm, queueRepository.Object, queueProcessor.Object, profileProvider.Object, pathBuilder.Object, progressReporter.Object, filePicker.Object, NullLogger<MainPresenter>.Instance);

        await presenter.InitializeAsync();

        view.Object.QueueItemsBinding.Should().NotBeNull();
        view.Object.SelectedPreset.Should().Be(defaultProfile);
        view.Object.AvailablePresets.Should().Contain(defaultProfile);
        vm.QueueItems.Should().ContainSingle(q => q.FilePath == "a.mp4");
    }

    [Fact]
    public async Task StartConversion_ShouldDelegateToQueueProcessor()
    {
        var view = new Mock<IMainView>();
        view.SetupAllProperties();
        var vm = new MainViewModel();
        vm.QueueItems.Add(new QueueItemViewModel { FilePath = "file.mp4" });
        var queueRepository = new Mock<IQueueRepository>();
        var queueProcessor = new Mock<IQueueProcessor>();
        queueProcessor.Setup(p => p.StartProcessingAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask).Verifiable();
        var profileProvider = new Mock<IProfileProvider>();
        profileProvider.Setup(p => p.GetAllProfilesAsync()).ReturnsAsync(new List<ConversionProfile>());
        profileProvider.Setup(p => p.GetDefaultProfileAsync()).ReturnsAsync(new ConversionProfile("d", "v", "a", "b", 1));
        var pathBuilder = new Mock<IOutputPathBuilder>();
        var progressReporter = new Mock<IProgressReporter>();
        var filePicker = new Mock<IFilePicker>();

        var presenter = new MainPresenter(view.Object, vm, queueRepository.Object, queueProcessor.Object, profileProvider.Object, pathBuilder.Object, progressReporter.Object, filePicker.Object, NullLogger<MainPresenter>.Instance);

        await presenter.InitializeAsync();
        await view.Object.StartConversionRequestedAsync.Invoke();

        queueProcessor.Verify(p => p.StartProcessingAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Presenter_ShouldHandleErrors()
    {
        var view = new Mock<IMainView>();
        view.SetupAllProperties();
        view.Setup(v => v.ShowError(It.IsAny<string>())).Verifiable();
        var vm = new MainViewModel();
        var queueRepository = new Mock<IQueueRepository>();
        queueRepository.Setup(r => r.GetAllAsync()).ThrowsAsync(new InvalidOperationException("fail"));
        var queueProcessor = new Mock<IQueueProcessor>();
        var profileProvider = new Mock<IProfileProvider>();
        profileProvider.Setup(p => p.GetAllProfilesAsync()).ReturnsAsync(new List<ConversionProfile>());
        profileProvider.Setup(p => p.GetDefaultProfileAsync()).ReturnsAsync(new ConversionProfile("d", "v", "a", "b", 1));
        var pathBuilder = new Mock<IOutputPathBuilder>();
        var progressReporter = new Mock<IProgressReporter>();
        var filePicker = new Mock<IFilePicker>();

        var presenter = new MainPresenter(view.Object, vm, queueRepository.Object, queueProcessor.Object, profileProvider.Object, pathBuilder.Object, progressReporter.Object, filePicker.Object, NullLogger<MainPresenter>.Instance);

        await presenter.Invoking(p => p.InitializeAsync()).Should().ThrowAsync<InvalidOperationException>();
        view.Verify(v => v.ShowError(It.Is<string>(s => s.Contains("fail"))), Times.Once);
    }
}
