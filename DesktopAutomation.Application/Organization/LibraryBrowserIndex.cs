namespace DesktopAutomation.Application.Organization;

public sealed record LibraryBrowserItem(Guid Id, string Name, string Description);

/// <summary>Reusable navigation and search over the existing, normalized library layout.</summary>
public sealed class LibraryBrowserIndex
{
    private readonly Dictionary<Guid, LibraryFolder> _folders;
    private readonly Dictionary<Guid, Guid?> _placements;

    public LibraryBrowserIndex(LibraryLayout layout, LibraryItemKind kind)
    {
        _folders = layout.Folders.Where(folder => folder.Kind == kind).ToDictionary(folder => folder.Id);
        _placements = layout.Placements.Where(item => item.Kind == kind)
            .GroupBy(item => item.ItemId).ToDictionary(group => group.Key, group => group.First().FolderId);
    }

    public Guid? ExistingFolder(Guid? id) => id.HasValue && _folders.ContainsKey(id.Value) ? id : null;

    public Guid? ContainingFolder(Guid itemId) => ExistingFolder(_placements.GetValueOrDefault(itemId));

    public IReadOnlyList<LibraryFolder> PathTo(Guid? folderId)
    {
        var path = new List<LibraryFolder>();
        var seen = new HashSet<Guid>();
        while (folderId.HasValue && seen.Add(folderId.Value) && _folders.TryGetValue(folderId.Value, out var folder))
        {
            path.Add(folder);
            folderId = folder.ParentId;
        }
        path.Reverse();
        return path;
    }

    // All items includes nested folders; selecting a folder shows its direct contents.
    public IReadOnlyList<Guid> Query(IEnumerable<LibraryBrowserItem> items, Guid? folderId, string search, bool descending)
    {
        folderId = ExistingFolder(folderId);
        var query = search.Trim();
        var matches = items.Where(item => (!folderId.HasValue || ContainingFolder(item.Id) == folderId) &&
            (query.Length == 0 || item.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
             item.Description.Contains(query, StringComparison.CurrentCultureIgnoreCase)));
        return (descending
            ? matches.OrderByDescending(item => item.Name, StringComparer.CurrentCultureIgnoreCase).ThenBy(item => item.Id)
            : matches.OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase).ThenBy(item => item.Id))
            .Select(item => item.Id).ToArray();
    }
}
