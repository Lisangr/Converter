using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Converter.Application.Models;
using Converter.UI.Dialogs;
using FluentAssertions;
using Xunit;

namespace Converter.Tests.UnitTests.UI.Dialogs;

public class ShareDialogTests
{
    [Fact]
    public void ShareDialog_ShouldRenderPreview()
    {
        RunSta(async () =>
        {
            // Arrange
            var report = CreateReport();
            using var dialog = new ShareDialog(report);
            var preview = GetField<System.Windows.Forms.PictureBox>(dialog, "_previewImage");

            // Act
            await WaitUntilAsync(() => preview.Tag is string path && File.Exists(path), TimeSpan.FromSeconds(3));

            // Assert
            preview.Tag.Should().NotBeNull();
            preview.Image.Should().NotBeNull();
        });
    }

    [Fact]
    public void ShareDialog_ShouldCopyContent()
    {
        RunSta(() =>
        {
            // Arrange
            var report = CreateReport();
            using var dialog = new ShareDialog(report);
            var twitterText = GetField<System.Windows.Forms.TextBox>(dialog, "_twitterText").Text;

            // Act
            var redditText = GetField<System.Windows.Forms.TextBox>(dialog, "_redditText").Text;

            // Assert
            twitterText.Should().Contain(report.Title);
            redditText.Should().Contain("Reddit", StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void ShareDialog_ShouldHandleTemplateSelection()
    {
        RunSta(() =>
        {
            // Arrange
            var report = CreateReport();
            using var dialog = new ShareDialog(report);
            var tabControl = GetField<System.Windows.Forms.TabControl>(dialog, "_tabControl");

            // Act
            tabControl.SelectedTab = tabControl.TabPages[1];
            var redditText = GetField<System.Windows.Forms.TextBox>(dialog, "_redditText").Text;
            tabControl.SelectedTab = tabControl.TabPages[2];
            var discordText = GetField<System.Windows.Forms.TextBox>(dialog, "_discordText").Text;

            // Assert
            tabControl.SelectedTab.Text.Should().Be("Discord");
            redditText.Should().Contain("Reddit", StringComparison.OrdinalIgnoreCase);
            discordText.Should().Contain(report.FilesConverted.ToString());
        });
    }

    private static ShareReport CreateReport()
    {
        return new ShareReport
        {
            Title = "Test Summary",
            Subtitle = "3 files processed",
            Emoji = "✅",
            FilesConverted = 3,
            ProcessingTime = TimeSpan.FromMinutes(5),
            TotalSpaceSaved = 1024 * 1024,
            TopCodecs = new[] { "h264" }.ToList(),
            GeneratedAt = DateTime.Now,
            AccentColor = Color.Aqua
        };
    }

    private static T GetField<T>(object target, string name)
    {
        return (T)(target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(target)
            ?? throw new InvalidOperationException());
    }

    private static void RunSta(Func<Task> action)
    {
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                action().GetAwaiter().GetResult();
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

    private static Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        return Task.Run(async () =>
        {
            var start = DateTime.UtcNow;
            while (!condition())
            {
                if (DateTime.UtcNow - start > timeout)
                {
                    throw new TimeoutException("Condition was not satisfied in time.");
                }

                await Task.Delay(50);
            }
        });
    }
}
