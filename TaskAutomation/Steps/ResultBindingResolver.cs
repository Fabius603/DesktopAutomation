using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using TaskAutomation.Jobs;
using TaskAutomation.Contracts.Geometry;

namespace TaskAutomation.Steps;

public enum ResultResolutionStatus
{
    Success,
    NotConfigured,
    SourceNotExecuted,
    PropertyNotFound,
    ValueIsNull,
    EmptyCollection,
    TypeMismatch
}

public sealed record ResolvedResultValue<T>(
    ResultResolutionStatus Status,
    IReadOnlyList<T> Values,
    StepResultBase? SourceResult,
    string? Error = null)
{
    public bool IsSuccess => Status == ResultResolutionStatus.Success;
    public T? FirstOrDefault => Values.Count == 0 ? default : Values[0];
}

/// <summary>Single execution-time implementation for every persisted result-property binding.</summary>
public static class ResultBindingResolver
{
    public static bool IsExpectedEmpty(ResultResolutionStatus status) => status is
        ResultResolutionStatus.ValueIsNull or ResultResolutionStatus.EmptyCollection;

    public static ResolvedResultValue<T> Resolve<T>(IJobResultStore results, ResultBinding? binding)
    {
        if (binding?.IsConfigured != true)
            return Failure<T>(ResultResolutionStatus.NotConfigured, null, "Keine Ergebnis-Eigenschaft ausgewählt.");

        var read = ValueReferenceResolver.Resolve(results, binding);
        var source = binding.TryGetStepResult(out var reference) ? results.GetRaw(reference.StepId) : null;
        if (!read.IsSuccess) return Failure<T>(read.Status == RuntimeValueReadStatus.PropertyUnavailable
            ? ResultResolutionStatus.PropertyNotFound : ResultResolutionStatus.SourceNotExecuted, source, read.Error ?? "StepValidation.Invalid");
        var raw = read.Value;
        if (raw is null)
            return Failure<T>(ResultResolutionStatus.ValueIsNull, source,
                $"Die Eigenschaft '{binding.PropertyPath}' enthält keinen Wert.");

        if (raw is JsonValue jsonValue) raw = Unwrap(jsonValue)!;
        var projected = Flatten(raw).Select(value => ProjectNumber<T>(value, read.Descriptor?.ValueKind)).ToArray();
        if (projected.Any(value => value is not T))
            return Failure<T>(ResultResolutionStatus.TypeMismatch, source,
                $"Die Eigenschaft '{binding.PropertyPath}' ist nicht vom erwarteten Typ {typeof(T).Name}.");
        var values = projected.OfType<T>().ToArray();
        if (values.Length > 0)
            return new(ResultResolutionStatus.Success, values, source);
        if (raw is IEnumerable and not string)
            return Failure<T>(ResultResolutionStatus.EmptyCollection, source,
                $"Die Eigenschaft '{binding.PropertyPath}' enthält keine passenden Werte.");
        return Failure<T>(ResultResolutionStatus.TypeMismatch, source,
            $"Die Eigenschaft '{binding.PropertyPath}' ist nicht vom erwarteten Typ {typeof(T).Name}.");
    }

    public static (ICaptureStepResult Capture, System.Drawing.Bitmap? Image, ResolvedResultValue<System.Drawing.Bitmap> Resolution)
        ResolveCapture(IJobResultStore results, ResultBinding? binding)
    {
        var resolution = Resolve<System.Drawing.Bitmap>(results, binding);
        return (resolution.SourceResult as ICaptureStepResult ?? CaptureFrame.Default,
            resolution.FirstOrDefault, resolution);
    }

    public static ResolvedResultValue<PixelPoint> ResolvePoints(
        IJobResultStore results, ResultBinding? binding) => Resolve<PixelPoint>(results, binding);

    public static ResolvedResultValue<DetectionItem> ResolveDetections(
        IJobResultStore results, ResultBinding? binding)
    {
        var items = Resolve<DetectionItem>(results, binding);
        if (items.IsSuccess) return items;

        var points = Resolve<PixelPoint>(results, binding);
        if (points.IsSuccess)
        {
            var pointSource = points.SourceResult as IDetectionStepResult;
            return new(ResultResolutionStatus.Success,
                points.Values.Select((point, index) => new DetectionItem
                {
                    Center = point,
                    BoundingBox = pointSource?.AllDetections.ElementAtOrDefault(index)?.BoundingBox
                                  ?? (index == 0 ? pointSource?.BoundingBox : null),
                    Confidence = pointSource?.AllDetections.ElementAtOrDefault(index)?.Confidence
                                 ?? pointSource?.Confidence ?? 0
                }).ToArray(), points.SourceResult);
        }

        var rectangles = Resolve<PixelRegion>(results, binding);
        if (!rectangles.IsSuccess)
            return new(rectangles.Status, Array.Empty<DetectionItem>(),
                rectangles.SourceResult, rectangles.Error);

        var rectangleSource = rectangles.SourceResult as IDetectionStepResult;
        return new(ResultResolutionStatus.Success,
            rectangles.Values.Select((rectangle, index) => new DetectionItem
            {
                Center = new PixelPoint(
                    rectangle.Left + rectangle.Width / 2,
                    rectangle.Top + rectangle.Height / 2),
                BoundingBox = rectangle,
                Confidence = rectangleSource?.AllDetections.ElementAtOrDefault(index)?.Confidence
                             ?? rectangleSource?.Confidence ?? 0
            }).ToArray(), rectangles.SourceResult);
    }

    public static bool TryReadPath(object source, string propertyPath, out object? value)
    {
        value = source;
        if (propertyPath == "$" || string.IsNullOrWhiteSpace(propertyPath)) return true;

        foreach (var rawSegment in propertyPath.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            var projectCollection = rawSegment.EndsWith("[]", StringComparison.Ordinal);
            var segment = projectCollection ? rawSegment[..^2] : rawSegment;
            if (value is null) return true;

            if (value is IEnumerable enumerable and not string and not JsonObject)
            {
                var projected = new List<object?>();
                foreach (var item in enumerable)
                    if (TryReadMember(item, segment, out var memberValue)) projected.Add(memberValue);
                    else { value = null; return false; }
                value = projected;
                continue;
            }

            if (!TryReadMember(value, segment, out value)) return false;
            if (projectCollection && value is not IEnumerable) return false;
        }
        return true;
    }

    private static object? ProjectNumber<T>(object? value, ResultValueKind? kind)
    {
        if (value is T || kind != ResultValueKind.Number
            || value is not (byte or short or int or long or float or double or decimal)) return value;
        if (typeof(T) != typeof(double) && typeof(T) != typeof(float) && typeof(T) != typeof(decimal)) return value;
        try { return Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture); }
        catch (OverflowException) { return value; }
    }

    private static IEnumerable<object?> Flatten(object value)
    {
        if (value is JsonValue scalar) { yield return Unwrap(scalar); yield break; }
        if (value is IEnumerable enumerable and not string and not JsonObject)
        {
            foreach (var item in enumerable)
                if (item is not null)
                    foreach (var flattened in Flatten(item)) yield return flattened;
            yield break;
        }
        yield return value;
    }

    private static bool TryReadMember(object? source, string name, out object? value)
    {
        value = null;
        if (source is null) return true;
        if (source is JsonObject jsonObject)
        {
            var property = jsonObject.FirstOrDefault(candidate =>
                candidate.Key.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (property.Key is null) return false;
            value = property.Value is JsonValue jsonValue
                ? Unwrap(jsonValue)
                : property.Value;
            return true;
        }
        var type = source.GetType();
        var member = (MemberInfo?)type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                     ?? type.GetField(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        value = member switch
        {
            PropertyInfo property => property.GetValue(source),
            FieldInfo field => field.GetValue(source),
            _ => null
        };
        return member is not null;
    }

    private static ResolvedResultValue<T> Failure<T>(ResultResolutionStatus status, StepResultBase? source, string error) =>
        new(status, Array.Empty<T>(), source, error);

    internal static object? Unwrap(JsonValue value)
    {
        if (value.TryGetValue<bool>(out var boolean)) return boolean;
        if (value.TryGetValue<int>(out var integer)) return integer;
        if (value.TryGetValue<long>(out var longInteger)) return longInteger;
        if (value.TryGetValue<double>(out var number)) return number;
        if (value.TryGetValue<decimal>(out var decimalNumber)) return decimalNumber;
        if (value.TryGetValue<DateTime>(out var dateTime)) return dateTime;
        if (value.TryGetValue<string>(out var text)) return text;
        return JsonSerializer.Deserialize<object>(value.ToJsonString());
    }
}
