using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using DesktopAutomationApp.Localization;
using DesktopAutomationApp.Settings;
using DesktopAutomationApp.Views;
using Microsoft.Extensions.Logging;

namespace DesktopAutomationApp.Services;

public interface IReleaseNotesService
{
    Task ShowIfNewAsync();
    Task ShowAllAsync();
}

public sealed class ReleaseNotesService : IReleaseNotesService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IUserPreferencesService _preferences;
    private readonly ILocalizationService _localization;
    private readonly ILogger<ReleaseNotesService> _log;
    private IReadOnlyList<ReleaseNoteDefinition>? _definitions;
    private bool _automaticCheckCompleted;

    public ReleaseNotesService(
        IUserPreferencesService preferences,
        ILocalizationService localization,
        ILogger<ReleaseNotesService> log)
    {
        _preferences = preferences;
        _localization = localization;
        _log = log;
    }

    public async Task ShowIfNewAsync()
    {
        if (_automaticCheckCompleted)
            return;

        _automaticCheckCompleted = true;
        var currentVersion = GetCurrentVersion();
        if (CompareVersions(_preferences.Current.LastSeenReleaseNotesVersion, currentVersion) >= 0)
            return;

        var unseenNotes = GetDisplayNotes()
            .Where(note => CompareVersions(note.Version, _preferences.Current.LastSeenReleaseNotesVersion) > 0
                           && CompareVersions(note.Version, currentVersion) <= 0)
            .ToArray();

        if (unseenNotes.Length == 0)
            return;

        ShowWindow(unseenNotes);
        _preferences.Current.LastSeenReleaseNotesVersion = currentVersion;
        await SaveSeenVersionAsync();
    }

    public Task ShowAllAsync()
    {
        var notes = GetDisplayNotes();
        if (notes.Count > 0)
            ShowWindow(notes);

        return Task.CompletedTask;
    }

    private void ShowWindow(IReadOnlyList<ReleaseNoteDisplay> notes)
    {
        var window = new ReleaseNotesWindow(notes)
        {
            Owner = Application.Current?.MainWindow
        };
        window.ShowDialog();
    }

    private IReadOnlyList<ReleaseNoteDisplay> GetDisplayNotes()
    {
        var useEnglish = _localization.CurrentCulture.TwoLetterISOLanguageName == "en";
        return LoadDefinitions()
            .OrderByDescending(note => ParseVersion(note.Version))
            .Select(note => new ReleaseNoteDisplay(
                note.Version,
                note.Date.ToString("d", _localization.CurrentCulture),
                note.Sections.Select(section => new ReleaseNoteSectionDisplay(
                    useEnglish ? section.Title.En : section.Title.De,
                    section.Changes.Select(change => new ReleaseNoteChangeDisplay(
                        LocalizationService.Instance[$"Ui.ReleaseNotes.Category.{change.Category}"],
                        useEnglish ? change.En : change.De)).ToArray())).ToArray()))
            .ToArray();
    }

    private IReadOnlyList<ReleaseNoteDefinition> LoadDefinitions()
    {
        if (_definitions != null)
            return _definitions;

        var assembly = typeof(ReleaseNotesService).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .Single(name => name.EndsWith("Resources.ReleaseNotes.json", StringComparison.Ordinal));
        using var stream = assembly.GetManifestResourceStream(resourceName)
                           ?? throw new InvalidOperationException("The embedded release notes could not be loaded.");
        _definitions = JsonSerializer.Deserialize<List<ReleaseNoteDefinition>>(stream, JsonOptions) ?? [];
        return _definitions;
    }

    private async Task SaveSeenVersionAsync()
    {
        try
        {
            await _preferences.SaveAsync();
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Die zuletzt gelesene Release-Notes-Version konnte nicht gespeichert werden.");
        }
    }

    private static string GetCurrentVersion()
    {
        var version = typeof(App).Assembly.GetName().Version;
        return version == null ? "0.0.0" : $"{version.Major}.{version.Minor}.{version.Build}";
    }

    private static int CompareVersions(string? left, string? right) =>
        ParseVersion(left).CompareTo(ParseVersion(right));

    private static Version ParseVersion(string? value) =>
        Version.TryParse(value?.TrimStart('v'), out var version) ? version : new Version(0, 0, 0);

    private sealed record ReleaseNoteDefinition(
        string Version,
        DateOnly Date,
        IReadOnlyList<ReleaseNoteSectionDefinition> Sections);

    private sealed record LocalizedReleaseText(string De, string En);

    private sealed record ReleaseNoteSectionDefinition(
        LocalizedReleaseText Title,
        IReadOnlyList<ReleaseNoteChangeDefinition> Changes);

    private sealed record ReleaseNoteChangeDefinition(string Category, string De, string En);
}

public sealed record ReleaseNoteDisplay(
    string Version,
    string Date,
    IReadOnlyList<ReleaseNoteSectionDisplay> Sections);

public sealed record ReleaseNoteSectionDisplay(
    string Title,
    IReadOnlyList<ReleaseNoteChangeDisplay> Changes);

public sealed record ReleaseNoteChangeDisplay(string Category, string Text);
