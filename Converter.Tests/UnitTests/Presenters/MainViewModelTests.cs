using Converter.Application.ViewModels;
using FluentAssertions;
using Xunit;

namespace Converter.Tests.UnitTests.Presenters;

public class MainViewModelTests
{
    [Fact]
    public void Constructor_ShouldInitializeCollections()
    {
        // Act
        var vm = new MainViewModel();

        // Assert
        vm.QueueItems.Should().NotBeNull();
        vm.Presets.Should().NotBeNull();
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        // Arrange
        var vm = new MainViewModel();

        // Act
        vm.FfmpegPath = "ffmpeg.exe";
        vm.OutputFolder = "C:/output";
        vm.IsBusy = true;
        vm.StatusText = "Working";

        // Assert
        vm.FfmpegPath.Should().Be("ffmpeg.exe");
        vm.OutputFolder.Should().Be("C:/output");
        vm.IsBusy.Should().BeTrue();
        vm.StatusText.Should().Be("Working");
    }
}
