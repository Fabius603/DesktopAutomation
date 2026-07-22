namespace Common.ApplicationData;

/// <summary>
/// Defines every persistent DesktopAutomation path. The Velopack installation
/// directory is deliberately not part of this class.
/// </summary>
public static class AppPaths
{
    public const string ApplicationFolderName = "DesktopAutomation";

    public static string RoamingRoot { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        ApplicationFolderName);

    public static string LocalRoot { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        ApplicationFolderName);

    public static string ConfigRoot => Path.Combine(RoamingRoot, "Configs");
    public static string JobConfigDirectory => Path.Combine(ConfigRoot, "Job");
    public static string MakroConfigDirectory => Path.Combine(ConfigRoot, "Makro");
    public static string AutomationConfigDirectory => Path.Combine(ConfigRoot, "Automation");
    public static string YoloModelsDirectory => Path.Combine(RoamingRoot, "YoloModels");

    public static string SettingsFile => Path.Combine(LocalRoot, "settings.json");
    public static string LogsDirectory => Path.Combine(LocalRoot, "Logs");
    public static string ExecutionLogsDirectory => Path.Combine(LogsDirectory, "Executions");
    public static string JobExecutionLogsDirectory => Path.Combine(ExecutionLogsDirectory, "Job");
    public static string AutomationLogsDirectory => Path.Combine(LogsDirectory, "Automations");

    public static string GetRoamingPath(params string[] segments) => Combine(RoamingRoot, segments);
    public static string GetLocalPath(params string[] segments) => Combine(LocalRoot, segments);

    /// <summary>
    /// Copies data from the former DesktopAutomationApp roots without replacing
    /// files that already exist in the canonical locations.
    /// </summary>
    public static void MigrateLegacyData()
    {
        TryCopyMissingFiles(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DesktopAutomationApp"),
            RoamingRoot);
        TryCopyMissingFiles(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DesktopAutomationApp"),
            LocalRoot);
    }

    private static string Combine(string root, IEnumerable<string> segments)
    {
        var path = root;
        foreach (var segment in segments)
        {
            if (string.IsNullOrWhiteSpace(segment) || Path.IsPathRooted(segment))
                throw new ArgumentException("Path segments must be non-empty relative paths.", nameof(segments));
            path = Path.Combine(path, segment);
        }
        return path;
    }

    private static void TryCopyMissingFiles(string sourceRoot, string targetRoot)
    {
        if (!Directory.Exists(sourceRoot))
            return;

        try
        {
            foreach (var sourceFile in Directory.EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(sourceRoot, sourceFile);
                var targetFile = Path.Combine(targetRoot, relativePath);
                if (File.Exists(targetFile))
                    continue;

                Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);
                File.Copy(sourceFile, targetFile, overwrite: false);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Migration is best-effort. Existing canonical data must remain usable
            // even if an obsolete directory is locked or no longer readable.
        }
    }
}
