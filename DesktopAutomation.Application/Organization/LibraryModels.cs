using System.Text.Json.Serialization;

namespace DesktopAutomation.Application.Organization;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LibraryItemKind
{
    Job,
    Makro,
    Automation
}

public sealed class LibraryFolder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public LibraryItemKind Kind { get; set; }
    public Guid? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public sealed class LibraryPlacement
{
    public LibraryItemKind Kind { get; set; }
    public Guid ItemId { get; set; }
    public Guid? FolderId { get; set; }
    public int SortOrder { get; set; }
}

public sealed class LibraryLayout
{
    public const int CurrentFormatVersion = 1;

    public int FormatVersion { get; set; } = CurrentFormatVersion;
    public List<LibraryFolder> Folders { get; set; } = [];
    public List<LibraryPlacement> Placements { get; set; } = [];
}
