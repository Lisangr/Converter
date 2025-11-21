using Xunit;

namespace Converter.Tests.LoadTests.Performance;

public class MemoryUsageTests
{
    [Fact(Skip = "Requires memory profiling setup")]
    public void ConversionPipeline_ShouldNotExceedMemoryBudget()
    {
    }
}
