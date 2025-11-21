using System;
using System.Threading;
using System.Windows.Forms;
using Converter.Extensions;
using FluentAssertions;
using Xunit;

namespace Converter.Tests.UnitTests.Extensions;

public class ControlExtensionsTests
{
    private class InvokeAwareControl : Control
    {
        private readonly bool _invokeRequired;
        public bool Invoked { get; private set; }

        public InvokeAwareControl(bool invokeRequired)
        {
            _invokeRequired = invokeRequired;
        }

        public override bool InvokeRequired => _invokeRequired;

        public override IAsyncResult BeginInvoke(Delegate method)
        {
            method.DynamicInvoke();
            Invoked = true;
            return new ManualResetEvent(true).BeginInvoke(method, Array.Empty<object?>());
        }

        public override void Invoke(Delegate method)
        {
            method.DynamicInvoke();
            Invoked = true;
        }
    }

    [Fact]
    public void InvokeIfRequired_WhenInvokeNotRequired_ExecutesInline()
    {
        // Arrange
        var control = new InvokeAwareControl(false);
        var executed = false;

        // Act
        control.InvokeIfRequired(() => executed = true);

        // Assert
        executed.Should().BeTrue();
        control.Invoked.Should().BeFalse();
    }

    [Fact]
    public void InvokeIfRequired_WhenInvokeRequired_UsesControlInvoke()
    {
        // Arrange
        var control = new InvokeAwareControl(true);
        var executed = false;

        // Act
        control.InvokeIfRequired(() => executed = true);

        // Assert
        executed.Should().BeTrue();
        control.Invoked.Should().BeTrue();
    }
}
