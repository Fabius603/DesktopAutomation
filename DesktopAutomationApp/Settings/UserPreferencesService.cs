using System.Text.Json;
using System.IO;
using Common.ApplicationData;

namespace DesktopAutomationApp.Settings;

public sealed class UserPreferencesService : IUserPreferencesService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _settingsPath;

    public UserPreferences Current { get; private set; } = new();

    public UserPreferencesService()
    {
        _settingsPath = AppPaths.SettingsFile;
    }

    public async Task LoadAsync()
    {
        try
        {
            if (!File.Exists(_settingsPath)) return;
            await using var stream = File.OpenRead(_settingsPath);
            Current = await JsonSerializer.DeserializeAsync<UserPreferences>(stream, JsonOptions)
                      ?? new UserPreferences();
        }
        catch (JsonException)
        {
            Current = new UserPreferences();
        }
    }

    public async Task SaveAsync()
    {
        var directory = Path.GetDirectoryName(_settingsPath)!;
        Directory.CreateDirectory(directory);
        var temporaryPath = _settingsPath + ".tmp";
        await using (var stream = File.Create(temporaryPath))
            await JsonSerializer.SerializeAsync(stream, Current, JsonOptions);
        File.Move(temporaryPath, _settingsPath, true);
    }
}
