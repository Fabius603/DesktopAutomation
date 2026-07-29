using DesktopAutomation.Application.Organization;

namespace DesktopAutomation.Application.Interfaces;

public interface ILibraryOrganizationService
{
    Task<LibraryLayout> LoadAsync();
    Task<LibraryFolder> CreateFolderAsync(LibraryItemKind kind, Guid? parentId, string name);
    Task RenameFolderAsync(Guid folderId, string name);
    Task MoveFolderAsync(Guid folderId, Guid? parentId);
    Task DeleteFolderAsync(Guid folderId);
    Task PlaceItemAsync(LibraryItemKind kind, Guid itemId, Guid? folderId);
    Task RemoveItemAsync(LibraryItemKind kind, Guid itemId);
}
