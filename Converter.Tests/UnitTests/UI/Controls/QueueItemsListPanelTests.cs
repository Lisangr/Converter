using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Converter.Application.ViewModels;
using Converter.Domain.Models;
using Converter.UI.Controls;
using FluentAssertions;
using Xunit;

namespace Converter.Tests.UnitTests.UI.Controls;

public class QueueItemsListPanelTests
{
    [Fact]
    public void QueueItemsListPanel_ShouldRenderItems()
    {
        RunSta(() =>
        {
            // Arrange
            var items = new List<QueueItemViewModel>
            {
                new() { Id = Guid.NewGuid(), FileName = "a.mp4", Status = ConversionStatus.Pending },
                new() { Id = Guid.NewGuid(), FileName = "b.mp4", Status = ConversionStatus.Completed }
            };

            using var panel = new QueueItemsListPanel();

            // Act
            panel.UpdateItems(items);

            // Assert
            var flow = GetField<FlowLayoutPanel>(panel, "_itemsFlowPanel");
            flow.Controls.OfType<QueueItemControl>().Count().Should().Be(2);
        });
    }

    [Fact]
    public void QueueItemsListPanel_ShouldForwardItemEvents()
    {
        RunSta(() =>
        {
            // Arrange
            var item = new QueueItemViewModel
            {
                Id = Guid.NewGuid(),
                FileName = "item.mp4",
                Status = ConversionStatus.Pending
            };

            using var panel = new QueueItemsListPanel();
            panel.UpdateItems(new[] { item });
            var control = GetField<FlowLayoutPanel>(panel, "_itemsFlowPanel").Controls.OfType<QueueItemControl>().Single();
            Guid? movedId = null;
            panel.MoveUpClicked += (_, id) => movedId = id;

            // Act
            var moveUp = GetField<Button>(control, "_btnMoveUp");
            moveUp.PerformClick();

            // Assert
            movedId.Should().Be(item.Id);
        });
    }

    [Fact]
    public void QueueItemsListPanel_ShouldUpdateExistingControls()
    {
        RunSta(() =>
        {
            // Arrange
            var item = new QueueItemViewModel
            {
                Id = Guid.NewGuid(),
                FileName = "item.mp4",
                Status = ConversionStatus.Pending,
                Progress = 10
            };

            using var panel = new QueueItemsListPanel();
            panel.UpdateItems(new[] { item });
            var control = GetField<FlowLayoutPanel>(panel, "_itemsFlowPanel").Controls.OfType<QueueItemControl>().Single();
            var progressBar = GetField<ProgressBar>(control, "_progressBar");

            // Act
            item.Progress = 70;
            panel.UpdateItems(new[] { item });

            // Assert
            progressBar.Value.Should().Be(70);
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
