using System.Reflection;
using Velopack;
using Velopack.Sources;

namespace DesktopAutomationApp.Services;

public interface IUpdateService
{
    Task<UpdateCheckResult> CheckForUpdateAsync();

    Task<bool> DownloadUpdateAsync(IProgress<int>? progress = null);

    bool PrepareUpdateAndRestart();
}

public sealed record UpdateCheckResult(
    bool HasUpdate,
    string LatestVersion,
    string ReleaseUrl,
    string CurrentVersion);

public sealed class UpdateService : IUpdateService
{
    private const string RepositoryUrl = "https://github.com/Fabius603/DesktopAutomation";

    private readonly UpdateManager _updateManager = new(
        new GithubSource(RepositoryUrl, accessToken: null, prerelease: false));

    private UpdateInfo? _availableUpdate;

    public async Task<UpdateCheckResult> CheckForUpdateAsync()
    {
        var currentVersion = _updateManager.CurrentVersion?.ToString() ?? GetAssemblyVersion();

        // A regular IDE/publish launch has no Velopack installation metadata.
        if (!_updateManager.IsInstalled)
            return new UpdateCheckResult(false, string.Empty, RepositoryUrl + "/releases", currentVersion);

        _availableUpdate = await _updateManager.CheckForUpdatesAsync();
        if (_availableUpdate is null)
            return new UpdateCheckResult(false, string.Empty, RepositoryUrl + "/releases", currentVersion);

        return new UpdateCheckResult(
            true,
            _availableUpdate.TargetFullRelease.Version.ToString(),
            RepositoryUrl + "/releases/tag/v" + _availableUpdate.TargetFullRelease.Version,
            currentVersion);
    }

    public async Task<bool> DownloadUpdateAsync(IProgress<int>? progress = null)
    {
        if (!_updateManager.IsInstalled || _availableUpdate is null)
            return false;

        await _updateManager.DownloadUpdatesAsync(
            _availableUpdate,
            progress is null ? null : progress.Report);

        return true;
    }

    public bool PrepareUpdateAndRestart()
    {
        var target = _availableUpdate?.TargetFullRelease ?? _updateManager.UpdatePendingRestart;
        if (!_updateManager.IsInstalled || target is null)
            return false;

        // Let WPF and the generic host shut down cleanly. Velopack waits for this
        // process to exit, applies the prepared package, and relaunches the app.
        _updateManager.WaitExitThenApplyUpdates(
            target,
            silent: false,
            restart: true,
            restartArgs: Array.Empty<string>());

        return true;
    }

    private static string GetAssemblyVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        return version is null ? "0.0.0" : $"{version.Major}.{version.Minor}.{version.Build}";
    }
}
