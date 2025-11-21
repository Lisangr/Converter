using System;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Converter.Application.Models;
using Converter.UI.Dialogs;
using FluentAssertions;
using Xunit;

namespace Converter.Tests.UnitTests.UI.Dialogs;

public class StatisticsDialogTests
{
    [Fact]
    public void StatisticsDialog_ShouldDisplaySummary()
    {
        RunSta(() =>
        {
            // Arrange
            var stats = new QueueStatistics
            {
                TotalItems = 10,
                CompletedItems = 8,
                PendingItems = 1,
                ProcessingItems = 1,
                FailedItems = 1,
                TotalInputSize = 2048,
                TotalOutputSize = 1024,
                TotalProcessingTime = TimeSpan.FromMinutes(15),
                AverageSpeed = 2.5
            };

            using var dialog = new StatisticsDialog(stats);
            var labels = dialog.Controls.OfType<TableLayoutPanel>().Single().Controls.OfType<Label>().ToList();

            // Assert
            labels.Should().Contain(l => l.Text.Contains("10"));
            labels.Should().Contain(l => l.Text.Contains("8 (80") || l.Text.Contains("80%"));
            labels.Should().Contain(l => l.Text.Contains("1") && l.Text.Contains("Очереди"));
        });
    }

    [Fact]
    public void StatisticsDialog_ShouldSupportExport()
    {
        RunSta(() =>
        {
            // Arrange
            var stats = new QueueStatistics
            {
                TotalItems = 2,
                CompletedItems = 1,
                TotalInputSize = 4096,
                TotalOutputSize = 1024,
                TotalProcessingTime = TimeSpan.FromMinutes(1),
                AverageSpeed = 4.2
            };

            using var dialog = new StatisticsDialog(stats);
            var labels = dialog.Controls.OfType<TableLayoutPanel>().Single().Controls.OfType<Label>().ToList();

            // Assert
            labels.Should().Contain(l => l.Text.Contains("4 KB"));
            labels.Should().Contain(l => l.Text.Contains("3 KB"));
            labels.Should().Contain(l => l.Text.Contains("4.2 MB/сек"));
        });
    }

    [Fact]
    public void StatisticsDialog_ShouldRefreshOnDataChange()
    {
        RunSta(() =>
        {
            // Arrange
            var stats = new QueueStatistics
            {
                TotalItems = 1,
                CompletedItems = 1,
                TotalInputSize = 1024,
                TotalOutputSize = 512,
                TotalProcessingTime = TimeSpan.FromSeconds(30),
                AverageSpeed = 1.1
            };

            using var dialog = new StatisticsDialog(stats);

            // Act
            stats.CompletedItems = 2;
            stats.TotalItems = 2;
            stats.TotalOutputSize = 256;

            var refreshedDialog = new StatisticsDialog(stats);
            var labels = refreshedDialog.Controls.OfType<TableLayoutPanel>().Single().Controls.OfType<Label>().ToList();

            // Assert
            labels.Should().Contain(l => l.Text.Contains("2 (100%"));
            labels.Should().Contain(l => l.Text.Contains("256"));

            refreshedDialog.Dispose();
        });
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
            IsBackground = true
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
