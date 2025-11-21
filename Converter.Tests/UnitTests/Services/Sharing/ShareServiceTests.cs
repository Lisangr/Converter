using Xunit;
using Converter.Services;
using Converter.Domain.Models;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Converter.Tests.UnitTests.Services.Sharing;

public class ShareServiceTests
{
    [Fact]
    public void GenerateReport_ShouldIncludeConversionStats()
    {
        var items = new List<QueueItem>
        {
            new() { Status = ConversionStatus.Completed, FileSizeBytes = 2000, OutputFileSizeBytes = 1000, ConversionDuration = TimeSpan.FromSeconds(2), Settings = new ConversionSettings { VideoCodec = "h264", PresetName = "1080p" } },
            new() { Status = ConversionStatus.Completed, FileSizeBytes = 3000, OutputFileSizeBytes = 1500, ConversionDuration = TimeSpan.FromSeconds(3), Settings = new ConversionSettings { VideoCodec = "h264", PresetName = "1080p" } },
            new() { Status = ConversionStatus.Error }
        };

        using var service = new ShareService();

        var report = service.GenerateReport(items);

        report.Should().NotBeNull();
        report!.FilesConverted.Should().Be(2);
        report.TotalSpaceSaved.Should().Be(2500);
        report.TopCodecs.Should().Contain("h264");
        report.MostUsedPreset.Should().Be("1080p");
    }

    [Fact]
    public void CopyToClipboard_ShouldPlaceContent()
    {
        const string expected = "clipboard text";
        using var service = new ShareService();

        var thread = new Thread(() =>
        {
            service.CopyToClipboard(expected);
            var actual = System.Windows.Forms.Clipboard.GetText();
            actual.Should().Be(expected);
        })
        { IsBackground = true };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
    }

    [Fact]
    public async Task CreateShareImage_ShouldRenderTemplate()
    {
        using var service = new ShareService();
        var report = new Converter.Application.Models.ShareReport
        {
            Title = "Test", Subtitle = "Subtitle", Emoji = "🙂", FilesConverted = 1, ProcessingTime = TimeSpan.FromSeconds(1), TotalSpaceSaved = 1024, AccentColor = Color.Blue
        };

        var output = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".png");

        var path = await service.GenerateImageReport(report, output);

        path.Should().Be(output);
        File.Exists(output).Should().BeTrue();

        File.Delete(output);
    }
}
