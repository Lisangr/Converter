using System;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Converter.UI.Controls;
using FluentAssertions;
using Xunit;

namespace Converter.Tests.UnitTests.UI.Controls;

public class PresetButtonTests
{
    [Fact]
    public void PresetButton_ShouldExposeTitleAndIcon()
    {
        RunSta(() =>
        {
            // Arrange
            using var button = new PresetButton
            {
                Title = "HD",
                IconText = "🎬",
                Description = "1080p"
            };

            // Act & Assert
            button.Title.Should().Be("HD");
            button.IconText.Should().Be("🎬");
            button.Description.Should().Be("1080p");
        });
    }

    [Fact]
    public void PresetButton_ShouldRaiseClick()
    {
        RunSta(() =>
        {
            // Arrange
            using var button = new PresetButton();
            bool clicked = false;
            button.Click += (_, _) => clicked = true;

            // Act
            button.PerformClick();

            // Assert
            clicked.Should().BeTrue();
        });
    }

    [Fact]
    public void PresetButton_ShouldToggleHoverState()
    {
        RunSta(() =>
        {
            // Arrange
            using var button = new PresetButton();
            var hoverField = button.GetType().GetField("_hover", BindingFlags.Instance | BindingFlags.NonPublic);

            // Act
            Invoke(button, "OnMouseEnter");
            var onHover = (bool)hoverField!.GetValue(button)!;
            Invoke(button, "OnMouseLeave");
            var offHover = (bool)hoverField.GetValue(button)!;

            // Assert
            onHover.Should().BeTrue();
            offHover.Should().BeFalse();
        });
    }

    private static void Invoke(Control control, string method)
    {
        control.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(control, new object[] { EventArgs.Empty });
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
