using System.Windows;

namespace TaskAutomation.Tests.DesktopAutomationApp;

/// <summary>Give off-screen render hosts a viewport independent of the runner's monitor.</summary>
internal static class RenderWindowSizing
{
    public static void Resize(Window window, double width, double height)
    {
        // WPF otherwise limits top-level windows to the native desktop tracking size.
        // Minimum dimensions take precedence over that limit, without changing the
        // desktop resolution or bypassing the application's responsive layout rules.
        window.MinWidth = width;
        window.MinHeight = height;
        window.Width = width;
        window.Height = height;
    }
}
