namespace TaskAutomation.Tests.DesktopAutomationApp;

// Render hosts run in separate processes but share desktop focus and popup activation.
// Keep their interactions exclusive while leaving the other UI tests parallel.
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class DesktopRenderingCollection
{
    public const string Name = "Desktop rendering";
}
