using Microsoft.Win32;
using System.IO;

namespace TaskAutomation.Steps;

/// <summary>Resolves explicit paths, PATH/PATHEXT commands and Windows App Paths.</summary>
public static class ExecutablePathResolver
{
    private const string AppPathsKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths";

    public static bool CanResolve(string? value) => TryResolve(value, out _);

    public static bool TryResolve(string? value, out string resolvedPath)
    {
        resolvedPath = string.Empty;
        if (string.IsNullOrWhiteSpace(value)) return false;

        var command = Environment.ExpandEnvironmentVariables(value.Trim().Trim('"'));
        if (File.Exists(command))
        {
            resolvedPath = Path.GetFullPath(command);
            return true;
        }

        // Never reduce a missing path to its file name and find an unrelated program.
        if (Path.IsPathFullyQualified(command)
            || command.Contains(Path.DirectorySeparatorChar)
            || command.Contains(Path.AltDirectorySeparatorChar))
            return false;

        foreach (var directory in EnumerateSearchDirectories())
        foreach (var fileName in EnumerateCandidateFileNames(command))
        {
            try
            {
                var candidate = Path.Combine(directory, fileName);
                if (!File.Exists(candidate)) continue;
                resolvedPath = Path.GetFullPath(candidate);
                return true;
            }
            catch (ArgumentException) { }
            catch (NotSupportedException) { }
            catch (PathTooLongException) { }
        }

        return TryResolveRegisteredAppPath(command, out resolvedPath);
    }

    private static IEnumerable<string> EnumerateSearchDirectories()
    {
        yield return Environment.CurrentDirectory;
        yield return AppContext.BaseDirectory;
        foreach (var directory in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
                     .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                     .Select(value => Environment.ExpandEnvironmentVariables(value.Trim('"')))
                     .Where(Directory.Exists)
                     .Distinct(StringComparer.OrdinalIgnoreCase))
            yield return directory;
    }

    private static IEnumerable<string> EnumerateCandidateFileNames(string command)
    {
        if (Path.HasExtension(command))
        {
            yield return command;
            yield break;
        }

        foreach (var extension in (Environment.GetEnvironmentVariable("PATHEXT") ?? ".COM;.EXE;.BAT;.CMD")
                     .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            yield return command + (extension.StartsWith('.') ? extension : "." + extension);
    }

    private static bool TryResolveRegisteredAppPath(string command, out string resolvedPath)
    {
        resolvedPath = string.Empty;
        var keyName = Path.HasExtension(command) ? command : command + ".exe";
        foreach (var hive in new[] { RegistryHive.CurrentUser, RegistryHive.LocalMachine })
        foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, view);
                using var key = baseKey.OpenSubKey($@"{AppPathsKey}\{keyName}");
                var path = key?.GetValue(null) as string;
                if (string.IsNullOrWhiteSpace(path)) continue;
                path = Environment.ExpandEnvironmentVariables(path.Trim().Trim('"'));
                if (!File.Exists(path)) continue;
                resolvedPath = Path.GetFullPath(path);
                return true;
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            catch (System.Security.SecurityException) { }
        }
        return false;
    }
}
