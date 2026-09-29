using DesktopAutomation.Application.Settings;
using TaskAutomation.Tests.TestDoubles;

namespace DesktopAutomation.IntegrationTests.Settings;

public sealed class UserPreferencesServiceTests
{
    [Fact]
    public async Task SaveAndLoad_PreservesPreferencesThroughAtomicFileStorage()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "settings.json");
        var writer = new UserPreferencesService(path);
        writer.Current.Culture = "en-US";
        writer.Current.ExpandedLibraryFolders["jobs"] = [Guid.Parse("b94d42e5-f10a-4991-a3d8-f02bf4b148df")];

        await writer.SaveAsync();
        var reader = new UserPreferencesService(path);
        await reader.LoadAsync();

        Assert.Equal("en-US", reader.Current.Culture);
        Assert.Single(reader.Current.ExpandedLibraryFolders["jobs"]);
        Assert.False(File.Exists(path + ".tmp"));
    }

    [Fact]
    public async Task Load_InvalidOrPartiallyMigratedJson_UsesSafeDefaults()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "settings.json");
        await File.WriteAllTextAsync(path, "{ invalid");
        var service = new UserPreferencesService(path);

        await service.LoadAsync();

        Assert.Equal("de-DE", service.Current.Culture);
        Assert.NotNull(service.Current.ExpandedLibraryFolders);

        await File.WriteAllTextAsync(path, "{\"ExpandedLibraryFolders\":null}");
        await service.LoadAsync();

        Assert.NotNull(service.Current.ExpandedLibraryFolders);
    }
}
