using System.Text.Json;

namespace DesktopAutomation.Application.Deployment;

public static class ProfileCompatibility
{
    public const int CurrentFormat = 1;
    // Called under ProfileLease, before loading or migrating any user data.
    public static bool EnsureSupported(string directory)
    {
        var path = Path.Combine(directory, ".profile-format.json");
        if (File.Exists(path))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            return document.RootElement.ValueKind == JsonValueKind.Object &&
                document.RootElement.TryGetProperty("format", out var value) &&
                value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var format) && format == CurrentFormat;
        }
        File.WriteAllText(path + ".tmp", JsonSerializer.Serialize(new { format = CurrentFormat }));
        File.Move(path + ".tmp", path);
        return true;
    }
}
