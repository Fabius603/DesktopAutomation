using TaskAutomation.Jobs;

namespace TaskAutomation.Steps;

public sealed record ResultPropertyDescriptor(
    string Name,
    string DisplayName,
    ResultValueKind DataType,
    string? Description = null,
    bool IsNullable = false,
    string? Example = null,
    ResultCardinality Cardinality = ResultCardinality.Single,
    string? EnumTypeName = null,
    IReadOnlyList<string>? EnumValues = null,
    string? Id = null,
    IReadOnlyDictionary<string, string>? EnumDisplayNames = null,
    bool IsDiagnostic = false)
{
    public string StableId => string.IsNullOrWhiteSpace(Id)
        ? ResultContractIds.FromPropertyPath(Name)
        : Id;
}

public static class ResultContractIds
{
    public static string FromPropertyPath(string path)
    {
        var result = new System.Text.StringBuilder(path.Length + 8);
        foreach (var character in path.Replace("[]", string.Empty, StringComparison.Ordinal))
        {
            if (character == '.')
            {
                if (result.Length > 0 && result[^1] != '.') result.Append('.');
                continue;
            }
            if (char.IsUpper(character) && result.Length > 0 && result[^1] is not '.' and not '_')
                result.Append('_');
            result.Append(char.ToLowerInvariant(character));
        }
        return result.ToString();
    }
}

