using System.Text.Json.Nodes;

namespace TaskAutomation.Steps;

internal static class StepDraftValueOverlay
{
    public static bool TrySet(JsonNode root, string path, JsonNode? value)
    {
        var current = root;
        var segments = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
        for (var index = 0; index < segments.Length; index++)
        {
            if (current is JsonArray arrayValue
                && int.TryParse(segments[index], out var arrayIndex)
                && arrayIndex >= 0 && arrayIndex < arrayValue.Count)
            {
                if (index == segments.Length - 1)
                {
                    arrayValue[arrayIndex] = value?.DeepClone();
                    return true;
                }
                if (arrayValue[arrayIndex] is not { } arrayChild) return false;
                current = arrayChild;
                continue;
            }
            if (current is not JsonObject objectValue) return false;
            var property = objectValue.FirstOrDefault(candidate =>
                Normalize(candidate.Key) == Normalize(segments[index])).Key;
            if (string.IsNullOrEmpty(property)) return false;
            if (index == segments.Length - 1)
            {
                objectValue[property] = value?.DeepClone();
                return true;
            }
            if (objectValue[property] is not { } child) return false;
            current = child;
        }
        return false;
    }

    private static string Normalize(string value) =>
        new(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
}
