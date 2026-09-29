using TaskAutomation.Hotkeys;
using TaskAutomation.WindowsIntegration;

namespace TaskAutomation.Tests.WindowsIntegration;

public sealed class ForceStopKeyConfigurationTests
{
    [Fact]
    public void MissingOrInvalidConfiguration_FallsBackToF10()
    {
        Assert.Equal(0x79u, ForceStopKeyConfiguration.Normalize(0));
        Assert.Equal(0x79u, ForceStopKeyConfiguration.Normalize(0x11));
        Assert.Equal(0x7Au, ForceStopKeyConfiguration.Normalize(0x7A));
    }

    [Fact]
    public void InputBlocker_AlwaysAllowsTheConfiguredForceStopKey()
    {
        var original = ForceStopKeyConfiguration.VirtualKey;
        try
        {
            ForceStopKeyConfiguration.Set(0x7A); // F11

            Assert.False(
                WindowsInputBlockController.ShouldBlockPhysicalKeyboardInput(0x7A, 0));
            Assert.True(
                WindowsInputBlockController.ShouldBlockPhysicalKeyboardInput(0x79, 0));
            Assert.False(
                WindowsInputBlockController.ShouldBlockPhysicalKeyboardInput(0x41, 0x10));
        }
        finally
        {
            ForceStopKeyConfiguration.Set(original);
        }
    }
}
