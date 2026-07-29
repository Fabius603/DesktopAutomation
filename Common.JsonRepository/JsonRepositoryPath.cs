using System.IO;

namespace Common.JsonRepository;

public static class JsonRepositoryPath
{
    public static string ForKey(string directoryPath, string key)
        => Path.Combine(directoryPath, FileNameForKey(key));

    public static string LegacyForKey(string directoryPath, string key)
        => Path.Combine(directoryPath, NamePolicy.Sanitize(key) + ".json");

    public static string FileNameForKey(string key)
    {
        var trimmed = key?.Trim() ?? string.Empty;
        return Guid.TryParse(trimmed, out var id)
            ? id.ToString("D") + ".json"
            : NamePolicy.Sanitize(trimmed) + ".json";
    }
}
