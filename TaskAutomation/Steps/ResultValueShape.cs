using System.Collections;
using System.Text.Json.Nodes;
using TaskAutomation.Contracts.Geometry;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps;

internal static class ResultValueShape
{
    public static ResultCardinality Cardinality(object? value) => value is IEnumerable and not string and not JsonObject
        ? ResultCardinality.Collection : value is null ? ResultCardinality.OptionalSingle : ResultCardinality.Single;

    public static ResultValueKind Kind(object? value)
    {
        if (value is JsonValue json) return Kind(ResultBindingResolver.Unwrap(json));
        if (value is IEnumerable items and not string and not JsonObject)
        {
            var kinds = items.Cast<object?>().Where(item => item is not null).Select(Kind).Distinct().ToArray();
            return kinds.Length == 1 ? kinds[0]
                : kinds.Length > 0 && kinds.All(kind => kind is ResultValueKind.Integer or ResultValueKind.Number)
                    ? ResultValueKind.Number : ResultValueKind.ResultObject;
        }
        return value switch
        {
            bool => ResultValueKind.Boolean,
            byte or short or int or long => ResultValueKind.Integer,
            float or double or decimal => ResultValueKind.Number,
            DateTime => ResultValueKind.DateTime,
            string => ResultValueKind.Text,
            PixelPoint => ResultValueKind.Point,
            PixelRegion => ResultValueKind.Rectangle,
            DetectionItem => ResultValueKind.Detection,
            RuntimeProcessReference => ResultValueKind.ProcessReference,
            System.Drawing.Bitmap => ResultValueKind.Image,
            _ => ResultValueKind.ResultObject
        };
    }
}
