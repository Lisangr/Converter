using Xunit;

namespace Converter.Tests.UnitTests.ErrorHandling;

public class FileSystemErrorTests
{
    [Fact(Skip = "Requires filesystem error handling implementation")]
    public void MissingDirectory_ShouldBeReported()
    {
    }

    [Fact(Skip = "Requires filesystem error handling implementation")]
    public void UnauthorizedAccess_ShouldBeHandled()
    {
    }
}
