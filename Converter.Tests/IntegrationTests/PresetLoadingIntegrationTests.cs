using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Converter.Application.Models;
using Converter.Application.Services;
using Converter.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Microsoft.Extensions.Logging.Abstractions;

namespace Converter.Tests.IntegrationTests;

public class PresetLoadingIntegrationTests
{
    [Fact]
    public async Task PresetLoading_ShouldLoadFromEmbeddedResources()
    {
        var provider = new ProfileProvider(NullLogger<ProfileProvider>.Instance);

        var profiles = await provider.GetAllProfilesAsync();
        var defaultProfile = await provider.GetDefaultProfileAsync();

        Assert.NotEmpty(profiles);
        Assert.Equal("default", defaultProfile.Id);
        Assert.Equal("Default", defaultProfile.Name);
    }

    [Fact]
    public async Task PresetLoading_ShouldLoadFromDisk()
    {
        var workingDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(workingDirectory);
        var profilesDir = Path.Combine(workingDirectory, "Profiles");
        Directory.CreateDirectory(profilesDir);

        try
        {
            var customProfile = new ConversionProfile
            {
                Id = "custom",
                Name = "Custom",
                Category = "Video",
                VideoCodec = "h265",
                AudioCodec = "aac",
                Bitrate = 2000,
                AudioBitrate = 128,
                Format = "mkv"
            };

            var json = System.Text.Json.JsonSerializer.Serialize(customProfile);
            await File.WriteAllTextAsync(Path.Combine(profilesDir, "custom.json"), json);
            await File.WriteAllTextAsync(Path.Combine(profilesDir, "default.profile"), "custom");

            var originalDirectory = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(workingDirectory);
            try
            {
                var provider = new ProfileProvider(NullLogger<ProfileProvider>.Instance);
                var profiles = await provider.GetAllProfilesAsync();
                var defaultProfile = await provider.GetDefaultProfileAsync();

                Assert.Contains(profiles, p => p.Id == "custom" && p.Name == "Custom");
                Assert.Equal("custom", defaultProfile.Id);
            }
            finally
            {
                Directory.SetCurrentDirectory(originalDirectory);
            }
        }
        finally
        {
            Directory.Delete(workingDirectory, true);
        }
    }

    [Fact]
    public async Task PresetLoading_ShouldValidateData()
    {
        var workingDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(workingDirectory);
        var profilesDir = Path.Combine(workingDirectory, "Profiles");
        Directory.CreateDirectory(profilesDir);

        try
        {
            await File.WriteAllTextAsync(Path.Combine(profilesDir, "broken.json"), "{ invalid json }");
            var originalDirectory = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(workingDirectory);
            try
            {
                var provider = new ProfileProvider(NullLogger<ProfileProvider>.Instance);
                var profiles = await provider.GetAllProfilesAsync();

                Assert.NotEmpty(profiles);
                Assert.All(profiles, p => Assert.False(string.IsNullOrWhiteSpace(p.Id)));
            }
            finally
            {
                Directory.SetCurrentDirectory(originalDirectory);
            }
        }
        finally
        {
            Directory.Delete(workingDirectory, true);
        }
    }
}