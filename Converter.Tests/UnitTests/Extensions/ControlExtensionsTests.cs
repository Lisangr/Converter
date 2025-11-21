using System;
using System.Threading;
using System.Windows.Forms;
using Converter.Extensions;
using FluentAssertions;
using Xunit;

namespace Converter.Tests.UnitTests.Extensions;

public class ControlExtensionsTests
{
    [Fact]
    public void InvokeIfRequired_ExecutesAction()
    {
        // Arrange
        using var control = new Control();
        var executed = false;

        // Act
        control.InvokeIfRequired(() => executed = true);

        // Assert
        executed.Should().BeTrue();
    }
}
