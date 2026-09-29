using ImageHelperMethods;

namespace TaskAutomation.Tests.Infrastructure;

public sealed class ScreenHelperTests
{
    [Fact]
    public void GetScreenByDesktopIndex_UsesTheSameOrderingAsTheMonitorPicker()
    {
        var screens = ScreenHelper.GetScreens();
        if (screens.Length == 0)
            return;

        for (var index = 0; index < screens.Length; index++)
            Assert.Equal(screens[index].DeviceName, ScreenHelper.GetScreenByDesktopIndex(index)?.DeviceName);
    }
}
