using Xunit;

namespace Converter.Tests.UnitTests.Infrastructure;

public class JsonQueueStoreTests
{
    [Fact(Skip = "Requires queue store persistence implementation")]
    public void SaveQueue_ShouldPersistToDisk()
    {
    }

    [Fact(Skip = "Requires queue store persistence implementation")]
    public void LoadQueue_ShouldRestoreItems()
    {
    }

    [Fact(Skip = "Requires queue store persistence implementation")]
    public void LoadQueue_ShouldHandleCorruptedFile()
    {
    }
}
