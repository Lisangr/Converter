using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Converter.Application.Services;
using Converter.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Converter.Tests.UnitTests.Services
{
    public class ProfileProviderTests : IDisposable
    {
        private readonly Mock<ILogger<ProfileProvider>> _mockLogger;
        private readonly ProfileProvider _provider;
        private readonly string _profilesDir;

        public ProfileProviderTests()
        {
            _mockLogger = new Mock<ILogger<ProfileProvider>>();
            _profilesDir = Path.Combine(Directory.GetCurrentDirectory(), "Profiles");

            // Чистим директорию профилей перед тестами, чтобы избежать влияния существующих файлов
            if (Directory.Exists(_profilesDir))
            {
                Directory.Delete(_profilesDir, true);
            }

            _provider = new ProfileProvider(_mockLogger.Object);
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(() => new ProfileProvider(null!));
        }

        [Fact]
        public async Task GetAllProfilesAsync_ShouldReturnAtLeastDefaultProfile()
        {
            var profiles = await _provider.GetAllProfilesAsync();

            profiles.Should().NotBeNull();
            profiles.Should().NotBeEmpty();
        }

        [Fact]
        public async Task GetDefaultProfileAsync_ShouldReturnDefaultProfile()
        {
            var defaultProfile = await _provider.GetDefaultProfileAsync();

            defaultProfile.Should().NotBeNull();
            defaultProfile.Name.Should().Be("Default");
        }

        [Fact]
        public async Task SaveProfileAsync_ShouldPersistProfileToDisk()
        {
            var profile = new ConversionProfile
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = "Test Profile",
                Category = "Tests",
                Format = "mp4"
            };

            await _provider.SaveProfileAsync(profile);

            var all = await _provider.GetAllProfilesAsync();
            all.Should().Contain(p => p.Id == profile.Id && p.Name == "Test Profile");
        }

        [Fact]
        public async Task GetProfileByIdAsync_WithUnknownId_ShouldReturnNull()
        {
            var profile = await _provider.GetProfileByIdAsync("unknown-id");
            profile.Should().BeNull();
        }

        [Fact]
        public async Task SetDefaultProfileAsync_WithInvalidId_ShouldThrow()
        {
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _provider.SetDefaultProfileAsync("invalid-id"));
        }

        [Fact]
        public async Task SetDefaultProfileAsync_WithValidId_ShouldChangeDefault()
        {
            var profile = new ConversionProfile
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = "New Default",
                Category = "Tests",
                Format = "mp4"
            };

            await _provider.SaveProfileAsync(profile);
            await _provider.SetDefaultProfileAsync(profile.Id);

            var defaultProfile = await _provider.GetDefaultProfileAsync();
            defaultProfile.Id.Should().Be(profile.Id);
        }

        [Fact]
        public async Task DeleteProfileAsync_ShouldRemoveProfile()
        {
            var profile = new ConversionProfile
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = "To Delete",
                Category = "Tests",
                Format = "mp4"
            };

            await _provider.SaveProfileAsync(profile);
            await _provider.DeleteProfileAsync(profile.Id);

            var all = await _provider.GetAllProfilesAsync();
            all.Should().NotContain(p => p.Id == profile.Id);
        }

        public void Dispose()
        {
            if (Directory.Exists(_profilesDir))
            {
                Directory.Delete(_profilesDir, true);
            }
        }
    }
}
