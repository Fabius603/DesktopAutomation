using DesktopAutomation.Application.Organization;

namespace TaskAutomation.Tests.Organization;

public sealed class LibraryBrowserIndexTests
{
    [Theory]
    [InlineData(LibraryItemKind.Job)]
    [InlineData(LibraryItemKind.Makro)]
    [InlineData(LibraryItemKind.Automation)]
    public void NavigationSearchAndSortingRespectExistingPlacements(LibraryItemKind kind)
    {
        var parent = new LibraryFolder { Kind = kind, Name = "Office" };
        var child = new LibraryFolder { Kind = kind, ParentId = parent.Id, Name = "Invoices" };
        LibraryBrowserItem[] items = [new(Guid.NewGuid(), "Zulu", "PDF archive"), new(Guid.NewGuid(), "Alpha", "Monthly"), new(Guid.NewGuid(), "Root", "")];
        var index = new LibraryBrowserIndex(new LibraryLayout
        {
            Folders = [parent, child],
            Placements = [new() { Kind = kind, ItemId = items[0].Id, FolderId = parent.Id }, new() { Kind = kind, ItemId = items[1].Id, FolderId = child.Id }]
        }, kind);

        Assert.Equal([items[2].Id], index.Query(items, null, "", false));
        Assert.Equal([parent.Id], index.QueryFolders(null, "", false).Select(folder => folder.Id));
        Assert.Equal([child.Id], index.QueryFolders(parent.Id, "INVOICE", false).Select(folder => folder.Id));
        Assert.Empty(index.QueryFolders(child.Id, "", false));
        Assert.Empty(index.QueryFolders(null, "Invoices", false));
        Assert.Equal([items[0].Id], index.Query(items, parent.Id, "pdf", false));
        Assert.Empty(index.Query(items, child.Id, "pdf", false));
        Assert.Equal([items[2].Id], index.Query(items, null, "", true));
        Assert.Equal([parent.Id, child.Id], index.PathTo(child.Id).Select(folder => folder.Id));
        Assert.Null(index.ExistingFolder(Guid.NewGuid()));
        Assert.Null(index.ContainingFolder(items[2].Id));
    }

    [Fact]
    public void StaleOrForeignPlacementsRemainAccessibleInAllItems()
    {
        var foreign = new LibraryFolder { Kind = LibraryItemKind.Makro, Name = "Macros" };
        var item = new LibraryBrowserItem(Guid.NewGuid(), "Job", "");
        var index = new LibraryBrowserIndex(new LibraryLayout
        {
            Folders = [foreign],
            Placements = [new() { Kind = LibraryItemKind.Job, ItemId = item.Id, FolderId = foreign.Id }]
        }, LibraryItemKind.Job);
        Assert.Equal([item.Id], index.Query([item], null, "", false));
        Assert.Empty(index.PathTo(foreign.Id));
        Assert.Null(index.ContainingFolder(item.Id));
    }
}
