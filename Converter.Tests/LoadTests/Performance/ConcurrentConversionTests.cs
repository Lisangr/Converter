using Xunit;

namespace Converter.Tests.LoadTests.Performance;

public class ConcurrentConversionTests
{
    [Fact(Skip = "Requires concurrent conversion setup")]
    public void ConcurrentConversions_ShouldScaleWithCpu()
    {
    }
}
