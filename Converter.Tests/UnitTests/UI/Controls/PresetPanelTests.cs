using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Converter.Application.Models;
using Converter.Application.Services;
using Converter.UI.Controls;
using FluentAssertions;
using Moq;
using Xunit;

namespace Converter.Tests.UnitTests.UI.Controls;

public class PresetPanelTests
{
    [Fact]
    public void PresetPanel_ShouldGroupPresets()
    {
        RunSta(() =>
        {
            // Arrange
            var presets = new List<PresetProfile>
            {
                new() { Id = "1", Name = "A", Category = "Video" },
                new() { Id = "2", Name = "B", Category = "Audio" }
            };

            var presetServiceMock = new Mock<IPresetService>();
            using var panel = new PresetPanel(presetServiceMock.Object);

            // Act
            panel.LoadPresets(presets);

            // Assert
            var root = GetField<FlowLayoutPanel>(panel, "_root");
            root.Controls.OfType<GroupBox>().Select(g => g.Text).Should().BeEquivalentTo("Audio", "Video");
        });
    }

    [Fact]
    public void PresetPanel_ShouldFallbackCategory()
    {
        RunSta(() =>
        {
            // Arrange
            var presets = new List<PresetProfile>
            {
                new() { Id = "1", Name = "A", Category = null }
            };

            var presetServiceMock = new Mock<IPresetService>();
            using var panel = new PresetPanel(presetServiceMock.Object);

            // Act
            panel.LoadPresets(presets);

            // Assert
            var root = GetField<FlowLayoutPanel>(panel, "_root");
            root.Controls.OfType<GroupBox>().Single().Text.Should().Be("Прочее");
        });
    }

    [Fact]
    public void PresetPanel_ShouldUpdateSelection()
    {
        RunSta(() =>
        {
            // Arrange
            var preset = new PresetProfile { Id = "preset-1", Name = "Test" };
            var presetServiceMock = new Mock<IPresetService>();
            using var panel = new PresetPanel(presetServiceMock.Object);
            panel.LoadPresets(new[] { preset });
            var group = GetField<FlowLayoutPanel>(panel, "_root").Controls.OfType<GroupBox>().Single();
            var button = group.Controls.OfType<FlowLayoutPanel>().Single().Controls.OfType<PresetButton>().Single();

            // Act
            panel.Highlight(preset.Id!);

            // Assert
            button.BackColor.Should().NotBe(Color.White);
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
