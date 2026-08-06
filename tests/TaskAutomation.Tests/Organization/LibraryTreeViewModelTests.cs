using DesktopAutomation.Application.Interfaces;
using DesktopAutomation.Application.Organization;
using DesktopAutomation.Application.Services;
using DesktopAutomationApp.Settings;
using DesktopAutomationApp.ViewModels.Library;
using System.Collections.Specialized;
using TaskAutomation.Tests.TestDoubles;

namespace TaskAutomation.Tests.Organization;

public sealed class LibraryTreeViewModelTests
{
    [Fact]
    public async Task FileCanBeMovedFromFolderToRootThroughTreeViewModel()
    {
        using var directory = new TemporaryDirectory();
        using var organization = new LibraryOrganizationService(
            Path.Combine(directory.Path, "LibraryLayout.json"));
        var folder = await organization.CreateFolderAsync(LibraryItemKind.Job, null, "Folder");
        var itemId = Guid.NewGuid();
        await organization.PlaceItemAsync(LibraryItemKind.Job, itemId, folder.Id);
        var preferences = new TestPreferencesService();
        preferences.Current.ExpandedLibraryFolders[nameof(LibraryItemKind.Job)] = [folder.Id];
        var viewModel = new LibraryTreeViewModel(
            organization,
            new TestDialogService(),
            preferences,
            LibraryItemKind.Job,
            "New job");
        await viewModel.SetItemsAsync(
        [
            new LibraryItemDescriptor
            {
                Id = itemId,
                Name = "Job",
                Model = new object(),
                Open = () => { }
            }
        ]);
        var itemNode = Assert.Single(viewModel.VisibleNodes, node => node.IsItem);

        await viewModel.MoveNodeAsync(itemNode, null);

        Assert.Empty((await organization.LoadAsync()).Placements);
        Assert.Null(Assert.Single(viewModel.VisibleNodes, node => node.IsItem).FolderId);
    }

    [Fact]
    public async Task LargeLibraryIsPublishedAsSingleCollectionUpdate()
    {
        const int folderCount = 100;
        const int itemCount = 2000;
        var folders = Enumerable.Range(0, folderCount)
            .Select(index => new LibraryFolder
            {
                Id = Guid.NewGuid(),
                Kind = LibraryItemKind.Job,
                Name = $"Folder {index:D3}"
            })
            .ToList();
        var items = Enumerable.Range(0, itemCount)
            .Select(index => new LibraryItemDescriptor
            {
                Id = Guid.NewGuid(),
                Name = $"Job {index:D4}",
                Model = new object(),
                Open = () => { }
            })
            .ToList();
        var layout = new LibraryLayout { Folders = folders };
        layout.Placements.AddRange(items.Select((item, index) => new LibraryPlacement
        {
            Kind = LibraryItemKind.Job,
            ItemId = item.Id,
            FolderId = folders[index % folders.Count].Id
        }));
        var preferences = new TestPreferencesService();
        preferences.Current.ExpandedLibraryFolders[nameof(LibraryItemKind.Job)] =
            folders.Select(folder => folder.Id).ToList();
        var viewModel = CreateViewModel(new InMemoryOrganizationService(layout), preferences);
        var collectionChanges = new List<NotifyCollectionChangedAction>();
        viewModel.VisibleNodes.CollectionChanged += (_, args) => collectionChanges.Add(args.Action);

        await viewModel.SetItemsAsync(items);

        Assert.Equal(folderCount + itemCount, viewModel.VisibleNodes.Count);
        Assert.Equal([NotifyCollectionChangedAction.Reset], collectionChanges);
    }

    [Fact]
    public async Task RepeatedDragOverSameFolderDoesNotUpdateVisibleNodesAgain()
    {
        var folder = new LibraryFolder
        {
            Id = Guid.NewGuid(),
            Kind = LibraryItemKind.Job,
            Name = "Folder"
        };
        var item = new LibraryItemDescriptor
        {
            Id = Guid.NewGuid(),
            Name = "Job",
            Model = new object(),
            Open = () => { }
        };
        var layout = new LibraryLayout
        {
            Folders = [folder],
            Placements =
            [
                new LibraryPlacement
                {
                    Kind = LibraryItemKind.Job,
                    ItemId = item.Id,
                    FolderId = folder.Id
                }
            ]
        };
        var preferences = new TestPreferencesService();
        preferences.Current.ExpandedLibraryFolders[nameof(LibraryItemKind.Job)] = [folder.Id];
        var viewModel = CreateViewModel(new InMemoryOrganizationService(layout), preferences);
        await viewModel.SetItemsAsync([item]);
        var folderNode = Assert.Single(viewModel.VisibleNodes, node => node.IsFolder);
        viewModel.SetDropTarget(folderNode);
        var propertyChanges = 0;
        foreach (var node in viewModel.VisibleNodes)
            node.PropertyChanged += (_, _) => propertyChanges++;

        viewModel.SetDropTarget(folderNode);

        Assert.Equal(0, propertyChanges);
    }

    [Fact]
    public async Task DragPreviewExposesDraggedFolderOrFileWithItsName()
    {
        var folder = new LibraryFolder
        {
            Id = Guid.NewGuid(),
            Kind = LibraryItemKind.Job,
            Name = "Reports"
        };
        var item = new LibraryItemDescriptor
        {
            Id = Guid.NewGuid(),
            Name = "Daily report",
            Model = new object(),
            Open = () => { }
        };
        var viewModel = CreateViewModel(
            new InMemoryOrganizationService(new LibraryLayout { Folders = [folder] }),
            new TestPreferencesService());
        await viewModel.SetItemsAsync([item]);
        var folderNode = Assert.Single(viewModel.VisibleNodes, node => node.IsFolder);
        var itemNode = Assert.Single(viewModel.VisibleNodes, node => node.IsItem);

        viewModel.BeginDrag(folderNode);

        Assert.True(viewModel.IsDragActive);
        Assert.True(viewModel.DraggedItemIsFolder);
        Assert.Equal("Reports", viewModel.DraggedItemName);

        viewModel.EndDrag();
        viewModel.BeginDrag(itemNode);

        Assert.True(viewModel.IsDragActive);
        Assert.False(viewModel.DraggedItemIsFolder);
        Assert.Equal("Daily report", viewModel.DraggedItemName);

        viewModel.EndDrag();
        Assert.False(viewModel.IsDragActive);
        Assert.Empty(viewModel.DraggedItemName);
    }

    [Fact]
    public async Task OpenCommandInvokesItemOpenAction()
    {
        var opened = false;
        var item = new LibraryItemDescriptor
        {
            Id = Guid.NewGuid(),
            Name = "Daily report",
            Model = new object(),
            Open = () => opened = true
        };
        var viewModel = CreateViewModel(
            new InMemoryOrganizationService(new LibraryLayout()),
            new TestPreferencesService());
        await viewModel.SetItemsAsync([item]);
        var itemNode = Assert.Single(viewModel.VisibleNodes, node => node.IsItem);

        viewModel.OpenNodeCommand.Execute(itemNode);

        Assert.True(opened);
    }

    [Fact]
    public async Task FolderCountsIncludeItemsFromNestedFolders()
    {
        var parent = new LibraryFolder
        {
            Id = Guid.NewGuid(),
            Kind = LibraryItemKind.Job,
            Name = "Parent"
        };
        var child = new LibraryFolder
        {
            Id = Guid.NewGuid(),
            Kind = LibraryItemKind.Job,
            ParentId = parent.Id,
            Name = "Child"
        };
        var parentItem = CreateItem("Parent job");
        var childItem = CreateItem("Child job");
        var layout = new LibraryLayout
        {
            Folders = [parent, child],
            Placements =
            [
                new LibraryPlacement
                {
                    Kind = LibraryItemKind.Job,
                    ItemId = parentItem.Id,
                    FolderId = parent.Id
                },
                new LibraryPlacement
                {
                    Kind = LibraryItemKind.Job,
                    ItemId = childItem.Id,
                    FolderId = child.Id
                }
            ]
        };
        var preferences = new TestPreferencesService();
        preferences.Current.ExpandedLibraryFolders[nameof(LibraryItemKind.Job)] = [parent.Id, child.Id];
        var viewModel = CreateViewModel(new InMemoryOrganizationService(layout), preferences);

        await viewModel.SetItemsAsync([parentItem, childItem]);

        Assert.Equal(2, viewModel.TotalItemCount);
        Assert.Equal(2, viewModel.TotalFolderCount);
        Assert.Equal(2, Assert.Single(viewModel.VisibleNodes, node => node.Folder?.Id == parent.Id).ContainedItemCount);
        Assert.Equal(1, Assert.Single(viewModel.VisibleNodes, node => node.Folder?.Id == child.Id).ContainedItemCount);
    }

    [Fact]
    public async Task SearchSummaryAndResetReflectVisibleMatches()
    {
        var viewModel = CreateViewModel(
            new InMemoryOrganizationService(new LibraryLayout()),
            new TestPreferencesService());
        await viewModel.SetItemsAsync([CreateItem("Daily report"), CreateItem("Monthly report")]);

        viewModel.SearchText = "Daily";

        Assert.True(viewModel.HasSearchText);
        Assert.Equal(1, viewModel.SearchResultCount);
        Assert.Single(viewModel.VisibleNodes);

        viewModel.ClearSearchCommand.Execute(null);

        Assert.False(viewModel.HasSearchText);
        Assert.Equal(2, viewModel.SearchResultCount);
        Assert.Equal(2, viewModel.VisibleNodes.Count);
    }

    private static LibraryItemDescriptor CreateItem(string name) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Model = new object(),
        Open = () => { }
    };

    private static LibraryTreeViewModel CreateViewModel(
        ILibraryOrganizationService organization,
        IUserPreferencesService preferences) =>
        new(
            organization,
            new TestDialogService(),
            preferences,
            LibraryItemKind.Job,
            "New job");

    private sealed class TestPreferencesService : IUserPreferencesService
    {
        public UserPreferences Current { get; } = new();
        public Task LoadAsync() => Task.CompletedTask;
        public Task SaveAsync() => Task.CompletedTask;
    }

    private sealed class TestDialogService : IDialogService
    {
        public Task<bool> ConfirmAsync(string message, string title) => Task.FromResult(false);
        public Task<bool?> ConfirmWithCancelAsync(string message, string title) =>
            Task.FromResult<bool?>(false);
        public Task<string?> AskForNameAsync(string title, string prompt, string? defaultValue = null) =>
            Task.FromResult<string?>(null);
        public void ShowError(string message, string title) { }
    }

    private sealed class InMemoryOrganizationService(LibraryLayout layout) : ILibraryOrganizationService
    {
        public Task<LibraryLayout> LoadAsync() => Task.FromResult(layout);
        public Task<LibraryFolder> CreateFolderAsync(LibraryItemKind kind, Guid? parentId, string name) =>
            throw new NotSupportedException();
        public Task RenameFolderAsync(Guid folderId, string name) => throw new NotSupportedException();
        public Task MoveFolderAsync(Guid folderId, Guid? parentId) => throw new NotSupportedException();
        public Task DeleteFolderAsync(Guid folderId) => throw new NotSupportedException();
        public Task PlaceItemAsync(LibraryItemKind kind, Guid itemId, Guid? folderId) =>
            throw new NotSupportedException();
        public Task RemoveItemAsync(LibraryItemKind kind, Guid itemId) => throw new NotSupportedException();
    }
}
