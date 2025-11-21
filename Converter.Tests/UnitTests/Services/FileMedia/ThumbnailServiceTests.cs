using Xunit;

namespace Converter.Tests.UnitTests.Services.FileMedia;

public class ThumbnailServiceTests
{
    [Fact(Skip = "Requires preview generation implementation")]
    public void GenerateThumbnail_ShouldCreateCachedImage()
    {
    }

    [Fact(Skip = "Requires caching implementation")]
    public void GetThumbnail_ShouldUseCacheWhenAvailable()
    {
    }

    [Fact(Skip = "Requires cache cleanup implementation")]
    public void ClearCache_ShouldRemoveStoredThumbnails()
    {
    }
}
