using System;
using System.Threading;
using System.Windows.Forms;
using Converter.Extensions;
using FluentAssertions;
using Xunit;

namespace Converter.Tests.UnitTests.Extensions;

public class ControlExtensionsTests
{
    private class TestControl : Control
    {
        public bool ShouldInvoke { get; set; }
        public bool WasInvoked { get; private set; }

        public override bool InvokeRequired => ShouldInvoke;

        // Override CreateHandle to prevent handle creation
        protected override void CreateHandle()
        {
            // Intentionally empty to prevent handle creation in tests
        }

        // Capture Invoke calls
        public override void Invoke(Delegate method, params object[] args)
        {
            WasInvoked = true;
            method.DynamicInvoke(args);
        }

        public override IAsyncResult BeginInvoke(Delegate method, params object[] args)
        {
            WasInvoked = true;
            method.DynamicInvoke(args);
            return new ManualResetEvent(true).BeginInvoke(method, args);
        }
    }

    [Fact]
    public void InvokeIfRequired_WhenInvokeNotRequired_ExecutesInline()
    {
        // Arrange
        var control = new TestControl { ShouldInvoke = false };
        var executed = false;

        // Act
        control.InvokeIfRequired(() => executed = true);

        // Assert
        executed.Should().BeTrue();
        control.WasInvoked.Should().BeFalse();
    }

    [Fact]
    public void InvokeIfRequired_WhenInvokeRequired_UsesControlInvoke()
    {
        // Arrange
        var control = new TestControl { ShouldInvoke = true };
        var executed = false;

        // Act
        control.InvokeIfRequired(() => executed = true);

        // Assert
        executed.Should().BeTrue();
        control.WasInvoked.Should().BeTrue();
    }
}
