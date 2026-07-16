using Microsoft.Win32;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace DesktopAutomationApp.Services;

public sealed record InstalledProgramSuggestion(
    string DisplayName,
    string Command,
    string ProcessName,
    bool IsDirectExecutable)
{
    public override string ToString() => Command;
}

public static class InstalledProgramDiscovery
{
    private const string AppPathsKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths";
    private static readonly Lazy<Task<IReadOnlyList<InstalledProgramSuggestion>>> CachedSuggestions =
        new(() => Task.Run(DiscoverCore));

    public static Task<IReadOnlyList<InstalledProgramSuggestion>> DiscoverAsync()
        => CachedSuggestions.Value;

    private static IReadOnlyList<InstalledProgramSuggestion> DiscoverCore()
    {
        var suggestions = new Dictionary<string, InstalledProgramSuggestion>(StringComparer.OrdinalIgnoreCase);
        AddPathPrograms(suggestions);
        AddRegisteredPrograms(suggestions);
        AddStartMenuPrograms(suggestions);
        return suggestions.Values
            .OrderBy(item => item.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(item => item.Command, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static void AddPathPrograms(IDictionary<string, InstalledProgramSuggestion> suggestions)
    {
        var directories = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => Environment.ExpandEnvironmentVariables(value.Trim('"')))
            .Where(Directory.Exists)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var directory in directories)
        {
            try
            {
                foreach (var path in Directory.EnumerateFiles(directory, "*.*", SearchOption.TopDirectoryOnly)
                             .Where(IsExecutableFile))
                {
                    var command = Path.GetFileName(path);
                    suggestions.TryAdd(command, new InstalledProgramSuggestion(
                        Path.GetFileNameWithoutExtension(command), command,
                        Path.GetFileNameWithoutExtension(command), true));
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            catch (ArgumentException) { }
        }
    }

    private static void AddRegisteredPrograms(IDictionary<string, InstalledProgramSuggestion> suggestions)
    {
        foreach (var hive in new[] { RegistryHive.CurrentUser, RegistryHive.LocalMachine })
        foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, view);
                using var key = baseKey.OpenSubKey(AppPathsKey);
                if (key == null) continue;
                foreach (var command in key.GetSubKeyNames()
                             .Where(name => name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)))
                {
                    using var appKey = key.OpenSubKey(command);
                    var target = appKey?.GetValue(null) as string;
                    var processName = string.IsNullOrWhiteSpace(target)
                        ? Path.GetFileNameWithoutExtension(command)
                        : Path.GetFileNameWithoutExtension(target.Trim().Trim('"'));
                    suggestions.TryAdd(command, new InstalledProgramSuggestion(
                        Path.GetFileNameWithoutExtension(command), command, processName, true));
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            catch (ArgumentException) { }
            catch (System.Security.SecurityException) { }
        }
    }

    private static void AddStartMenuPrograms(IDictionary<string, InstalledProgramSuggestion> suggestions)
    {
        Type? shellType = null;
        object? shell = null;
        var startMenus = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu)
        }.Where(Directory.Exists).Distinct(StringComparer.OrdinalIgnoreCase);

        try
        {
            shellType = Type.GetTypeFromProgID("WScript.Shell");
            shell = shellType == null ? null : Activator.CreateInstance(shellType);
            foreach (var startMenu in startMenus)
            {
                try
                {
                    foreach (var path in Directory.EnumerateFiles(startMenu, "*.lnk", SearchOption.AllDirectories))
                    {
                        var name = Path.GetFileNameWithoutExtension(path);
                        var target = TryGetShortcutTarget(path, shellType, shell);
                        var processName = string.IsNullOrWhiteSpace(target)
                            ? string.Empty
                            : Path.GetFileNameWithoutExtension(target);
                        suggestions.TryAdd(path, new InstalledProgramSuggestion(name, path, processName, false));
                    }
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
                catch (ArgumentException) { }
            }
        }
        catch (COMException) { }
        catch (TargetInvocationException) { }
        finally
        {
            if (shell != null && Marshal.IsComObject(shell)) Marshal.FinalReleaseComObject(shell);
        }
    }

    private static string? TryGetShortcutTarget(string shortcutPath, Type? shellType, object? shell)
    {
        object? shortcut = null;
        try
        {
            if (shellType == null || shell == null) return null;
            shortcut = shellType.InvokeMember(
                "CreateShortcut", BindingFlags.InvokeMethod, null, shell, [shortcutPath]);
            return shortcut?.GetType().InvokeMember(
                "TargetPath", BindingFlags.GetProperty, null, shortcut, null) as string;
        }
        catch (COMException) { return null; }
        catch (TargetInvocationException) { return null; }
        finally
        {
            if (shortcut != null && Marshal.IsComObject(shortcut)) Marshal.FinalReleaseComObject(shortcut);
        }
    }

    private static bool IsExecutableFile(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Equals(".exe", StringComparison.OrdinalIgnoreCase)
               || extension.Equals(".com", StringComparison.OrdinalIgnoreCase)
               || extension.Equals(".bat", StringComparison.OrdinalIgnoreCase)
               || extension.Equals(".cmd", StringComparison.OrdinalIgnoreCase);
    }
}
