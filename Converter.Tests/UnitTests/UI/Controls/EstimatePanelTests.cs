using System;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Converter.Application.Abstractions;
using Converter.Application.Models;
using Converter.UI.Controls;
using FluentAssertions;
using Moq;
using Xunit;

namespace Converter.Tests.UnitTests.UI.Controls;

public class EstimatePanelTests
{
    [Fact]
    public void EstimatePanel_ShouldDisplayCalculatingState()
    {
        RunSta(() =>
        {
            // Arrange
            var theme = Theme.Light;
            var themeService = CreateThemeService(theme);

            using var panel = new EstimatePanel(themeService.Object);
            var inputLabel = GetField<Label>(panel, "lblInput");
            var outputLabel = GetField<Label>(panel, "lblOutput");
            var savedLabel = GetField<Label>(panel, "lblSaved");
            var timeLabel = GetField<Label>(panel, "lblTime");

            // Act
            panel.ShowCalculating();

            // Assert
            inputLabel.Text.Should().Be("Текущий размер: расчет...");
            outputLabel.Text.Should().Be("После конвертации: ...");
            savedLabel.Text.Should().Be("Экономия: ...");
            timeLabel.Text.Should().Be("⏱️ Примерное время: ...");
            timeLabel.ForeColor.Should().Be(theme["TextPrimary"]);
        });
    }

    [Fact]
    public void EstimatePanel_ShouldRenderEstimateAndWarnings()
    {
        RunSta(() =>
        {
            // Arrange
            var theme = Theme.Light;
            var themeService = CreateThemeService(theme);
            using var panel = new EstimatePanel(themeService.Object)
            {
                WarningThreshold = 5
            };

            var estimate = new ConversionEstimate
            {
                InputFileSizeBytes = 1024 * 1024,
                EstimatedOutputSizeBytes = 512 * 1024,
                SpaceSavedBytes = 512 * 1024,
                EstimatedDuration = TimeSpan.FromMinutes(10),
                CompressionRatio = 0.5
            };

            var savedLabel = GetField<Label>(panel, "lblSaved");
            var timeLabel = GetField<Label>(panel, "lblTime");
            var progressBar = GetField<ProgressBar>(panel, "pbPerf");

            // Act
            panel.UpdateEstimate(estimate);

            // Assert
            savedLabel.Text.Should().Contain(estimate.SpaceSavedFormatted);
            timeLabel.Text.Should().Contain(estimate.DurationFormatted);
            timeLabel.ForeColor.Should().Be(theme["Warning"]);
            progressBar.Value.Should().BeGreaterThan(0);
        });
    }

    private static Mock<IThemeService> CreateThemeService(Theme theme)
    {
        var mock = new Mock<IThemeService>();
        mock.SetupGet(t => t.CurrentTheme).Returns(theme);
        mock.SetupAdd(t => t.ThemeChanged += It.IsAny<EventHandler<Theme>>());
        mock.SetupRemove(t => t.ThemeChanged -= It.IsAny<EventHandler<Theme>>());
        mock.Setup(t => t.Dispose());
        return mock;
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
