using System;
using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Converter.Application.ViewModels;
using Converter.Domain.Models;
using Converter.UI.Controls;
using FluentAssertions;
using Xunit;

namespace Converter.Tests.UnitTests.UI.Controls;

public class QueueItemControlTests
{
    [Fact]
    public void QueueItemControl_ShouldDisplayStatus()
    {
        RunSta(() =>
        {
            // Arrange
            var vm = new QueueItemViewModel
            {
                Id = Guid.NewGuid(),
                FileName = "video.mp4",
                Status = ConversionStatus.Completed,
                Progress = 100
            };

            using var control = new QueueItemControl(vm);
            var statusLabel = GetField<Label>(control, "_status");
            var indicator = GetField<Panel>(control, "_statusIndicator");

            // Act
            vm.Status = ConversionStatus.Failed;

            // Assert
            statusLabel.Text.Should().Be("Ошибка");
            indicator.BackColor.Should().Be(Color.Red);
        });
    }

    [Fact]
    public void QueueItemControl_ShouldShowProgress()
    {
        RunSta(() =>
        {
            // Arrange
            var vm = new QueueItemViewModel
            {
                Id = Guid.NewGuid(),
                FileName = "video.mp4",
                Status = ConversionStatus.Pending,
                Progress = 0
            };

            using var control = new QueueItemControl(vm);
            var progressBar = GetField<ProgressBar>(control, "_progressBar");

            // Act
            vm.Status = ConversionStatus.Processing;
            vm.Progress = 45;

            // Assert
            progressBar.Value.Should().Be(45);
            GetField<Label>(control, "_eta").Text.Should().Contain("ETA");
        });
    }

    [Fact]
    public void QueueItemControl_ShouldAllowRemoval()
    {
        RunSta(() =>
        {
            // Arrange
            var vm = new QueueItemViewModel
            {
                Id = Guid.NewGuid(),
                FileName = "remove.me",
                Status = ConversionStatus.Pending
            };

            using var control = new QueueItemControl(vm);
            var cancelButton = GetField<Button>(control, "_btnCancel");
            Guid? cancelledId = null;
            control.CancelClicked += (_, id) => cancelledId = id;

            // Act
            cancelButton.PerformClick();

            // Assert
            cancelledId.Should().Be(vm.Id);
        });
    }

    private static T GetField<T>(object target, string name)
    {
        return (T)(target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(target)
            ?? throw new InvalidOperationException($"Field {name} not found"));
    }

    private static void RunSta(Action action)
    {
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        })
        {
            IsBackground = true,
            Name = "sta-test-thread"
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (exception != null)
        {
            throw exception;
        }
    }
}
