using DesktopAutomation.Application.Interfaces;
using DesktopAutomation.Application.Organization;
using DesktopAutomation.Application.Services;
using DesktopAutomation.Application.Settings;
using DesktopAutomationApp.ViewModels.Library;
using System.Collections.Specialized;
using TaskAutomation.Tests.TestDoubles;

namespace TaskAutomation.Tests.Organization;

public sealed class LibraryTreeViewModelTests
{
    [Theory]
    [InlineData(LibraryItemKind.Job)]
    [InlineData(LibraryItemKind.Makro)]
    [InlineData(LibraryItemKind.Automation)]
    public async Task BothPanesExposeFilesAndFoldersAndRootShowsOnlyDirectContents(LibraryItemKind kind)
    {
        using var directory = new TemporaryDirectory();
        using var organization = new LibraryOrganizationService(Path.Combine(directory.Path, "library.json"));
        var parent = await organization.CreateFolderAsync(kind, null, "Parent");
        var child = await organization.CreateFolderAsync(kind, parent.Id, "Child");
        var rootItem = CreateItem("Root file");
        var nestedItem = CreateItem("Nested file");
        await organization.PlaceItemAsync(kind, nestedItem.Id, parent.Id);
        var vm = new LibraryTreeViewModel(organization, new TestDialogService(), new TestPreferencesService(), kind, "New");
        await vm.SetItemsAsync([rootItem, nestedItem]);

        Assert.Equal(vm.RootLabel, vm.AllItemsLabel);
        Assert.Equal([parent.Id, rootItem.Id], vm.ContentNodes.Select(node => node.Id));
        Assert.DoesNotContain(vm.VisibleNodes, node => node.Id == nestedItem.Id);
        vm.OpenNodeCommand.Execute(vm.ContentNodes.Single(node => node.Id == parent.Id));
        Assert.Equal([child.Id, nestedItem.Id], vm.ContentNodes.Select(node => node.Id));
        Assert.Contains(vm.VisibleNodes, node => node.Id == nestedItem.Id);
        vm.NavigateCommand.Execute(vm.Breadcrumbs[0]);
        Assert.Null(vm.SelectedFolderId);
        Assert.Equal([parent.Id, rootItem.Id], vm.ContentNodes.Select(node => node.Id));
    }

    [Fact]
    public async Task CapturedBatchMove_RemainsScopedWhenSelectionChanges()
    {
        using var directory = new TemporaryDirectory();
        using var organization = new LibraryOrganizationService(Path.Combine(directory.Path, "LibraryLayout.json"));
        var folder = await organization.CreateFolderAsync(LibraryItemKind.Job, null, "Target");
        var vm = new LibraryTreeViewModel(organization, new TestDialogService(), new TestPreferencesService(), LibraryItemKind.Job, "New job");
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        await vm.SetItemsAsync(ids.Select((id, index) => new LibraryItemDescriptor { Id = id, Name = index.ToString(), Model = new object(), Open = () => { } }));
        var targets = vm.ContentNodes.Where(node => ids.Take(2).Contains(node.Id)).ToArray();
        vm.SetSelectedNodes(vm.ContentNodes.Where(node => node.Id == ids[2]));
        await vm.MoveSelectionAsync(folder.Id, targets);
        var layout = await organization.LoadAsync();
        Assert.All(ids.Take(2), id => Assert.Contains(layout.Placements, placement => placement.ItemId == id && placement.FolderId == folder.Id));
        Assert.DoesNotContain(layout.Placements, placement => placement.ItemId == ids[2] && placement.FolderId == folder.Id);
    }

    [Fact]
    public async Task CreatingSubfolderExpandsParentAndPreservesTheChosenFolder()
    {
        using var directory = new TemporaryDirectory();
        using var organization = new LibraryOrganizationService(Path.Combine(directory.Path, "LibraryLayout.json"));
        var parent = await organization.CreateFolderAsync(LibraryItemKind.Job, null, "Parent");
        var preferences = new TestPreferencesService();
        var vm = new LibraryTreeViewModel(organization, new TestDialogService("Child"), preferences, LibraryItemKind.Job, "New job");
        await vm.SetItemsAsync([]);
        vm.SelectedFolderId = parent.Id;
        var command = (global::DesktopAutomationApp.ViewModels.AsyncRelayCommand<LibraryTreeNodeViewModel?>)vm.NewSubfolderCommand;
        command.Execute(Assert.Single(vm.FolderNodes));
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (command.IsExecuting) await Task.Delay(10, timeout.Token);
        Assert.Contains(parent.Id, preferences.Current.ExpandedLibraryFolders[nameof(LibraryItemKind.Job)]);
        Assert.Contains(vm.FolderNodes, node => node.Name == "Child");
        Assert.Equal(parent.Id, vm.SelectedFolderId);
        vm.SearchText = "No matching job";
        Assert.Equal(2, vm.FolderNodes.Count);
        Assert.Empty(vm.ContentNodes);
        Assert.Equal(parent.Id, vm.SelectedFolderId);
    }

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
    public async Task RenameNodeCommandInvokesItemRenameAction()
    {
        var renamed = false;
        var item = new LibraryItemDescriptor
        {
            Id = Guid.NewGuid(),
            Name = "Daily report",
            Model = new object(),
            Open = () => { },
            RenameAsync = () => { renamed = true; return Task.CompletedTask; }
        };
        var viewModel = CreateViewModel(
            new InMemoryOrganizationService(new LibraryLayout()),
            new TestPreferencesService());
        await viewModel.SetItemsAsync([item]);
        var itemNode = Assert.Single(viewModel.VisibleNodes, node => node.IsItem);

        viewModel.RenameNodeCommand.Execute(itemNode);
        await Task.Yield();

        Assert.True(renamed);
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
        Assert.Single(viewModel.ContentNodes);

        viewModel.ClearSearchCommand.Execute(null);

        Assert.False(viewModel.HasSearchText);
        Assert.Equal(2, viewModel.SearchResultCount);
        Assert.Equal(2, viewModel.ContentNodes.Count);
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

    private sealed class TestDialogService(string? response = null) : IDialogService
    {
        public Task<bool> ConfirmAsync(string message, string title) => Task.FromResult(false);
        public Task<bool?> ConfirmWithCancelAsync(string message, string title) =>
            Task.FromResult<bool?>(false);
        public Task<string?> AskForNameAsync(string title, string prompt, string? defaultValue = null) =>
            Task.FromResult(response);
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
