namespace TaskAutomation.Tests.TestDoubles;

internal sealed class TemporaryDirectory : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
        "DesktopAutomation.Tests", Guid.NewGuid().ToString("N"));
    public TemporaryDirectory() => Directory.CreateDirectory(Path);
    public void Dispose()
    {
        try { if (Directory.Exists(Path)) Directory.Delete(Path, recursive: true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
