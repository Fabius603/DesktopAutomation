using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using DesktopAutomationApp.Converters;
using DesktopAutomationApp.Localization;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;

namespace DesktopAutomationApp.Services.Jobs;

/// <summary>Creates concise, mode-aware details for every job step.</summary>
public sealed class JobStepDetailsProvider
{
    private static readonly IReadOnlyDictionary<string, string> SettingPropertyKeys =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["BoundsSource"] = "Ui.Step.DynamicRoi.Source",
            ["CaptureCursor"] = "Ui.Step.Settings.CaptureMousePointer",
            ["ClearOnJobEnd"] = "Ui.Step.Settings.RemoveWhenJobEnds",
            ["ColorHex"] = "Ui.Step.Settings.Color",
            ["CombineMode"] = "Ui.Step.Settings.CombineWith",
            ["ConfidenceThreshold"] = "Ui.Step.Settings.ConfidencePercent",
            ["DelayMs"] = "Ui.Step.Settings.WaitTimeMs",
            ["DesktopIdx"] = "Ui.Step.Settings.Monitor",
            ["DetectionsSource"] = "Ui.Step.Settings.DetectionStep",
            ["DownscaleFactor"] = "Ui.Step.Settings.Downscale",
            ["DurationMs"] = "Ui.Step.Settings.DisplayDurationMs",
            ["DynamicRoiSource"] = "Ui.Step.DynamicRoi.Selector",
            ["ExecutablePath"] = "Ui.Step.Settings.PathProgram",
            ["ExpressionSettings"] = "Ui.Step.Settings.AxisExpressions",
            ["Expressions"] = "Ui.Step.Settings.AxisExpressions",
            ["FontSize"] = "Ui.Step.Settings.FontSizePt",
            ["FullSearchInterval"] = "Ui.Step.DynamicRoi.FullSearchInterval",
            ["ImageSource"] = "Ui.Step.Settings.ImageSource",
            ["JobId"] = "Ui.Step.Settings.Job",
            ["JobName"] = "Ui.Step.Settings.Job",
            ["LowesRatioThreshold"] = "Ui.Step.Settings.LoweSRatio01",
            ["MakroId"] = "Ui.Step.Settings.Macro",
            ["MakroName"] = "Ui.Step.Settings.Macro",
            ["ManualX"] = "Ui.Step.Settings.X",
            ["ManualY"] = "Ui.Step.Settings.Y",
            ["MatchRequirement"] = "Ui.Step.Settings.Evaluation",
            ["MaxSampleAgeMs"] = "Ui.Step.Settings.MaxAgeMs",
            ["MinMatchCount"] = "Ui.Step.Settings.MinMatches",
            ["MinSamples"] = "Ui.Step.Settings.MinValues",
            ["MinimumConfidence"] = "Ui.Step.Settings.MinimumConfidencePercent",
            ["MonitorIndex"] = "Ui.Step.Settings.Monitor",
            ["MultiplePoints"] = "Ui.Step.Settings.UseAllPointsFoundByThisStep",
            ["OffsetX"] = "Ui.Step.Settings.XOffsetPixels",
            ["OffsetY"] = "Ui.Step.Settings.YOffsetPixels",
            ["OffsetSettings"] = "Ui.Step.Settings.OffsetTolerance",
            ["OriginX"] = "Ui.Step.Settings.Origin",
            ["OriginY"] = "Ui.Step.Settings.Origin",
            ["Padding"] = "Ui.Step.DynamicRoi.Padding",
            ["PlacementMode"] = "Ui.Step.Settings.Position",
            ["Points"] = "Ui.Step.Settings.PointsToCheck",
            ["PointsSource"] = "Ui.Step.Settings.PointSource",
            ["ProcessSource"] = "Ui.Step.Settings.ProcessSource",
            ["PropertyPath"] = "Ui.Step.Settings.Property",
            ["Query"] = "Ui.Job.Steps.DetailProperty.Query",
            ["ReferencePointsSource"] = "Ui.Step.Settings.ReferenceSource",
            ["ReferenceX"] = "Ui.Step.Settings.X",
            ["ReferenceY"] = "Ui.Step.Settings.Y",
            ["ResetAfterMisses"] = "Ui.Step.DynamicRoi.ResetAfterMisses",
            ["ResetDistanceThreshold"] = "Ui.Step.Settings.ResetAtDistance",
            ["ROI"] = "Ui.Step.Settings.ROI",
            ["ScriptPath"] = "Ui.Step.Settings.ScriptPath",
            ["Settings"] = "Ui.Job.Steps.DetailProperty.Settings",
            ["SkipEndSteps"] = "Ui.Step.Settings.SkipEndSteps",
            ["Source"] = "Ui.Step.Settings.PointSource",
            ["SourceStepId"] = "Ui.Step.Settings.SourceStep",
            ["Target"] = "Ui.Job.Steps.DetailProperty.Target",
            ["TemplateMatchMode"] = "Ui.Step.Settings.MatchMode",
            ["TemplatePath"] = "Ui.Step.Settings.Template",
            ["Text"] = "Ui.Step.Settings.DisplayText",
            ["TimeoutMs"] = "Ui.Step.Settings.TimeoutMs",
            ["WaitForExit"] = "Ui.Step.Settings.WaitForCompletion",
            ["WindowTitleContains"] = "Ui.Step.Settings.WindowTitleContains"
        };

    public JobStepDetails GetDetails(JobStep step, IEnumerable? steps)
    {
        var items = new List<(string Group, StepDetailItem Item)>();
        var settings = step.GetType().GetProperty("Settings")?.GetValue(step);
        if (step is WindowsStateQueryStep windowsState)
        {
            var capability = new TaskAutomation.WindowsIntegration.WindowsCapabilityCatalog().Find(windowsState.Settings.QueryType);
            items.Add(("general", new StepDetailItem(Loc.Get("Ui.Windows.Capability"),
                capability is null ? windowsState.Settings.QueryType : WindowsCapabilityLocalization.DisplayName(capability))));
            foreach (var parameter in capability?.Parameters ?? [])
                if (windowsState.Settings.Parameters.TryGetValue(parameter.Name, out var value) && !string.IsNullOrWhiteSpace(value))
                    items.Add(("general", new StepDetailItem(WindowsCapabilityLocalization.ParameterName(parameter), value)));
        }
        else if (settings is IfConditionSettings conditions)
            AddConditions(conditions, items, steps);
        else if (settings is PointComparisonSettings comparison)
            AddPointComparison(comparison, items, steps);
        else if (settings is not null)
            AddProperties(settings, string.Empty, items, steps, 0);

        var order = new[] { "source", "detection", "roi", "general", "conditions", "advanced" };
        var groups = items.GroupBy(item => item.Group)
            .OrderBy(group => Array.IndexOf(order, group.Key))
            .Select(group => new StepDetailGroup(GroupTitle(group.Key), group.Select(item => item.Item).ToArray()))
            .ToArray();
        return new JobStepDetails(groups, CreateResultDetails(step));
    }

    private static void AddConditions(IfConditionSettings settings,
        List<(string Group, StepDetailItem Item)> target, IEnumerable? steps)
    {
        target.Add(("general", new StepDetailItem(Loc.Get("Ui.Step.Settings.ConditionMatchMode"),
            settings.MatchMode == ConditionMatchMode.All
                ? Loc.Get("Ui.Step.Settings.AllAND")
                : Loc.Get("Ui.Step.Settings.OneOR"))));
        var index = 1;
        foreach (var condition in settings.Conditions)
            target.Add(("conditions", new StepDetailItem(
                $"{index++}. {Loc.Get("Ui.Step.IfEditor.Condition")}",
                ConditionDisplayFormatter.Format(condition, steps as IList))));
    }

    private static void AddPointComparison(PointComparisonSettings settings,
        List<(string Group, StepDetailItem Item)> target, IEnumerable? steps)
    {
        AddLeaf(nameof(settings.Mode), settings.Mode, target, "general");
        AddLeaf(nameof(settings.MatchRequirement), settings.MatchRequirement, target, "general");
        for (var index = 0; index < settings.Points.Count; index++)
            AddProperties(settings.Points[index], $"{index + 1}. ", target, steps, 1);

        if (settings.Mode == PointComparisonMode.Offset)
            AddProperties(settings.OffsetSettings, string.Empty, target, steps, 1);
        else
            AddProperties(settings.ExpressionSettings, string.Empty, target, steps, 1);
    }

    private static void AddProperties(object owner, string prefix,
        List<(string Group, StepDetailItem Item)> target, IEnumerable? steps, int depth)
    {
        if (depth > 3) return;
        foreach (var property in owner.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
                     .Where(property => property.CanRead && property.GetIndexParameters().Length == 0))
        {
            var value = property.GetValue(owner);
            if (!ShouldShow(owner, property, value)) continue;

            if (value is ResultBinding binding)
            {
                target.Add(("source", new StepDetailItem(
                    prefix + LocalizedSettingName(property.Name), FormatBinding(binding, steps))));
                continue;
            }

            if (IsNested(property.PropertyType))
            {
                if (value is not null)
                    AddProperties(value, prefix + LocalizedSettingName(property.Name) + " / ", target, steps, depth + 1);
                continue;
            }

            AddLeaf(prefix + LocalizedSettingName(property.Name), value, target, GetGroup(property.Name));
        }
    }

    private static bool ShouldShow(object owner, PropertyInfo property, object? value)
    {
        var name = property.Name;
        if (value is ResultBinding binding) return binding.IsConfigured;
        if (value is null) return false;

        if (owner is ProcessTargetSettings target)
            return target.ProcessSource.IsConfigured
                ? name == nameof(ProcessTargetSettings.ProcessSource)
                : name != nameof(ProcessTargetSettings.ProcessSource)
                  && value is string queryText
                  && !string.IsNullOrWhiteSpace(queryText);

        if (value is string text) return !string.IsNullOrWhiteSpace(text);
        if (value is bool flag) return flag;
        if (value is Guid guid) return guid != Guid.Empty && !HasReadableName(owner, name);
        if (value is IEnumerable sequence and not string) return sequence.Cast<object>().Any();

        if (owner is StartProcessSettings start)
        {
            if (name == nameof(StartProcessSettings.Action)) return true;
            if (start.Action == StartProcessAction.Terminate)
                return name == nameof(StartProcessSettings.Target);
            if (name == nameof(StartProcessSettings.Target)) return false;
            if (name is nameof(StartProcessSettings.OffsetX) or nameof(StartProcessSettings.OffsetY))
                return start.PlacementMode == StartProcessPlacementMode.Custom;
        }

        if (owner is FocusProcessSettings focus
            && name == nameof(FocusProcessSettings.WindowMode))
            return focus.Action == FocusProcessAction.BringToFront;

        if (name == "ROI")
        {
            var enabled = owner.GetType().GetProperty("EnableROI")?.GetValue(owner) as bool?;
            return enabled == true;
        }
        if (name == "EnableROI") return value is true;

        if (owner is PointEntry point)
            return point.Source == PointEntrySource.Manual
                ? name != nameof(PointEntry.PointsSource)
                : name is nameof(PointEntry.Source) or nameof(PointEntry.PointsSource);

        if (owner is OffsetComparisonSettings offset)
        {
            if (name == nameof(OffsetComparisonSettings.ReferencePointsSource))
                return offset.ReferenceSource == PointEntrySource.JobResult;
            if (name is nameof(OffsetComparisonSettings.ReferenceX) or nameof(OffsetComparisonSettings.ReferenceY))
                return offset.ReferenceSource == PointEntrySource.Manual;
        }

        if (IsAdvanced(name) && IsDefaultValue(owner, property, value)) return false;
        return true;
    }

    private static bool HasReadableName(object owner, string idPropertyName)
    {
        var nameProperty = idPropertyName.EndsWith("Id", StringComparison.Ordinal)
            ? owner.GetType().GetProperty(idPropertyName[..^2] + "Name")
            : null;
        return nameProperty?.GetValue(owner) is string text && !string.IsNullOrWhiteSpace(text);
    }

    private static bool IsDefaultValue(object owner, PropertyInfo property, object value)
    {
        try
        {
            var defaults = Activator.CreateInstance(owner.GetType());
            return defaults is not null && Equals(property.GetValue(defaults), value);
        }
        catch { return false; }
    }

    private static bool IsAdvanced(string name) =>
        name.Contains("Interval", StringComparison.OrdinalIgnoreCase)
        || name.Contains("Reset", StringComparison.OrdinalIgnoreCase)
        || name.Contains("Timeout", StringComparison.OrdinalIgnoreCase)
        || name.Contains("Cache", StringComparison.OrdinalIgnoreCase)
        || name.Contains("Downscale", StringComparison.OrdinalIgnoreCase)
        || name.Contains("MaxSample", StringComparison.OrdinalIgnoreCase)
        || name.Contains("MaxFit", StringComparison.OrdinalIgnoreCase)
        || name.Contains("MaxPrediction", StringComparison.OrdinalIgnoreCase);

    private static void AddLeaf(string name, object? value,
        List<(string Group, StepDetailItem Item)> target, string group) =>
        target.Add((group, new StepDetailItem(LocalizedSettingPath(name), FormatValue(value))));

    private static string FormatBinding(ResultBinding binding, IEnumerable? steps)
    {
        var source = ResolveStep(binding.SourceStepId, steps) ?? binding.SourceStepId;
        var property = string.IsNullOrWhiteSpace(binding.PropertyPath)
            ? binding.PropertyId
            : binding.PropertyPath;
        return string.IsNullOrWhiteSpace(property)
            ? source
            : $"{source} → {LocalizedPropertyName(property)}";
    }

    private static StepResultDetails? CreateResultDetails(JobStep step)
    {
        var descriptor = StepResultMetadata.GetResultTypeForStep(step);
        if (descriptor is null) return null;

        var properties = descriptor.PropertyTree
            .Select(node => CreateResultPropertyDetails(node, string.Empty, descriptor.TypeName))
            .ToArray();
        if (properties.Length == 0) return null;
        return new StepResultDetails(descriptor.DisplayName, properties);
    }

    private static StepResultPropertyDetails CreateResultPropertyDetails(
        ResultPropertyNode node,
        string parentPath,
        string resultTypeName)
    {
        var path = string.IsNullOrWhiteSpace(parentPath)
            ? node.Segment
            : $"{parentPath}.{node.Segment}";
        return new StepResultPropertyDetails(
            StepLocalization.PropertyPath(resultTypeName, path),
            node.Property is null ? string.Empty : FriendlyType(node.Property),
            node.Property is null
                ? string.Empty
                : StepLocalization.PropertyDescription(resultTypeName, node.Property),
            node.Children.Select(child => CreateResultPropertyDetails(child, path, resultTypeName)).ToArray());
    }

    private static string FriendlyType(ResultPropertyDescriptor property)
    {
        var semanticType = property.DataType switch
        {
            ResultValueKind.Boolean => Loc.Get("Ui.Job.Steps.ResultType.Boolean"),
            ResultValueKind.Integer => Loc.Get("Ui.Job.Steps.ResultType.Integer"),
            ResultValueKind.Number => Loc.Get("Ui.Job.Steps.ResultType.Number"),
            ResultValueKind.Text => Loc.Get("Ui.Job.Steps.ResultType.Text"),
            ResultValueKind.DateTime => Loc.Get("Ui.Job.Steps.ResultType.DateTime"),
            ResultValueKind.Image => Loc.Get("Ui.Job.Steps.ResultType.Image"),
            ResultValueKind.Point => Loc.Get("Ui.Job.Steps.ResultType.Point"),
            ResultValueKind.Rectangle => Loc.Get("Ui.Job.Steps.ResultType.Rectangle"),
            ResultValueKind.Detection => Loc.Get("Ui.Job.Steps.ResultType.Detection"),
            ResultValueKind.ProcessReference => Loc.Get("Ui.Job.Steps.ResultType.Process"),
            ResultValueKind.Enum => Loc.Get("Ui.Job.Steps.ResultType.Enum"),
            _ => Loc.Get("Ui.Job.Steps.ResultType.Object")
        };
        return property.Cardinality switch
        {
            ResultCardinality.Collection => $"{Loc.Get("Ui.Job.Steps.ResultType.List")}<{semanticType}>",
            ResultCardinality.OptionalSingle => $"{semanticType} ({Loc.Get("Ui.Job.Steps.ResultType.Optional")})",
            _ => semanticType
        };
    }

    private static string LocalizedPropertyName(string path) => StepLocalization.PropertyPath(path);

    private static string LocalizedSettingPath(string path) => string.Join(" / ",
        path.Split(" / ", StringSplitOptions.TrimEntries).Select(part =>
        {
            var numberedPrefixLength = part.TakeWhile(character => char.IsDigit(character) || character is '.' or ' ').Count();
            var prefix = part[..numberedPrefixLength];
            var propertyName = part[numberedPrefixLength..];
            return prefix + LocalizedSettingName(propertyName);
        }));

    private static string LocalizedSettingName(string propertyName)
    {
        if (SettingPropertyKeys.TryGetValue(propertyName, out var mappedKey))
            return Loc.Get(mappedKey);
        var key = $"Ui.Step.Settings.{propertyName}";
        var translated = Loc.Get(key);
        return translated == $"[{key}]" ? Humanize(propertyName) : translated;
    }

    private static bool IsNested(Type type) => !type.IsPrimitive && !type.IsEnum
        && type != typeof(string) && !type.IsValueType && !typeof(IEnumerable).IsAssignableFrom(type);
    private static string Humanize(string value) => string.Join(" / ", value.Split('.').Select(segment =>
        Regex.Replace(segment, "(?<=[a-z0-9])([A-Z])", " $1")));
    private static string FormatValue(object? value) => value switch
    {
        null => "–",
        bool => Loc.Get("Ui.Common.Yes"),
        OpenCvSharp.Rect rectangle => $"X {rectangle.X}  ·  Y {rectangle.Y}  ·  {rectangle.Width} × {rectangle.Height} px",
        double number => number.ToString("0.###", CultureInfo.CurrentCulture),
        float number => number.ToString("0.###", CultureInfo.CurrentCulture),
        IEnumerable sequence when value is not string => string.Join(", ", sequence.Cast<object>()),
        _ => value.ToString() ?? "–"
    };

    private static string GetGroup(string name)
    {
        if (name.Contains("Source", StringComparison.OrdinalIgnoreCase)) return "source";
        if (name.Contains("Roi", StringComparison.OrdinalIgnoreCase)) return "roi";
        if (name.Contains("Threshold", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Confidence", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Template", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Color", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Model", StringComparison.OrdinalIgnoreCase)) return "detection";
        return IsAdvanced(name) ? "advanced" : "general";
    }

    private static string GroupTitle(string group) => group switch
    {
        "source" => Loc.Get("Ui.Job.Steps.DetailsSources"),
        "detection" => Loc.Get("Ui.Job.Steps.DetailsDetection"),
        "roi" => Loc.Get("Ui.Job.Steps.DetailsRoi"),
        "advanced" => Loc.Get("Ui.Job.Steps.DetailsAdvanced"),
        "conditions" => Loc.Get("Ui.Job.Steps.DetailsConditions"),
        _ => Loc.Get("Ui.Job.Steps.DetailsGeneral")
    };

    private static string? ResolveStep(string id, IEnumerable? steps)
    {
        var list = steps?.Cast<object>().OfType<JobStep>().ToList();
        var index = list?.FindIndex(step => step.Id == id) ?? -1;
        if (index < 0) return null;
        var step = list![index];
        return StepLocalization.ResultStepName(step, list);
    }
}
