using System.Text.Json;
using Common.ApplicationData;
using DesktopAutomation.Application.Interfaces;
using DesktopAutomation.Application.Organization;

namespace DesktopAutomation.Application.Services;

public sealed class LibraryOrganizationService : ILibraryOrganizationService, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    private readonly string _path;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public LibraryOrganizationService() : this(AppPaths.LibraryLayoutFile) { }

    public LibraryOrganizationService(string path)
    {
        _path = path;
    }

    public async Task<LibraryLayout> LoadAsync()
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            return await LoadCoreAsync().ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<LibraryFolder> CreateFolderAsync(LibraryItemKind kind, Guid? parentId, string name)
    {
        var normalizedName = NormalizeName(name);
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            var layout = await LoadCoreAsync().ConfigureAwait(false);
            ValidateParent(layout, kind, parentId);
            EnsureSiblingNameAvailable(layout, kind, parentId, normalizedName, null);
            var folder = new LibraryFolder
            {
                Kind = kind,
                ParentId = parentId,
                Name = normalizedName,
                SortOrder = NextFolderOrder(layout, kind, parentId)
            };
            layout.Folders.Add(folder);
            await SaveCoreAsync(layout).ConfigureAwait(false);
            return folder;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task RenameFolderAsync(Guid folderId, string name)
    {
        var normalizedName = NormalizeName(name);
        await MutateAsync(layout =>
        {
            var folder = FindFolder(layout, folderId);
            EnsureSiblingNameAvailable(layout, folder.Kind, folder.ParentId, normalizedName, folder.Id);
            folder.Name = normalizedName;
        }).ConfigureAwait(false);
    }

    public async Task MoveFolderAsync(Guid folderId, Guid? parentId)
    {
        await MutateAsync(layout =>
        {
            var folder = FindFolder(layout, folderId);
            ValidateParent(layout, folder.Kind, parentId);
            if (parentId == folder.Id || IsDescendant(layout, parentId, folder.Id))
                throw new InvalidOperationException("Ein Ordner kann nicht in sich selbst oder einen Unterordner verschoben werden.");
            EnsureSiblingNameAvailable(layout, folder.Kind, parentId, folder.Name, folder.Id);
            folder.ParentId = parentId;
            folder.SortOrder = NextFolderOrder(layout, folder.Kind, parentId);
        }).ConfigureAwait(false);
    }

    public async Task DeleteFolderAsync(Guid folderId)
    {
        await MutateAsync(layout =>
        {
            var folder = FindFolder(layout, folderId);
            foreach (var child in layout.Folders.Where(candidate => candidate.ParentId == folder.Id))
                child.ParentId = folder.ParentId;
            foreach (var placement in layout.Placements.Where(candidate =>
                         candidate.Kind == folder.Kind && candidate.FolderId == folder.Id))
                placement.FolderId = folder.ParentId;
            layout.Folders.Remove(folder);
        }).ConfigureAwait(false);
    }

    public async Task PlaceItemAsync(LibraryItemKind kind, Guid itemId, Guid? folderId)
    {
        await MutateAsync(layout =>
        {
            ValidateParent(layout, kind, folderId);
            var placement = layout.Placements.FirstOrDefault(candidate =>
                candidate.Kind == kind && candidate.ItemId == itemId);
            if (!folderId.HasValue)
            {
                if (placement != null)
                    layout.Placements.Remove(placement);
                return;
            }
            if (placement == null)
            {
                placement = new LibraryPlacement { Kind = kind, ItemId = itemId };
                layout.Placements.Add(placement);
            }
            placement.FolderId = folderId;
            placement.SortOrder = NextItemOrder(layout, kind, folderId, itemId);
        }).ConfigureAwait(false);
    }

    public async Task RemoveItemAsync(LibraryItemKind kind, Guid itemId)
    {
        await MutateAsync(layout =>
            layout.Placements.RemoveAll(candidate => candidate.Kind == kind && candidate.ItemId == itemId))
            .ConfigureAwait(false);
    }

    private async Task MutateAsync(Action<LibraryLayout> mutation)
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            var layout = await LoadCoreAsync().ConfigureAwait(false);
            mutation(layout);
            await SaveCoreAsync(layout).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<LibraryLayout> LoadCoreAsync()
    {
        if (!File.Exists(_path)) return new LibraryLayout();
        try
        {
            return await ReadLayoutAsync(_path).ConfigureAwait(false);
        }
        catch (JsonException)
        {
            return await TryReadBackupAsync().ConfigureAwait(false);
        }
        catch (IOException)
        {
            return await TryReadBackupAsync().ConfigureAwait(false);
        }
    }

    private async Task<LibraryLayout> TryReadBackupAsync()
    {
        var backupPath = _path + ".bak";
        if (!File.Exists(backupPath)) return new LibraryLayout();
        try
        {
            return await ReadLayoutAsync(backupPath).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is JsonException or IOException)
        {
            return new LibraryLayout();
        }
    }

    private static async Task<LibraryLayout> ReadLayoutAsync(string path)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var layout = await JsonSerializer.DeserializeAsync<LibraryLayout>(stream, JsonOptions).ConfigureAwait(false)
                     ?? new LibraryLayout();
        layout.Folders ??= [];
        layout.Placements ??= [];
        Normalize(layout);
        return layout;
    }

    private async Task SaveCoreAsync(LibraryLayout layout)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var temporaryPath = _path + ".tmp";
        var backupPath = _path + ".bak";
        await using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await JsonSerializer.SerializeAsync(stream, layout, JsonOptions).ConfigureAwait(false);
            await stream.FlushAsync().ConfigureAwait(false);
        }
        if (File.Exists(_path))
            File.Replace(temporaryPath, _path, backupPath, ignoreMetadataErrors: true);
        else
            File.Move(temporaryPath, _path);
    }

    private static void Normalize(LibraryLayout layout)
    {
        layout.Folders = layout.Folders
            .Where(folder => folder.Id != Guid.Empty && !string.IsNullOrWhiteSpace(folder.Name))
            .GroupBy(folder => folder.Id)
            .Select(group => group.First())
            .ToList();
        var folderIds = layout.Folders.Select(folder => folder.Id).ToHashSet();
        foreach (var folder in layout.Folders)
        {
            folder.Name = folder.Name.Trim();
            if (folder.ParentId == folder.Id ||
                folder.ParentId.HasValue && !folderIds.Contains(folder.ParentId.Value))
                folder.ParentId = null;
        }
        foreach (var folder in layout.Folders)
        {
            var visited = new HashSet<Guid> { folder.Id };
            var parentId = folder.ParentId;
            while (parentId.HasValue)
            {
                if (!visited.Add(parentId.Value))
                {
                    folder.ParentId = null;
                    break;
                }
                parentId = layout.Folders.FirstOrDefault(candidate => candidate.Id == parentId.Value)?.ParentId;
            }
        }
        var validFolders = layout.Folders.ToDictionary(folder => folder.Id);
        foreach (var placement in layout.Placements)
        {
            if (placement.FolderId.HasValue &&
                (!validFolders.TryGetValue(placement.FolderId.Value, out var folder) || folder.Kind != placement.Kind))
                placement.FolderId = null;
        }
        layout.Placements = layout.Placements
            .Where(placement => placement.ItemId != Guid.Empty && placement.FolderId.HasValue)
            .GroupBy(placement => (placement.Kind, placement.ItemId))
            .Select(group => group.First())
            .ToList();
    }

    private static string NormalizeName(string name)
    {
        var normalized = name?.Trim() ?? string.Empty;
        if (normalized.Length == 0) throw new ArgumentException("Der Ordnername darf nicht leer sein.", nameof(name));
        return normalized;
    }

    private static LibraryFolder FindFolder(LibraryLayout layout, Guid id)
        => layout.Folders.FirstOrDefault(folder => folder.Id == id)
           ?? throw new InvalidOperationException("Der Ordner existiert nicht mehr.");

    private static void ValidateParent(LibraryLayout layout, LibraryItemKind kind, Guid? parentId)
    {
        if (!parentId.HasValue) return;
        var parent = FindFolder(layout, parentId.Value);
        if (parent.Kind != kind)
            throw new InvalidOperationException("Ordner unterschiedlicher Bibliotheken können nicht gemischt werden.");
    }

    private static void EnsureSiblingNameAvailable(
        LibraryLayout layout, LibraryItemKind kind, Guid? parentId, string name, Guid? excludedId)
    {
        if (layout.Folders.Any(folder => folder.Kind == kind && folder.ParentId == parentId &&
                                         folder.Id != excludedId &&
                                         string.Equals(folder.Name, name, StringComparison.CurrentCultureIgnoreCase)))
            throw new InvalidOperationException("In diesem Ordner existiert bereits ein Ordner mit diesem Namen.");
    }

    private static bool IsDescendant(LibraryLayout layout, Guid? candidateId, Guid folderId)
    {
        var visited = new HashSet<Guid>();
        while (candidateId.HasValue && visited.Add(candidateId.Value))
        {
            if (candidateId.Value == folderId) return true;
            candidateId = layout.Folders.FirstOrDefault(folder => folder.Id == candidateId.Value)?.ParentId;
        }
        return false;
    }

    private static int NextFolderOrder(LibraryLayout layout, LibraryItemKind kind, Guid? parentId)
        => layout.Folders.Where(folder => folder.Kind == kind && folder.ParentId == parentId)
            .Select(folder => folder.SortOrder).DefaultIfEmpty(-1).Max() + 1;

    private static int NextItemOrder(LibraryLayout layout, LibraryItemKind kind, Guid? folderId, Guid excludedItemId)
        => layout.Placements.Where(item => item.Kind == kind && item.FolderId == folderId && item.ItemId != excludedItemId)
            .Select(item => item.SortOrder).DefaultIfEmpty(-1).Max() + 1;

    public void Dispose() => _gate.Dispose();
}
