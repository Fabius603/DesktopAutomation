namespace Common.ApplicationData;

/// <summary>Non-destructive, restartable import into user-owned data outside MSIX AppData redirection.</summary>
public static class LegacyDataImport
{
    public static bool CopyOnce(string destination, params string[] sources)
    {
        var marker = Path.Combine(destination, ".legacy-import-completed");
        if (File.Exists(marker)) return true;
        try
        {
            Directory.CreateDirectory(destination);
            foreach (var source in sources)
            {
                if (!Directory.Exists(source)) continue;
                if ((File.GetAttributes(source) & FileAttributes.ReparsePoint) != 0) continue;
                CopyDirectory(source, destination, true);
            }
            File.WriteAllText(marker, "1");
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Partial imports can be retried; originals and existing destination files are preserved.
            return false;
        }
    }

    private static void CopyDirectory(string source, string destination, bool root = false)
    {
        foreach (var entry in Directory.EnumerateFileSystemEntries(source))
        {
            var attributes = File.GetAttributes(entry);
            if ((attributes & FileAttributes.ReparsePoint) != 0) continue;
            var name = Path.GetFileName(entry);
            if (root && name == "Profiles") continue; // Other named profiles are imported only when selected.
            if (name is ".instance.lock" or ".legacy-import-completed") continue;
            var target = Path.Combine(destination, name);
            if ((attributes & FileAttributes.Directory) != 0)
            {
                Directory.CreateDirectory(target);
                CopyDirectory(entry, target);
            }
            else if (!File.Exists(target))
            {
                // Publish a complete copy atomically so interrupted imports never expose a partial file.
                var temporary = target + ".import-" + Guid.NewGuid().ToString("N");
                try { File.Copy(entry, temporary); File.Move(temporary, target); }
                finally { if (File.Exists(temporary)) File.Delete(temporary); }
            }
        }
    }
}
