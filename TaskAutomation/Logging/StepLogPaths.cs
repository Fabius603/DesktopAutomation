using TaskAutomation.Jobs;
using TaskAutomation.Steps;

namespace TaskAutomation.Logging;

/// <summary>Approved filesystem metadata. Resolves paths without probing the filesystem.</summary>
public static class StepLogPaths
{
    public static string? Normalize(string? value, bool expandEnvironment = false)
    {
        if (string.IsNullOrWhiteSpace(value) || value.IndexOfAny(Path.GetInvalidPathChars()) >= 0) return null;
        try { return Path.GetFullPath(expandEnvironment ? Environment.ExpandEnvironmentVariables(value.Trim()) : value.Trim()); }
        catch (Exception error) when (error is ArgumentException or NotSupportedException or IOException) { return null; }
    }
    public static IReadOnlyList<LogPath> Capture(JobStep step, object? result = null)
    {
        var paths = new List<LogPath>();
        void Add(string kind, string? value) { if (Normalize(value) is { } path) paths.Add(new(kind, path)); }
        if (result is SaveImageResult image && !string.IsNullOrWhiteSpace(image.FilePath))
        {
            if (Normalize(image.FilePath) is { } file) { Add("TargetFile", file); Add("TargetDirectory", Path.GetDirectoryName(file)); }
        }
        else if (step is SaveImageStep { Settings: not null } save)
        {
            Add("TargetDirectory", Normalize(save.Settings.SavePath, expandEnvironment: true));
            if (Normalize(save.Settings.SavePath, expandEnvironment: true) is { } directory && !string.IsNullOrWhiteSpace(save.Settings.FileName)
                && save.Settings.FileName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0
                && Path.GetFileName(save.Settings.FileName) == save.Settings.FileName)
                Add("TargetFile", Path.Combine(directory, save.Settings.FileName));
        }
        if (result is FileSystemOperationResult files)
        { Add("Source", files.SourcePath); Add("Target", files.TargetPath); }
        if (result is FileSystemPathQueryResult query) Add("CheckedPath", query.Path);
        if (step is ScriptExecutionStep { Settings: not null } script) Add("ScriptFile", script.Settings.ScriptPath);
        return paths;
    }
}
