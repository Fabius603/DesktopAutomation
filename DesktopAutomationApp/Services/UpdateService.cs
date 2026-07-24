using System.Reflection;
using Velopack;
using Velopack.Sources;
using Microsoft.Extensions.Logging;

namespace DesktopAutomationApp.Services;

public interface IUpdateService
{
    event Action<UpdateCheckResult>? UpdateChecked;

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
    private readonly ILogger<UpdateService> _log;
    private readonly SemaphoreSlim _checkLock = new(1, 1);

    public event Action<UpdateCheckResult>? UpdateChecked;

    public UpdateService(ILogger<UpdateService> log) => _log = log;

    public async Task<UpdateCheckResult> CheckForUpdateAsync()
    {
        await _checkLock.WaitAsync();
        try
        {
            var currentVersion = _updateManager.CurrentVersion?.ToString() ?? GetAssemblyVersion();
            _log.LogInformation("Update-Prüfung gestartet. Aktuelle Version: {Version}", currentVersion);

            // A regular IDE/publish launch has no Velopack installation metadata.
            if (!_updateManager.IsInstalled)
            {
                _log.LogInformation("Update-Prüfung übersprungen: Die Anwendung wird nicht über Velopack ausgeführt.");
                return Publish(new UpdateCheckResult(false, string.Empty, RepositoryUrl + "/releases", currentVersion));
            }

            _availableUpdate = await _updateManager.CheckForUpdatesAsync();
            if (_availableUpdate is null)
            {
                _log.LogInformation("Update-Prüfung abgeschlossen: Keine neue Version verfügbar.");
                return Publish(new UpdateCheckResult(false, string.Empty, RepositoryUrl + "/releases", currentVersion));
            }

            _log.LogInformation("Update verfügbar: {CurrentVersion} -> {LatestVersion}", currentVersion, _availableUpdate.TargetFullRelease.Version);

            return Publish(new UpdateCheckResult(
                true,
                _availableUpdate.TargetFullRelease.Version.ToString(),
                RepositoryUrl + "/releases/tag/v" + _availableUpdate.TargetFullRelease.Version,
                currentVersion));
        }
        finally
        {
            _checkLock.Release();
        }
    }

    private UpdateCheckResult Publish(UpdateCheckResult result)
    {
        UpdateChecked?.Invoke(result);
        return result;
    }

    public async Task<bool> DownloadUpdateAsync(IProgress<int>? progress = null)
    {
        if (!_updateManager.IsInstalled || _availableUpdate is null)
        {
            _log.LogWarning("Update-Download übersprungen: Es ist kein installierbares Update verfügbar.");
            return false;
        }

        _log.LogInformation("Update-Download gestartet: Version {Version}", _availableUpdate.TargetFullRelease.Version);
        await _updateManager.DownloadUpdatesAsync(
            _availableUpdate,
            progress is null ? null : progress.Report);

        _log.LogInformation("Update-Download abgeschlossen: Version {Version}", _availableUpdate.TargetFullRelease.Version);
        return true;
    }

    public bool PrepareUpdateAndRestart()
    {
        var target = _availableUpdate?.TargetFullRelease ?? _updateManager.UpdatePendingRestart;
        if (!_updateManager.IsInstalled || target is null)
        {
            _log.LogWarning("Update-Neustart übersprungen: Es ist kein vorbereitetes Update verfügbar.");
            return false;
        }

        // Let WPF and the generic host shut down cleanly. Velopack waits for this
        // process to exit, applies the prepared package, and relaunches the app.
        _updateManager.WaitExitThenApplyUpdates(
            target,
            silent: false,
            restart: true,
            restartArgs: Array.Empty<string>());

        _log.LogInformation("Anwendung wird zur Installation von Version {Version} neu gestartet.", target.Version);

        return true;
    }

    private static string GetAssemblyVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        return version is null ? "0.0.0" : $"{version.Major}.{version.Minor}.{version.Build}";
    }
}
