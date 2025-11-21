using System;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Converter.Application.Models;
using Converter.Domain.Models;
using Converter.UI.Controls;
using FluentAssertions;
using Xunit;

namespace Converter.Tests.UnitTests.UI.Controls;

public class QueueControlPanelTests
{
    [Fact]
    public void QueueControlPanel_ShouldRaiseControlEvents()
    {
        RunSta(() =>
        {
            // Arrange
            using var panel = new QueueControlPanel();
            var startButton = GetField<Button>(panel, "_btnStart");
            var pauseButton = GetField<Button>(panel, "_btnPause");
            var clearButton = GetField<Button>(panel, "_btnClearCompleted");

            bool startRaised = false, pauseRaised = false, clearRaised = false;
            panel.StartClicked += (_, _) => startRaised = true;
            panel.PauseClicked += (_, _) => pauseRaised = true;
            panel.ClearCompletedClicked += (_, _) => clearRaised = true;

            // Act
            startButton.PerformClick();
            pauseButton.PerformClick();
            clearButton.PerformClick();

            // Assert
            startRaised.Should().BeTrue();
            pauseRaised.Should().BeTrue();
            clearRaised.Should().BeTrue();
        });
    }

    [Fact]
    public void QueueControlPanel_ShouldFilterAndUpdateStatistics()
    {
        RunSta(() =>
        {
            // Arrange
            using var panel = new QueueControlPanel();
            var filter = GetField<ComboBox>(panel, "_filterStatus");
            var statsLabel = GetField<Label>(panel, "_lblStats");
            ConversionStatus? status = null;
            panel.FilterChanged += (_, s) => status = s;

            // Act
            filter.SelectedIndex = 2; // Processing
            var stats = new QueueStatistics
            {
                TotalItems = 4,
                CompletedItems = 2,
                FailedItems = 1
            };
            panel.UpdateStatistics(stats);

            // Assert
            status.Should().Be(ConversionStatus.Processing);
            statsLabel.Text.Should().Contain("2/4 завершено");
            statsLabel.Text.Should().Contain("50%");
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
