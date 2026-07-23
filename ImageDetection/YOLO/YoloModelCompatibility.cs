using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace ImageDetection.YOLO;

public static class YoloModelCompatibility
{
    private static readonly string[][] EquivalentClassGroups =
    [
        ["Search Field", "Search Bar"]
    ];

    public static int[] ResolveRequestedClassIds(
        IReadOnlyList<string> labels,
        string requestedClass)
    {
        var exactId = FindLabel(labels, requestedClass);
        if (exactId < 0) return [];

        var equivalentGroup = EquivalentClassGroups.FirstOrDefault(group =>
            group.Contains(requestedClass, StringComparer.OrdinalIgnoreCase));
        if (equivalentGroup is null) return [exactId];

        return equivalentGroup
            .Select(label => FindLabel(labels, label))
            .Where(id => id >= 0)
            .Distinct()
            .ToArray();
    }

    public static int ResolveSquareInputSize(
        IReadOnlyDictionary<string, NodeMetadata> inputMetadata,
        int fallbackInputSize)
    {
        if (inputMetadata.Count != 1)
            throw new NotSupportedException(
                $"YOLO-Modelle müssen genau einen Eingang besitzen; gefunden: {inputMetadata.Count}.");

        var metadata = inputMetadata.Values.Single();
        return ResolveSquareInputSize(metadata.Dimensions.ToArray(), metadata.ElementDataType, fallbackInputSize);
    }

    public static int ResolveSquareInputSize(
        int[] dimensions,
        TensorElementType elementDataType,
        int fallbackInputSize)
    {
        if (elementDataType is not TensorElementType.Float and not TensorElementType.Float16)
            throw new NotSupportedException(
                $"Nicht unterstützter YOLO-Eingangstyp: {elementDataType}.");

        if (dimensions.Length != 4 ||
            dimensions[0] is not (1 or -1) ||
            dimensions[1] != 3)
        {
            throw new NotSupportedException(
                $"Unerwartete YOLO-Eingabeform: [{string.Join(",", dimensions)}].");
        }

        var height = dimensions[2];
        var width = dimensions[3];
        if (height > 0 && width > 0)
        {
            if (height != width)
                throw new NotSupportedException(
                    $"Nur quadratische YOLO-Eingaben werden unterstützt; gefunden: {width}x{height}.");

            return height;
        }

        if (fallbackInputSize <= 0)
            throw new NotSupportedException("Das dynamische YOLO-Modell benötigt eine gültige Eingabegröße.");

        return fallbackInputSize;
    }

    public static void ValidateDetectionOutput(int[] dimensions, int labelCount)
    {
        if (labelCount <= 0)
            throw new NotSupportedException("Das YOLO-Modell enthält keine Klassen.");

        if (dimensions.Length != 3 || dimensions[0] != 1)
            throw new NotSupportedException(
                $"Unerwartete YOLO-Ausgabeform: [{string.Join(",", dimensions)}].");

        var attributes = Math.Min(dimensions[1], dimensions[2]);
        var expectedAttributes = 4 + labelCount;
        if (attributes != expectedAttributes)
        {
            throw new NotSupportedException(
                $"Das YOLO-Modell liefert {attributes} Attribute; für {labelCount} Klassen werden {expectedAttributes} erwartet.");
        }
    }

    private static int FindLabel(IReadOnlyList<string> labels, string name)
    {
        for (var index = 0; index < labels.Count; index++)
            if (string.Equals(labels[index], name, StringComparison.OrdinalIgnoreCase))
                return index;
        return -1;
    }
}
