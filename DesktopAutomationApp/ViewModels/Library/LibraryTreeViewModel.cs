using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using DesktopAutomation.Application.Interfaces;
using DesktopAutomation.Application.Organization;
using DesktopAutomationApp.Localization;
using DesktopAutomationApp.Settings;

namespace DesktopAutomationApp.ViewModels.Library;

internal sealed class ResettableObservableCollection<T> : ObservableCollection<T>
{
    public void ReplaceAll(IEnumerable<T> items)
    {
        Items.Clear();
        foreach (var item in items)
            Items.Add(item);

        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
}

public sealed class LibraryItemDescriptor
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public string Subtitle { get; init; } = string.Empty;
    public required object Model { get; init; }
    public required Action Open { get; init; }
    public Action? Execute { get; init; }
    public Action? Stop { get; init; }
    public Func<bool>? IsRunning { get; init; }
    public Func<bool>? CanExecute { get; init; }
    public Func<Task<bool>>? DeleteAsync { get; init; }
}

public sealed class LibraryTreeNodeViewModel : ViewModelBase
{
    private readonly LibraryTreeViewModel _owner;
    private bool _isExpanded;
    private bool _isDragging;
    private bool _isDropGroupMember;
    private bool _isDropGroupFirst;
    private bool _isDropGroupLast;

    internal LibraryTreeNodeViewModel(
        LibraryTreeViewModel owner,
        LibraryFolder folder,
        int depth,
        bool isExpanded)
    {
        _owner = owner;
        Folder = folder;
        Depth = depth;
        _isExpanded = isExpanded;
    }

    internal LibraryTreeNodeViewModel(
        LibraryTreeViewModel owner,
        LibraryItemDescriptor item,
        Guid? folderId,
        int depth)
    {
        _owner = owner;
        Item = item;
        FolderId = folderId;
        Depth = depth;
    }

    public LibraryFolder? Folder { get; }
    public LibraryItemDescriptor? Item { get; }
    public Guid? FolderId { get; }
    public bool IsFolder => Folder != null;
    public bool IsItem => Item != null;
    public bool CanMoveUpOneLevel => Folder?.ParentId != null || Item != null && FolderId != null;
    public Guid Id => Folder?.Id ?? Item!.Id;
    public string Name => Folder?.Name ?? Item?.Name ?? string.Empty;
    public string Subtitle => Item?.Subtitle ?? string.Empty;
    public int Depth { get; }
    public double Indent => Depth * 22d;
    public bool HasSubtitle => !string.IsNullOrWhiteSpace(Subtitle);
    public bool CanExecute => Item?.Execute != null && (Item.CanExecute?.Invoke() ?? true);
    public bool IsRunning => Item?.IsRunning?.Invoke() ?? false;
    public bool IsDragging
    {
        get => _isDragging;
        internal set
        {
            if (_isDragging == value) return;
            _isDragging = value;
            OnPropertyChanged();
        }
    }
    public bool IsDropGroupMember
    {
        get => _isDropGroupMember;
        internal set
        {
            if (_isDropGroupMember == value) return;
            _isDropGroupMember = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DropBorderThickness));
            OnPropertyChanged(nameof(DropCornerRadius));
        }
    }
    internal bool IsDropGroupFirst
    {
        get => _isDropGroupFirst;
        set
        {
            if (_isDropGroupFirst == value) return;
            _isDropGroupFirst = value;
            OnPropertyChanged(nameof(DropBorderThickness));
            OnPropertyChanged(nameof(DropCornerRadius));
        }
    }
    internal bool IsDropGroupLast
    {
        get => _isDropGroupLast;
        set
        {
            if (_isDropGroupLast == value) return;
            _isDropGroupLast = value;
            OnPropertyChanged(nameof(DropBorderThickness));
            OnPropertyChanged(nameof(DropCornerRadius));
        }
    }
    public Thickness DropBorderThickness => !IsDropGroupMember
        ? new Thickness(1)
        : new Thickness(2, IsDropGroupFirst ? 2 : 0, 2, IsDropGroupLast ? 2 : 0);
    public CornerRadius DropCornerRadius => !IsDropGroupMember
        ? new CornerRadius(5)
        : new CornerRadius(
            IsDropGroupFirst ? 6 : 0,
            IsDropGroupFirst ? 6 : 0,
            IsDropGroupLast ? 6 : 0,
            IsDropGroupLast ? 6 : 0);

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value) return;
            _isExpanded = value;
            OnPropertyChanged();
            _owner.OnExpansionChanged(this);
        }
    }

    public void OpenOrToggle()
    {
        if (Folder != null)
        {
            _owner.SelectedFolderId = Folder.Id;
            IsExpanded = !IsExpanded;
        }
        else
        {
            Item?.Open();
        }
    }

    internal void RefreshState()
    {
        OnPropertyChanged(nameof(IsRunning));
        OnPropertyChanged(nameof(CanExecute));
    }
}

public sealed class LibraryTreeViewModel : ViewModelBase
{
    private readonly ILibraryOrganizationService _organization;
    private readonly IDialogService _dialogs;
    private readonly IUserPreferencesService _preferences;
    private readonly LibraryItemKind _kind;
    private IReadOnlyList<LibraryItemDescriptor> _items = [];
    private LibraryLayout _layout = new();
    private string _searchText = string.Empty;
    private bool _suppressExpansionSave;
    private bool _isDragActive;
    private bool _isRootDropTarget;
    private string _draggedItemName = string.Empty;
    private readonly ResettableObservableCollection<LibraryTreeNodeViewModel> _visibleNodes = [];
    private readonly List<LibraryTreeNodeViewModel> _dropGroupNodes = [];
    private readonly Dictionary<Guid, int> _visibleFolderIndexes = [];
    private LibraryTreeNodeViewModel? _draggedNode;
    private Guid? _dropTargetFolderId;

    public LibraryTreeViewModel(
        ILibraryOrganizationService organization,
        IDialogService dialogs,
        IUserPreferencesService preferences,
        LibraryItemKind kind,
        string newItemLabel)
    {
        _organization = organization;
        _dialogs = dialogs;
        _preferences = preferences;
        _kind = kind;
        NewItemLabel = newItemLabel;

        OpenNodeCommand = new RelayCommand<LibraryTreeNodeViewModel?>(node => node?.OpenOrToggle());
        ExecuteNodeCommand = new RelayCommand<LibraryTreeNodeViewModel?>(node =>
        {
            if (node?.Item == null) return;
            if (node.IsRunning) node.Item.Stop?.Invoke();
            else node.Item.Execute?.Invoke();
            node.RefreshState();
        }, node => node?.IsItem == true && (node.IsRunning || node.CanExecute));
        NewFolderCommand = new AsyncRelayCommand(CreateFolderAsync);
        NewSubfolderCommand = new AsyncRelayCommand<LibraryTreeNodeViewModel?>(
            node => CreateFolderAsync(node?.Folder?.Id), node => node?.IsFolder == true);
        NewItemInFolderCommand = new AsyncRelayCommand<LibraryTreeNodeViewModel?>(
            CreateItemInFolderAsync, node => node?.IsFolder == true);
        RenameFolderCommand = new AsyncRelayCommand<LibraryTreeNodeViewModel?>(RenameFolderAsync, node => node?.IsFolder == true);
        DeleteNodeCommand = new AsyncRelayCommand<LibraryTreeNodeViewModel?>(DeleteNodeAsync, node => node != null);
        MoveUpOneLevelCommand = new AsyncRelayCommand<LibraryTreeNodeViewModel?>(
            MoveUpOneLevelAsync,
            node => node?.CanMoveUpOneLevel == true);
        ExpandAllCommand = new RelayCommand(() => SetAllExpanded(true));
        CollapseAllCommand = new RelayCommand(() => SetAllExpanded(false));
    }

    public ObservableCollection<LibraryTreeNodeViewModel> VisibleNodes => _visibleNodes;
    public Guid? SelectedFolderId { get; set; }
    public string NewItemLabel { get; }
    public event Func<Guid?, Task>? RequestCreateItem;
    public ICommand OpenNodeCommand { get; }
    public ICommand ExecuteNodeCommand { get; }
    public ICommand NewFolderCommand { get; }
    public ICommand NewSubfolderCommand { get; }
    public ICommand NewItemInFolderCommand { get; }
    public ICommand RenameFolderCommand { get; }
    public ICommand DeleteNodeCommand { get; }
    public ICommand MoveUpOneLevelCommand { get; }
    public ICommand ExpandAllCommand { get; }
    public ICommand CollapseAllCommand { get; }
    public bool HasItems => VisibleNodes.Count > 0;
    public bool IsDragActive
    {
        get => _isDragActive;
        private set { _isDragActive = value; OnPropertyChanged(); }
    }
    public bool IsRootDropTarget
    {
        get => _isRootDropTarget;
        set
        {
            if (_isRootDropTarget == value) return;
            _isRootDropTarget = value;
            OnPropertyChanged();
        }
    }
    public string DraggedItemName
    {
        get => _draggedItemName;
        private set { _draggedItemName = value; OnPropertyChanged(); }
    }
    public string EmptyText => string.IsNullOrWhiteSpace(SearchText)
        ? Loc.Get("Ui.Library.Empty")
        : Loc.Get("Ui.Library.NoSearchResults");

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText == value) return;
            _searchText = value ?? string.Empty;
            OnPropertyChanged();
            Rebuild();
        }
    }

    public async Task SetItemsAsync(IEnumerable<LibraryItemDescriptor> items)
    {
        _items = items.ToArray();
        _layout = await _organization.LoadAsync();
        Rebuild();
    }

    public void RefreshItemStates()
    {
        foreach (var node in VisibleNodes)
            node.RefreshState();
        (ExecuteNodeCommand as RelayCommand<LibraryTreeNodeViewModel?>)?.RaiseCanExecuteChanged();
    }

    public void BeginDrag(LibraryTreeNodeViewModel node)
    {
        ClearDropTargets();
        if (_draggedNode != null)
            _draggedNode.IsDragging = false;
        _draggedNode = node;
        node.IsDragging = true;
        DraggedItemName = node.Name;
        IsDragActive = true;
    }

    public void SetDropTarget(LibraryTreeNodeViewModel? node)
    {
        if (node == null)
        {
            ClearDropTargets();
            return;
        }
        var folderId = node.Folder?.Id ?? node.FolderId;
        if (!folderId.HasValue)
        {
            SetRootDropTarget();
            return;
        }

        if (_dropTargetFolderId == folderId && _dropGroupNodes.Count > 0)
            return;

        ClearDropTargets();
        if (!_visibleFolderIndexes.TryGetValue(folderId.Value, out var folderIndex))
            return;
        var folderNode = VisibleNodes.ElementAtOrDefault(folderIndex);
        if (folderNode?.Folder == null) return;
        var endIndex = folderIndex + 1;
        while (endIndex < VisibleNodes.Count && VisibleNodes[endIndex].Depth > folderNode.Depth)
            endIndex++;
        for (var index = folderIndex; index < endIndex; index++)
        {
            var groupNode = VisibleNodes[index];
            groupNode.IsDropGroupMember = true;
            groupNode.IsDropGroupFirst = index == folderIndex;
            groupNode.IsDropGroupLast = index == endIndex - 1;
            _dropGroupNodes.Add(groupNode);
        }
        _dropTargetFolderId = folderId;
    }

    public void SetRootDropTarget()
    {
        if (IsRootDropTarget && _dropGroupNodes.Count == 0)
            return;
        ClearDropTargets();
        IsRootDropTarget = true;
    }

    public void EndDrag()
    {
        if (_draggedNode != null)
            _draggedNode.IsDragging = false;
        _draggedNode = null;
        ClearDropTargets();
        IsDragActive = false;
        DraggedItemName = string.Empty;
    }

    private void ClearDropTargets()
    {
        foreach (var node in _dropGroupNodes)
        {
            node.IsDropGroupMember = false;
            node.IsDropGroupFirst = false;
            node.IsDropGroupLast = false;
        }
        _dropGroupNodes.Clear();
        _dropTargetFolderId = null;
        IsRootDropTarget = false;
    }

    public async Task MoveNodeAsync(LibraryTreeNodeViewModel? node, Guid? targetFolderId)
    {
        if (node == null) return;
        if (node.Folder is { } folder)
        {
            if (folder.Id == targetFolderId || folder.ParentId == targetFolderId)
                return;
        }
        else if (node.Item != null && node.FolderId == targetFolderId)
        {
            return;
        }
        try
        {
            if (node.Folder != null)
                await _organization.MoveFolderAsync(node.Folder.Id, targetFolderId);
            else if (node.Item != null)
                await _organization.PlaceItemAsync(_kind, node.Item.Id, targetFolderId);
            _layout = await _organization.LoadAsync();
            Rebuild();
        }
        catch (InvalidOperationException exception)
        {
            _dialogs.ShowError(exception.Message, Loc.Get("Ui.Library.OperationFailed"));
        }
    }

    public Task MoveUpOneLevelAsync(LibraryTreeNodeViewModel? node)
    {
        var containingFolderId = node?.Folder?.ParentId ?? node?.FolderId;
        if (!containingFolderId.HasValue) return Task.CompletedTask;
        var targetFolderId = _layout.Folders
            .FirstOrDefault(folder => folder.Id == containingFolderId.Value)
            ?.ParentId;
        return MoveNodeAsync(node, targetFolderId);
    }

    internal void OnExpansionChanged(LibraryTreeNodeViewModel node)
    {
        if (_suppressExpansionSave || node.Folder == null || !string.IsNullOrWhiteSpace(SearchText)) return;
        var expanded = GetExpandedFolderIds();
        if (node.IsExpanded) expanded.Add(node.Folder.Id);
        else expanded.Remove(node.Folder.Id);
        _preferences.Current.ExpandedLibraryFolders[_kind.ToString()] = expanded.ToList();
        _ = _preferences.SaveAsync();
        Rebuild();
    }

    private Task CreateFolderAsync() => CreateFolderAsync(SelectedFolderId);

    private async Task CreateFolderAsync(Guid? parentId)
    {
        var name = await _dialogs.AskForNameAsync(
            Loc.Get("Ui.Library.NewFolderTitle"),
            Loc.Get("Ui.Library.NewFolderPrompt"));
        if (string.IsNullOrWhiteSpace(name)) return;
        try
        {
            var folder = await _organization.CreateFolderAsync(_kind, parentId, name);
            GetExpandedFolderIds().Add(folder.ParentId ?? Guid.Empty);
            _layout = await _organization.LoadAsync();
            Rebuild();
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            _dialogs.ShowError(exception.Message, Loc.Get("Ui.Library.OperationFailed"));
        }
    }

    private async Task CreateItemInFolderAsync(LibraryTreeNodeViewModel? node)
    {
        if (node?.Folder == null || RequestCreateItem == null) return;
        await RequestCreateItem.Invoke(node.Folder.Id);
    }

    private async Task RenameFolderAsync(LibraryTreeNodeViewModel? node)
    {
        if (node?.Folder == null) return;
        var name = await _dialogs.AskForNameAsync(
            Loc.Get("Ui.Library.RenameFolderTitle"),
            Loc.Get("Ui.Library.RenameFolderPrompt"),
            node.Folder.Name);
        if (string.IsNullOrWhiteSpace(name)) return;
        try
        {
            await _organization.RenameFolderAsync(node.Folder.Id, name);
            _layout = await _organization.LoadAsync();
            Rebuild();
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            _dialogs.ShowError(exception.Message, Loc.Get("Ui.Library.OperationFailed"));
        }
    }

    private async Task DeleteNodeAsync(LibraryTreeNodeViewModel? node)
    {
        if (node == null) return;
        if (node.Folder != null)
        {
            var confirmed = await _dialogs.ConfirmAsync(
                Loc.Format("Ui.Library.DeleteFolderPrompt", node.Name),
                Loc.Get("Ui.Library.DeleteFolderTitle"));
            if (!confirmed) return;
            if (SelectedFolderId == node.Folder.Id)
                SelectedFolderId = node.Folder.ParentId;
            await _organization.DeleteFolderAsync(node.Folder.Id);
        }
        else if (node.Item?.DeleteAsync != null)
        {
            if (!await node.Item.DeleteAsync()) return;
            await _organization.RemoveItemAsync(_kind, node.Item.Id);
            _items = _items.Where(item => item.Id != node.Item.Id).ToArray();
        }
        _layout = await _organization.LoadAsync();
        Rebuild();
    }

    private void SetAllExpanded(bool expanded)
    {
        _suppressExpansionSave = true;
        try
        {
            var ids = GetExpandedFolderIds();
            ids.Clear();
            if (expanded)
                foreach (var folder in _layout.Folders.Where(folder => folder.Kind == _kind))
                    ids.Add(folder.Id);
            _preferences.Current.ExpandedLibraryFolders[_kind.ToString()] = ids.ToList();
            _ = _preferences.SaveAsync();
        }
        finally
        {
            _suppressExpansionSave = false;
        }
        Rebuild();
    }

    private HashSet<Guid> GetExpandedFolderIds()
    {
        if (!_preferences.Current.ExpandedLibraryFolders.TryGetValue(_kind.ToString(), out var ids))
        {
            ids = [];
            _preferences.Current.ExpandedLibraryFolders[_kind.ToString()] = ids;
        }
        return ids.ToHashSet();
    }

    private void Rebuild()
    {
        var query = SearchText.Trim();
        var expanded = GetExpandedFolderIds();
        var folders = _layout.Folders.Where(folder => folder.Kind == _kind).ToList();
        var foldersById = folders.ToDictionary(folder => folder.Id);
        var foldersByParent = folders
            .GroupBy(folder => FolderKey(folder.ParentId))
            .ToDictionary(group => group.Key, group =>
                group.OrderBy(folder => folder.Name, StringComparer.CurrentCultureIgnoreCase).ToList());
        var placements = _layout.Placements.Where(placement => placement.Kind == _kind)
            .ToDictionary(placement => placement.ItemId, placement => placement);
        static Guid FolderKey(Guid? folderId) => folderId ?? Guid.Empty;
        var itemsByFolder = new Dictionary<Guid, List<LibraryItemDescriptor>>();
        foreach (var item in _items)
        {
            var folderId = placements.TryGetValue(item.Id, out var placement) &&
                           placement.FolderId.HasValue &&
                           foldersById.ContainsKey(placement.FolderId.Value)
                ? placement.FolderId
                : null;
            var key = FolderKey(folderId);
            if (!itemsByFolder.TryGetValue(key, out var items))
            {
                items = [];
                itemsByFolder[key] = items;
            }
            items.Add(item);
        }
        foreach (var items in itemsByFolder.Values)
            items.Sort((left, right) =>
                StringComparer.CurrentCultureIgnoreCase.Compare(left.Name, right.Name));

        bool Matches(LibraryItemDescriptor item) =>
            query.Length == 0 ||
            item.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
            item.Subtitle.Contains(query, StringComparison.CurrentCultureIgnoreCase);

        var folderMatchCache = new Dictionary<Guid, bool>();
        bool FolderHasMatch(Guid folderId)
        {
            if (folderMatchCache.TryGetValue(folderId, out var cached))
                return cached;
            var matches = foldersById[folderId].Name.Contains(
                              query, StringComparison.CurrentCultureIgnoreCase) ||
                          itemsByFolder.GetValueOrDefault(folderId)?.Any(Matches) == true ||
                          foldersByParent.GetValueOrDefault(folderId)?.Any(child => FolderHasMatch(child.Id)) == true;
            folderMatchCache[folderId] = matches;
            return matches;
        }

        var result = new List<LibraryTreeNodeViewModel>();
        void AddLevel(Guid? parentId, int depth)
        {
            if (foldersByParent.TryGetValue(FolderKey(parentId), out var childFolders))
            {
                foreach (var folder in childFolders)
                {
                    if (query.Length > 0 && !FolderHasMatch(folder.Id)) continue;
                    var isExpanded = query.Length > 0 || expanded.Contains(folder.Id);
                    result.Add(new LibraryTreeNodeViewModel(this, folder, depth, isExpanded));
                    if (isExpanded) AddLevel(folder.Id, depth + 1);
                }
            }

            if (!itemsByFolder.TryGetValue(FolderKey(parentId), out var levelItems)) return;
            foreach (var item in levelItems.Where(Matches))
                result.Add(new LibraryTreeNodeViewModel(this, item, parentId, depth));
        }

        AddLevel(null, 0);
        _visibleFolderIndexes.Clear();
        for (var index = 0; index < result.Count; index++)
        {
            if (result[index].Folder is { } folder)
                _visibleFolderIndexes[folder.Id] = index;
        }
        _visibleNodes.ReplaceAll(result);
        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(EmptyText));
    }
}
