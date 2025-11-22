using System;
using System.Drawing;
using System.IO;
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

public class FileListItemTests
{
    [Fact]
    public void FileListItem_ShouldDisplayFileName()
    {
        RunSta(() =>
        {
            // Arrange
            using var temp = new TempFile();
            var themeService = CreateThemeService();

            using var item = new FileListItem(temp.Path, themeService.Object);
            var nameLabel = GetField<Label>(item, "_fileName");

            // Assert
            nameLabel.Text.Should().Be(Path.GetFileName(temp.Path));
        });
    }

    [Fact]
    public void FileListItem_ShouldRenderThumbnail()
    {
        RunSta(() =>
        {
            // Arrange
            using var temp = new TempFile();
            var themeService = CreateThemeService();
            using var item = new FileListItem(temp.Path, themeService.Object);
            var thumbnail = GetField<PictureBox>(item, "_thumbnail");
            using var image = new Bitmap(10, 10);

            // Act
            item.Thumbnail = image;

            // Assert
            thumbnail.Image.Should().BeSameAs(image);
        });
    }

    [Fact]
    public void FileListItem_ShouldHighlightSelection()
    {
        RunSta(() =>
        {
            // Arrange
            using var temp = new TempFile();
            var themeService = CreateThemeService();
            using var item = new FileListItem(temp.Path, themeService.Object);
            bool selectionChanged = false;
            item.SelectionChanged += (_, _) => selectionChanged = true;

            // Act: эмулируем пользовательский клик через свойство IsSelected
            item.IsSelected = true;

            // Assert
            selectionChanged.Should().BeTrue();
            item.IsSelected.Should().BeTrue();
        });
    }

    private static Mock<IThemeService> CreateThemeService()
    {
        var mock = new Mock<IThemeService>();
        mock.SetupGet(s => s.CurrentTheme).Returns(Theme.Light);
        mock.SetupAdd(s => s.ThemeChanged += It.IsAny<EventHandler<Theme>>());
        mock.SetupRemove(s => s.ThemeChanged -= It.IsAny<EventHandler<Theme>>());
        mock.Setup(s => s.Dispose());
        return mock;
    }

    private sealed class TempFile : IDisposable
    {
        public string Path { get; } = System.IO.Path.GetTempFileName();
        public void Dispose()
        {
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }
        }
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
