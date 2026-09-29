using DesktopAutomation.Application.Organization;
using DesktopAutomation.Application.Services;
using TaskAutomation.Tests.TestDoubles;

namespace TaskAutomation.Tests.Organization;

public sealed class LibraryOrganizationServiceTests
{
    [Fact]
    public async Task MissingLayout_StartsWithEmptyRoot()
    {
        using var directory = new TemporaryDirectory();
        using var service = Service(directory);

        var layout = await service.LoadAsync();

        Assert.Empty(layout.Folders);
        Assert.Empty(layout.Placements);
    }

    [Fact]
    public async Task FoldersCanBeNestedAndItemsPlaced()
    {
        using var directory = new TemporaryDirectory();
        using var service = Service(directory);
        var parent = await service.CreateFolderAsync(LibraryItemKind.Job, null, "Produktion");
        var child = await service.CreateFolderAsync(LibraryItemKind.Job, parent.Id, "Berichte");
        var itemId = Guid.NewGuid();

        await service.PlaceItemAsync(LibraryItemKind.Job, itemId, child.Id);
        var layout = await service.LoadAsync();

        Assert.Equal(parent.Id, Assert.Single(layout.Folders, folder => folder.Id == child.Id).ParentId);
        Assert.Equal(child.Id, Assert.Single(layout.Placements).FolderId);
    }

    [Fact]
    public async Task ItemCanBeMovedFromFolderBackToRoot()
    {
        using var directory = new TemporaryDirectory();
        using var service = Service(directory);
        var folder = await service.CreateFolderAsync(LibraryItemKind.Job, null, "Folder");
        var itemId = Guid.NewGuid();
        await service.PlaceItemAsync(LibraryItemKind.Job, itemId, folder.Id);

        await service.PlaceItemAsync(LibraryItemKind.Job, itemId, null);
        var layout = await service.LoadAsync();

        Assert.Empty(layout.Placements);
    }

    [Fact]
    public async Task ExistingNullPlacementIsNormalizedToImplicitRoot()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "LibraryLayout.json");
        var itemId = Guid.NewGuid();
        await File.WriteAllTextAsync(path, $$"""
            {
              "FormatVersion": 1,
              "Folders": [],
              "Placements": [
                { "Kind": "Job", "ItemId": "{{itemId}}", "FolderId": null, "SortOrder": 3 }
              ]
            }
            """);
        using var service = new LibraryOrganizationService(path);

        var layout = await service.LoadAsync();

        Assert.Empty(layout.Placements);
    }

    [Fact]
    public async Task FolderCannotBeMovedIntoOwnDescendant()
    {
        using var directory = new TemporaryDirectory();
        using var service = Service(directory);
        var parent = await service.CreateFolderAsync(LibraryItemKind.Job, null, "Parent");
        var child = await service.CreateFolderAsync(LibraryItemKind.Job, parent.Id, "Child");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.MoveFolderAsync(parent.Id, child.Id));
    }

    [Fact]
    public async Task DeletingFolderPromotesChildrenAndItems()
    {
        using var directory = new TemporaryDirectory();
        using var service = Service(directory);
        var parent = await service.CreateFolderAsync(LibraryItemKind.Makro, null, "Parent");
        var child = await service.CreateFolderAsync(LibraryItemKind.Makro, parent.Id, "Child");
        var itemId = Guid.NewGuid();
        await service.PlaceItemAsync(LibraryItemKind.Makro, itemId, parent.Id);

        await service.DeleteFolderAsync(parent.Id);
        var layout = await service.LoadAsync();

        Assert.Null(Assert.Single(layout.Folders, folder => folder.Id == child.Id).ParentId);
        Assert.Empty(layout.Placements);
    }

    [Fact]
    public async Task DuplicateSiblingNamesAreRejectedCaseInsensitively()
    {
        using var directory = new TemporaryDirectory();
        using var service = Service(directory);
        await service.CreateFolderAsync(LibraryItemKind.Automation, null, "Arbeit");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateFolderAsync(LibraryItemKind.Automation, null, "arbeit"));
    }

    private static LibraryOrganizationService Service(TemporaryDirectory directory)
        => new(Path.Combine(directory.Path, "LibraryLayout.json"));
}
