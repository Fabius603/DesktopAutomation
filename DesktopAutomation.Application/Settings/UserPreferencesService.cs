using System.Text.Json;
using Common.ApplicationData;

namespace DesktopAutomation.Application.Settings;

public sealed class UserPreferencesService : IUserPreferencesService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _settingsPath;
    private readonly SemaphoreSlim _ioGate = new(1, 1);

    public UserPreferences Current { get; private set; } = new();

    public UserPreferencesService()
        : this(AppPaths.SettingsFile)
    {
    }

    public UserPreferencesService(string settingsPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(settingsPath);
        _settingsPath = settingsPath;
    }

    public async Task LoadAsync()
    {
        await _ioGate.WaitAsync();
        try
        {
            if (!File.Exists(_settingsPath)) return;
            await using var stream = File.OpenRead(_settingsPath);
            Current = await JsonSerializer.DeserializeAsync<UserPreferences>(stream, JsonOptions)
                      ?? new UserPreferences();
            Current.ExpandedLibraryFolders ??= new Dictionary<string, List<Guid>>();
        }
        catch (JsonException)
        {
            Current = new UserPreferences();
        }
        finally { _ioGate.Release(); }
    }

    public async Task SaveAsync()
    {
        await _ioGate.WaitAsync();
        try
        {
            var directory = Path.GetDirectoryName(_settingsPath)!;
            Directory.CreateDirectory(directory);
            var temporaryPath = _settingsPath + ".tmp";
            await using (var stream = File.Create(temporaryPath))
                await JsonSerializer.SerializeAsync(stream, Current, JsonOptions);
            File.Move(temporaryPath, _settingsPath, true);
        }
        finally { _ioGate.Release(); }
    }
}
