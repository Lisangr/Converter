using System;
using Converter.Application.Abstractions;
using Converter.Application.Services;
using Converter.Domain.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Converter.Tests.UnitTests.Application;

public class UiProgressReporterTests
{
    private readonly Mock<IMainView> _viewMock = new(MockBehavior.Strict);
    private readonly Mock<IUiDispatcher> _dispatcherMock = new();
    private readonly Mock<ILogger<UiProgressReporter>> _loggerMock = new();

    private UiProgressReporter CreateSut()
    {
        _dispatcherMock
            .Setup(d => d.Invoke(It.IsAny<Action>()))
            .Callback<Action>(a => a());

        return new UiProgressReporter(_viewMock.Object, _dispatcherMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void Report_ShouldUpdateTotalProgress()
    {
        const int progress = 42;
        _viewMock.Setup(v => v.UpdateTotalProgress(progress));

        var sut = CreateSut();

        sut.Report(progress);

        _viewMock.VerifyAll();
    }

    [Fact]
    public void ReportDouble_ShouldConvertToPercentage()
    {
        const double progress = 0.37;
        _viewMock.Setup(v => v.UpdateTotalProgress(37));

        var sut = CreateSut();

        sut.Report(progress);

        _viewMock.VerifyAll();
    }

    [Fact]
    public void ReportItemProgress_ShouldUpdateItemAndStatus()
    {
        var item = new QueueItem { Id = Guid.NewGuid(), Progress = 0 };
        _viewMock.SetupSet(v => v.StatusText = "processing (25%)");

        var sut = CreateSut();

        sut.ReportItemProgress(item, 25, "processing");

        item.Progress.Should().Be(25);
        _viewMock.VerifyAll();
    }

    [Fact]
    public void ReportError_ShouldMarkItemFailedAndShowError()
    {
        var item = new QueueItem { Id = Guid.NewGuid(), Status = ConversionStatus.Pending };
        const string error = "file missing";
        _viewMock.Setup(v => v.ShowError(error));

        var sut = CreateSut();

        sut.ReportError(item, error);

        item.Status.Should().Be(ConversionStatus.Failed);
        item.ErrorMessage.Should().Be(error);
        _viewMock.VerifyAll();
    }
}
