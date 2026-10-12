namespace Common.ApplicationData;

/// <summary>
/// Defines every persistent DesktopAutomation path. The Velopack installation
/// directory is deliberately not part of this class.
/// </summary>
public static class AppPaths
{
    static AppPaths() { } // Initialize only after the command-line profile is selected.
    public const string ApplicationFolderName = "DesktopAutomation";

    public static string? ProfileName { get; } = ValidateProfile(Environment.GetEnvironmentVariable("DESKTOPAUTOMATION_PROFILE"));
    public static bool IsDefaultProfile => ProfileName is null;
    private static string ProfileFolder => ProfileName is null ? "" : Path.Combine("Profiles", ProfileName);
    public static string DataRoot { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ApplicationFolderName, ProfileFolder);

    public static string? ValidateProfile(string? name)
    {
        return ProfileNames.Validate(name);
    }

    public static string RoamingRoot { get; } = Path.Combine(DataRoot, "Roaming");

    public static string LocalRoot { get; } = Path.Combine(DataRoot, "Local");

    public static string ConfigRoot => Path.Combine(RoamingRoot, "Configs");
    public static string JobConfigDirectory => Path.Combine(ConfigRoot, "Job");
    public static string MakroConfigDirectory => Path.Combine(ConfigRoot, "Makro");
    public static string AutomationConfigDirectory => Path.Combine(ConfigRoot, "Automation");
    public static string LibraryLayoutFile => Path.Combine(ConfigRoot, "LibraryLayout.json");
    public static string YoloModelsDirectory => Path.Combine(RoamingRoot, "YoloModels");

    public static string SettingsFile => Path.Combine(LocalRoot, "settings.json");
    public static string SecretsDirectory => Path.Combine(LocalRoot, "Secrets");
    public static string LogsDirectory => Path.Combine(LocalRoot, "Logs");
    public static string LogAttentionDirectory => Path.Combine(LocalRoot, "LogAttention");
    public static string StructuredLogsDirectory => Path.Combine(LogsDirectory, "v2");
    public static string ExecutionLogsDirectory => Path.Combine(LogsDirectory, "Executions");
    public static string JobExecutionLogsDirectory => Path.Combine(ExecutionLogsDirectory, "Job");
    public static string AutomationLogsDirectory => Path.Combine(LogsDirectory, "Automations");

    public static string GetRoamingPath(params string[] segments) => Combine(RoamingRoot, segments);
    public static string GetLocalPath(params string[] segments) => Combine(LocalRoot, segments);

    /// <summary>
    /// Copies data from the former DesktopAutomationApp roots without replacing
    /// files that already exist in the canonical locations.
    /// </summary>
    public static bool MigrateLegacyData()
    {
        var legacyProfile = ProfileName is null ? "" : Path.Combine("Profiles", ProfileName);
        var roamingImported = LegacyDataImport.CopyOnce(RoamingRoot,
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ApplicationFolderName, legacyProfile),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DesktopAutomationApp", legacyProfile));
        var localImported = LegacyDataImport.CopyOnce(LocalRoot,
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ApplicationFolderName, legacyProfile),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DesktopAutomationApp", legacyProfile));
        return roamingImported && localImported;
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

}
