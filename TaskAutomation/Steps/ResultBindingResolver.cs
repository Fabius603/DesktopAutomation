using System.Collections;
using System.Reflection;
using TaskAutomation.Jobs;

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
    public static ResolvedResultValue<T> Resolve<T>(IJobResultStore results, ResultBinding? binding)
    {
        if (binding?.IsConfigured != true)
            return Failure<T>(ResultResolutionStatus.NotConfigured, null, "Keine Ergebnis-Eigenschaft ausgewählt.");

        var source = results.GetRaw(binding.SourceStepId);
        if (source is null || !source.WasExecuted)
            return Failure<T>(ResultResolutionStatus.SourceNotExecuted, source,
                $"Der Quell-Step '{binding.SourceStepId}' wurde noch nicht ausgeführt.");

        var propertyPath = binding.PropertyPath;
        if (!string.IsNullOrWhiteSpace(binding.PropertyId)
            && StepResultMetadata.TryGetProperty(
                source.GetType(), binding.PropertyId, binding.PropertyPath, out var property))
            propertyPath = property.Name;

        if (string.IsNullOrWhiteSpace(propertyPath) || !TryReadPath(source, propertyPath, out var raw))
            return Failure<T>(ResultResolutionStatus.PropertyNotFound, source,
                $"Die Ergebnis-Eigenschaft '{binding.PropertyId ?? binding.PropertyPath}' existiert im Ergebnis nicht.");
        if (raw is null)
            return Failure<T>(ResultResolutionStatus.ValueIsNull, source,
                $"Die Eigenschaft '{binding.PropertyPath}' enthält keinen Wert.");

        var values = Flatten(raw).OfType<T>().ToArray();
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

    public static ResolvedResultValue<System.Drawing.Point> ResolvePoints(
        IJobResultStore results, ResultBinding? binding) => Resolve<System.Drawing.Point>(results, binding);

    public static ResolvedResultValue<DetectionItem> ResolveDetections(
        IJobResultStore results, ResultBinding? binding)
    {
        var items = Resolve<DetectionItem>(results, binding);
        if (items.IsSuccess) return items;

        var points = Resolve<System.Drawing.Point>(results, binding);
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

        var rectangles = Resolve<System.Drawing.Rectangle>(results, binding);
        if (!rectangles.IsSuccess)
            return new(rectangles.Status, Array.Empty<DetectionItem>(),
                rectangles.SourceResult, rectangles.Error);

        var rectangleSource = rectangles.SourceResult as IDetectionStepResult;
        return new(ResultResolutionStatus.Success,
            rectangles.Values.Select((rectangle, index) => new DetectionItem
            {
                Center = new System.Drawing.Point(
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

            if (value is IEnumerable enumerable and not string)
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

    private static IEnumerable<object?> Flatten(object value)
    {
        if (value is IEnumerable enumerable and not string)
        {
            foreach (var item in enumerable)
                if (item is IEnumerable nested and not string)
                    foreach (var nestedItem in nested) yield return nestedItem;
                else yield return item;
            yield break;
        }
        yield return value;
    }

    private static bool TryReadMember(object? source, string name, out object? value)
    {
        value = null;
        if (source is null) return true;
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
}
