using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using DesktopAutomationApp.Converters;
using DesktopAutomationApp.Localization;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Contracts.Geometry;
using TaskAutomation.Steps.Definitions;
using TaskAutomation.WindowsIntegration;

namespace DesktopAutomationApp.Services.Jobs;

/// <summary>Creates concise, mode-aware details for every job step.</summary>
public sealed class JobStepDetailsProvider
{
    private static readonly IReadOnlyDictionary<string, string> SettingPropertyKeys =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["BoundsSource"] = "Ui.Step.DynamicRoi.Source",
            ["CameraName"] = "Ui.Step.Camera.Camera",
            ["QualityMode"] = "Ui.Step.Camera.Quality",
            ["FramesPerSecond"] = "Ui.Step.Camera.FramesPerSecond",
            ["PixelFormat"] = "Ui.Step.Camera.PixelFormat",
            ["CaptureCursor"] = "Ui.Step.Settings.CaptureMousePointer",
            ["ClearOnJobEnd"] = "Ui.Step.Settings.RemoveWhenJobEnds",
            ["ColorHex"] = "Ui.Step.Settings.Color",
            ["CreateParentDirectories"] = "Ui.Step.FileSystem.CreateParents",
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
            ["Filter"] = "Ui.Step.FileSystem.Filter",
            ["FileName"] = "Ui.Step.Settings.FileName",
            ["FullSearchInterval"] = "Ui.Step.DynamicRoi.FullSearchInterval",
            ["NewName"] = "Ui.Step.FileSystem.NewName",
            ["Operation"] = "Ui.Step.FileSystem.Operation",
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
            ["RetryCount"] = "Ui.Step.FileSystem.RetryCount",
            ["RetryDelayMs"] = "Ui.Step.FileSystem.RetryDelay",
            ["RetryLockedFiles"] = "Ui.Step.FileSystem.RetryLocked",
            ["ROI"] = "Ui.Step.Settings.ROI",
            ["ScriptPath"] = "Ui.Step.Settings.ScriptPath",
            ["SavePath"] = "Ui.Step.Settings.SavePath",
            ["SourcePath"] = "Ui.Step.FileSystem.Source",
            ["SourceResult"] = "Ui.Step.FileSystem.Source",
            ["Settings"] = "Ui.Job.Steps.DetailProperty.Settings",
            ["SkipEndSteps"] = "Ui.Step.Settings.SkipEndSteps",
            ["Source"] = "Ui.Step.Settings.PointSource",
            ["SourceStepId"] = "Ui.Step.Settings.SourceStep",
            ["Target"] = "Ui.Job.Steps.DetailProperty.Target",
            ["TargetPath"] = "Ui.Step.FileSystem.Target",
            ["TargetResult"] = "Ui.Step.FileSystem.Target",
            ["TemplateMatchMode"] = "Ui.Step.Settings.MatchMode",
            ["TemplatePath"] = "Ui.Step.Settings.Template",
            ["Text"] = "Ui.Step.Settings.DisplayText",
            ["TimeoutMs"] = "Ui.Step.Settings.TimeoutMs",
            ["WaitForExit"] = "Ui.Step.Settings.WaitForCompletion",
            ["WindowTitleContains"] = "Ui.Step.Settings.WindowTitleContains"
        };

    public string GetSummary(JobStep step, IEnumerable? steps)
    {
        if (!BuiltInStepDefinitions.Instance.TryGetByType(step.GetType(), out var definition))
            return string.Empty;

        var draft = definition.CreateDraft(step);
        var fields = definition.Descriptor.Fields.ToDictionary(field => field.Id, StringComparer.Ordinal);
        var values = new List<string>();
        foreach (var item in definition.Descriptor.Presentation.SummaryItems
                     .OrderByDescending(item => item.Priority))
        {
            if (!fields.TryGetValue(item.FieldId, out var field)
                || !draft.Values.TryGetValue(item.FieldId, out var value)
                || value is null
                || !IsDefinitionFieldVisible(field, draft)
                || item.HideWhenEmpty && IsSummaryValueEmpty(field, value))
                continue;

            var formatted = FormatSummaryValue(item, field, value, steps);
            if (item.HideWhenEmpty && string.IsNullOrWhiteSpace(formatted))
                continue;
            values.Add(string.IsNullOrWhiteSpace(item.LabelKey)
                ? formatted
                : $"{Loc.Get(item.LabelKey)}: {formatted}");
        }
        return string.Join(" · ", values);
    }

    public JobStepDetails GetDetails(JobStep step, IEnumerable? steps)
    {
        var items = new List<(string Group, StepDetailItem Item)>();
        var settings = step.GetType().GetProperty("Settings")?.GetValue(step);
        if (BuiltInStepDefinitions.Instance.TryGetByType(step.GetType(), out var definition))
        {
            AddDefinitionDetails(definition, step, items, steps);
        }
        else if (settings is IfConditionSettings conditions)
            AddConditions(conditions, items, steps);
        else if (settings is not null)
            AddProperties(settings, string.Empty, items, steps, 0);

        var order = new[] { "source", "detection", "roi", "general", "conditions", "advanced" };
        var groups = items.GroupBy(item => item.Group)
            .OrderBy(group => Array.IndexOf(order, group.Key))
            .Select(group => new StepDetailGroup(GroupTitle(group.Key), group.Select(item => item.Item).ToArray()))
            .ToArray();
        return new JobStepDetails(groups, CreateResultDetails(step));
    }

    private static void AddDefinitionDetails(
        IStepDefinition definition,
        JobStep step,
        List<(string Group, StepDetailItem Item)> target,
        IEnumerable? steps)
    {
        var draft = definition.CreateDraft(step);
        var fields = definition.Descriptor.Fields.ToDictionary(field => field.Id, StringComparer.Ordinal);
        foreach (var fieldId in definition.Descriptor.Presentation.DetailFieldIds)
        {
            if (!fields.TryGetValue(fieldId, out var field)
                || !draft.Values.TryGetValue(fieldId, out var value)
                || value is null
                || !IsDefinitionFieldVisible(field, draft))
                continue;
            if (string.Equals(field.EditorHint, StepEditorHints.ConditionEditor, StringComparison.Ordinal))
            {
                try
                {
                    var settings = value.Deserialize<IfConditionSettings>();
                    if (settings is not null) AddConditions(settings, target, steps);
                }
                catch (JsonException) { }
                continue;
            }
            if (string.Equals(field.EditorHint, StepEditorHints.WindowsCapabilityPicker, StringComparison.Ordinal))
            {
                AddWindowsCapabilityDetails(value, target);
                continue;
            }
            target.Add(("general", new StepDetailItem(
                Loc.Get(field.LabelKey),
                FormatDefinitionValue(field, value, steps))));
        }
    }

    private static bool IsDefinitionFieldVisible(StepFieldDescriptor field, StepDraft draft)
    {
        if (field.VisibleWhen is { } rule && !VisibilityRuleMatches(rule, draft))
            return false;
        return field.VisibleWhenAll is not { Count: > 0 } rules
            || rules.All(candidate => VisibilityRuleMatches(candidate, draft));
    }

    private static bool VisibilityRuleMatches(StepVisibilityRule rule, StepDraft draft)
    {
        if (!draft.Values.TryGetValue(rule.FieldId, out var actual)) return false;
        return rule.AnyOfValues is { Count: > 0 }
            ? rule.AnyOfValues.Any(expected => DefinitionValuesEqual(actual, expected))
            : DefinitionValuesEqual(actual, rule.EqualsValue);
    }

    private static bool DefinitionValuesEqual(
        System.Text.Json.Nodes.JsonNode? actual,
        System.Text.Json.Nodes.JsonNode? expected) =>
        string.Equals(actual?.ToJsonString(), expected?.ToJsonString(), StringComparison.OrdinalIgnoreCase);

    private static string FormatDefinitionValue(
        StepFieldDescriptor field,
        System.Text.Json.Nodes.JsonNode value,
        IEnumerable? steps)
    {
        if (string.Equals(field.EditorHint, StepEditorHints.CameraPicker, StringComparison.Ordinal))
            return FormatCameraSelection(value);
        if (string.Equals(field.EditorHint, StepEditorHints.VisualOverlay, StringComparison.Ordinal))
            return FormatVisualOverlay(value);
        if (string.Equals(field.EditorHint, StepEditorHints.RoiPicker, StringComparison.Ordinal))
            return FormatRoi(value, steps);
        if (string.Equals(field.EditorHint, StepEditorHints.YoloPicker, StringComparison.Ordinal))
            return FormatYoloSelection(value);
        if (string.Equals(field.EditorHint, StepEditorHints.ConditionEditor, StringComparison.Ordinal))
            return FormatConditionSelection(value);
        if (string.Equals(field.EditorHint, StepEditorHints.WindowsCapabilityPicker, StringComparison.Ordinal))
            return FormatWindowsCapabilitySelection(value);
        if (string.Equals(field.EditorHint, StepEditorHints.ScreenPointPicker, StringComparison.Ordinal))
            return FormatScreenPoint(value);
        if (string.Equals(field.EditorHint, StepEditorHints.UserChoiceOptions, StringComparison.Ordinal))
            return FormatUserChoiceOptions(value);
        if (string.Equals(field.EditorHint, StepEditorHints.PointEntryList, StringComparison.Ordinal))
            return FormatPointEntries(value);
        if (string.Equals(field.EditorHint, StepEditorHints.AxisExpressionList, StringComparison.Ordinal))
            return FormatAxisExpressions(value);
        if (value is not JsonValue jsonValue)
            return value.ToJsonString();

        return field.ValueKind switch
        {
            StepValueKind.Duration when TryGetInteger(jsonValue, out var duration) =>
                Loc.Format("Ui.Step.Generated.DurationMilliseconds", duration),
            StepValueKind.Integer when TryGetInteger(jsonValue, out var integer) =>
                integer.ToString(CultureInfo.CurrentCulture),
            StepValueKind.Number when TryGetNumber(jsonValue, out var number) =>
                number.ToString(CultureInfo.CurrentCulture),
            StepValueKind.Boolean when jsonValue.TryGetValue<bool>(out var flag) =>
                flag ? Loc.Get("Ui.Common.Yes") : Loc.Get("Ui.Common.No"),
            StepValueKind.Enum when jsonValue.TryGetValue<string>(out var option) =>
                FormatDefinitionOption(field, option),
            StepValueKind.ResultBinding => FormatDefinitionBinding(value, steps),
            StepValueKind.Object => FormatDefinitionObject(value, steps),
            _ when jsonValue.TryGetValue<string>(out var text) => text,
            _ => value.ToJsonString()
        };
    }

    private static bool TryGetInteger(JsonValue value, out long number)
    {
        if (value.TryGetValue<long>(out number)) return true;
        if (value.TryGetValue<int>(out var integer))
        {
            number = integer;
            return true;
        }
        if (value.TryGetValue<decimal>(out var decimalNumber)
            && decimal.Truncate(decimalNumber) == decimalNumber
            && decimalNumber is >= long.MinValue and <= long.MaxValue)
        {
            number = (long)decimalNumber;
            return true;
        }
        number = default;
        return false;
    }

    private static bool TryGetNumber(JsonValue value, out decimal number)
    {
        if (value.TryGetValue<decimal>(out number)) return true;
        if (value.TryGetValue<long>(out var integer))
        {
            number = integer;
            return true;
        }
        if (value.TryGetValue<double>(out var floatingPoint)
            && double.IsFinite(floatingPoint)
            && floatingPoint is >= (double)decimal.MinValue and <= (double)decimal.MaxValue)
        {
            number = (decimal)floatingPoint;
            return true;
        }
        number = default;
        return false;
    }

    private static string FormatScreenPoint(JsonNode value)
    {
        try
        {
            var point = value.Deserialize<StepScreenPointSelectionValue>();
            return point is null ? value.ToJsonString() : Loc.Format(
                "Ui.Step.Generated.ScreenPoint", point.MonitorIndex + 1, point.X, point.Y);
        }
        catch (JsonException) { return value.ToJsonString(); }
    }

    private static string FormatUserChoiceOptions(JsonNode value)
    {
        try
        {
            return string.Join(", ", value.Deserialize<List<StepUserChoiceOptionValue>>()?
                .Select(option => option.Label).Where(label => !string.IsNullOrWhiteSpace(label)) ?? []);
        }
        catch (JsonException) { return value.ToJsonString(); }
    }

    private static string FormatPointEntries(JsonNode value)
    {
        try
        {
            var points = value.Deserialize<List<StepPointEntryValue>>() ?? [];
            return Loc.Format("Ui.Step.Generated.PointCount", points.Count);
        }
        catch (JsonException) { return value.ToJsonString(); }
    }

    private static string FormatAxisExpressions(JsonNode value)
    {
        try
        {
            return string.Join(", ", value.Deserialize<List<StepAxisExpressionValue>>()?
                .Select(expression => $"{expression.Axis} {expression.Operator} {expression.Value.ToString(CultureInfo.CurrentCulture)}") ?? []);
        }
        catch (JsonException) { return value.ToJsonString(); }
    }

    private static string FormatSummaryValue(
        StepSummaryItemDescriptor item,
        StepFieldDescriptor field,
        System.Text.Json.Nodes.JsonNode value,
        IEnumerable? steps)
    {
        var formatted = FormatDefinitionValue(field, value, steps);
        return item.Format switch
        {
            StepSummaryValueFormat.ShortText when formatted.Length > 80 => formatted[..77] + "...",
            StepSummaryValueFormat.FileName => Path.GetFileName(formatted.TrimEnd(Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar)),
            StepSummaryValueFormat.DurationMilliseconds when value is JsonValue durationValue
                && TryGetInteger(durationValue, out var duration) =>
                Loc.Format("Ui.Step.Generated.DurationMilliseconds", duration),
            StepSummaryValueFormat.BooleanBadge when value is JsonValue booleanValue
                && booleanValue.TryGetValue<bool>(out var flag) =>
                flag ? Loc.Get("Ui.Common.Yes") : Loc.Get("Ui.Common.No"),
            _ => formatted
        };
    }

    private static bool IsSummaryValueEmpty(
        StepFieldDescriptor field,
        System.Text.Json.Nodes.JsonNode value)
    {
        try
        {
            if (field.ValueKind is StepValueKind.Text or StepValueKind.MultilineText
                or StepValueKind.FilePath or StepValueKind.DirectoryPath)
                return value is JsonValue textValue
                    && textValue.TryGetValue<string>(out var text)
                    && string.IsNullOrWhiteSpace(text);
            if (field.ValueKind == StepValueKind.ResultBinding)
                return value.Deserialize<ResultBinding>()?.IsConfigured != true;
            if (string.Equals(field.EditorHint, StepEditorHints.CameraPicker, StringComparison.Ordinal))
            {
                var camera = value.Deserialize<StepCameraSelectionValue>();
                return string.IsNullOrWhiteSpace(camera?.CameraId);
            }
            if (field.EditorHint is StepEditorHints.ProcessTargetPicker
                or StepEditorHints.ExecutableProcessTargetPicker)
            {
                var selector = value.Deserialize<StepProcessSelectorValue>();
                var binding = selector?.ProcessSource?.Deserialize<ResultBinding>();
                return binding?.IsConfigured != true
                       && string.IsNullOrWhiteSpace(selector?.ProcessName)
                       && string.IsNullOrWhiteSpace(selector?.ExecutablePath);
            }
            if (field.ValueKind == StepValueKind.Object)
            {
                var reference = value.Deserialize<StepReferenceValue>();
                if (reference is not null)
                    return string.IsNullOrWhiteSpace(reference.Id) && string.IsNullOrWhiteSpace(reference.Name);
            }
        }
        catch (JsonException) { }
        catch (InvalidOperationException) { }
        return false;
    }

    private static string FormatCameraSelection(System.Text.Json.Nodes.JsonNode value)
    {
        try
        {
            var camera = value.Deserialize<StepCameraSelectionValue>();
            if (camera is null) return "–";
            var name = string.IsNullOrWhiteSpace(camera.CameraName) ? camera.CameraId : camera.CameraName;
            var quality = camera.QualityMode switch
            {
                nameof(CameraQualityMode.Automatic) => Loc.Get("Ui.Step.Camera.QualityAutomatic"),
                nameof(CameraQualityMode.HighestAvailable) => Loc.Get("Ui.Step.Camera.QualityHighest"),
                nameof(CameraQualityMode.Specific) =>
                    $"{camera.Width} × {camera.Height} · {camera.FramesPerSecond:0.##} FPS · {camera.PixelFormat}",
                _ => camera.QualityMode
            };
            return $"{name} · {quality}";
        }
        catch (JsonException)
        {
            return "–";
        }
        catch (InvalidOperationException)
        {
            return "–";
        }
    }

    private static string FormatVisualOverlay(System.Text.Json.Nodes.JsonNode value)
    {
        try
        {
            var overlay = value.Deserialize<VisualOverlaySettings>();
            return overlay is null
                ? "–"
                : Loc.Format(
                    "Ui.Step.Generated.OverlaySummary",
                    overlay.DetectionResults.Count,
                    overlay.TextResults.Count);
        }
        catch (JsonException) { return "–"; }
        catch (InvalidOperationException) { return "–"; }
    }

    private static string FormatDefinitionOption(StepFieldDescriptor field, string value)
    {
        var option = field.Options?.FirstOrDefault(candidate =>
            string.Equals(candidate.Value, value, StringComparison.OrdinalIgnoreCase));
        return option is null ? value : Loc.Get(option.LabelKey);
    }

    private static string FormatDefinitionBinding(
        System.Text.Json.Nodes.JsonNode value,
        IEnumerable? steps)
    {
        try { return FormatBinding(value.Deserialize<ResultBinding>() ?? new ResultBinding(), steps); }
        catch (System.Text.Json.JsonException) { return value.ToJsonString(); }
    }

    private static string FormatDefinitionObject(
        System.Text.Json.Nodes.JsonNode value,
        IEnumerable? steps)
    {
        try
        {
            var reference = value.Deserialize<StepReferenceValue>();
            if (reference is not null && !string.IsNullOrWhiteSpace(reference.Name))
                return reference.Name;
        }
        catch (System.Text.Json.JsonException) { }
        try
        {
            var selector = value.Deserialize<StepProcessSelectorValue>();
            var binding = selector?.ProcessSource?.Deserialize<ResultBinding>();
            if (binding?.IsConfigured == true)
                return FormatBinding(binding, steps);
            var process = !string.IsNullOrWhiteSpace(selector?.ProcessName)
                ? selector.ProcessName
                : selector?.ExecutablePath;
            if (!string.IsNullOrWhiteSpace(process)
                && !string.IsNullOrWhiteSpace(selector?.WindowTitleContains))
                return $"{process} · {Loc.Get("Ui.Step.Settings.WindowTitleContains")}: {selector.WindowTitleContains}";
            if (!string.IsNullOrWhiteSpace(process))
                return process;
        }
        catch (System.Text.Json.JsonException) { }
        return value.ToJsonString();
    }

    private static string FormatRoi(System.Text.Json.Nodes.JsonNode value, IEnumerable? steps)
    {
        try
        {
            var roi = value.Deserialize<StepRoiSelectionValue>();
            if (roi is null) return "–";
            var staticValue = roi.Enabled
                ? Loc.Format("Ui.Step.Generated.RoiStatic", roi.X, roi.Y, roi.Width, roi.Height)
                : string.Empty;
            var dynamicBinding = roi.DynamicSource?.Deserialize<ResultBinding>();
            var dynamicValue = dynamicBinding?.IsConfigured == true
                ? Loc.Format("Ui.Step.Generated.RoiDynamic", FormatBinding(dynamicBinding, steps))
                : string.Empty;
            if (staticValue.Length > 0 && dynamicValue.Length > 0)
                return $"{staticValue} · {dynamicValue}";
            return staticValue.Length > 0 ? staticValue : dynamicValue.Length > 0 ? dynamicValue : "–";
        }
        catch (System.Text.Json.JsonException) { return "–"; }
        catch (InvalidOperationException) { return "–"; }
    }

    private static string FormatYoloSelection(System.Text.Json.Nodes.JsonNode value)
    {
        try
        {
            var selection = value.Deserialize<StepYoloSelectionValue>();
            if (selection is null) return "–";
            if (string.IsNullOrWhiteSpace(selection.Model)) return selection.ClassName;
            return string.IsNullOrWhiteSpace(selection.ClassName)
                ? selection.Model
                : $"{selection.Model} · {selection.ClassName}";
        }
        catch (System.Text.Json.JsonException) { return "–"; }
        catch (InvalidOperationException) { return "–"; }
    }

    private static string FormatConditionSelection(System.Text.Json.Nodes.JsonNode value)
    {
        try
        {
            var settings = value.Deserialize<IfConditionSettings>();
            if (settings is null) return string.Empty;
            var matchMode = settings.MatchMode == ConditionMatchMode.Any
                ? Loc.Get("Ui.Step.Settings.OneOR")
                : Loc.Get("Ui.Step.Settings.AllAND");
            return Loc.Format("Ui.Step.Generated.ConditionSummary", settings.Conditions.Count, matchMode);
        }
        catch (JsonException) { return string.Empty; }
        catch (InvalidOperationException) { return string.Empty; }
    }

    private static string FormatWindowsCapabilitySelection(System.Text.Json.Nodes.JsonNode value)
    {
        try
        {
            var selection = value.Deserialize<StepWindowsCapabilitySelectionValue>();
            if (selection is null) return string.Empty;
            var capability = new WindowsCapabilityCatalog().Find(selection.CapabilityId);
            return capability is null
                ? selection.CapabilityId
                : WindowsCapabilityLocalization.DisplayName(capability);
        }
        catch (JsonException) { return string.Empty; }
        catch (InvalidOperationException) { return string.Empty; }
    }

    private static void AddWindowsCapabilityDetails(
        System.Text.Json.Nodes.JsonNode value,
        List<(string Group, StepDetailItem Item)> target)
    {
        try
        {
            var selection = value.Deserialize<StepWindowsCapabilitySelectionValue>();
            if (selection is null) return;
            var capability = new WindowsCapabilityCatalog().Find(selection.CapabilityId);
            target.Add(("general", new StepDetailItem(
                Loc.Get("Ui.Windows.Capability"),
                capability is null
                    ? selection.CapabilityId
                    : WindowsCapabilityLocalization.DisplayName(capability))));
            foreach (var parameter in capability?.Parameters ?? [])
            {
                var parameterValue = selection.Parameters.FirstOrDefault(candidate =>
                    candidate.Key.Equals(parameter.Name, StringComparison.OrdinalIgnoreCase)).Value;
                if (!string.IsNullOrWhiteSpace(parameterValue))
                    target.Add(("general", new StepDetailItem(
                        WindowsCapabilityLocalization.ParameterName(parameter), parameterValue)));
            }
        }
        catch (JsonException) { }
        catch (InvalidOperationException) { }
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
        if (owner is CameraCaptureSettings && name == nameof(CameraCaptureSettings.CameraId)) return false;
        if (owner is CameraCaptureSettings camera
            && name is nameof(CameraCaptureSettings.Width)
                or nameof(CameraCaptureSettings.Height)
                or nameof(CameraCaptureSettings.FramesPerSecond)
                or nameof(CameraCaptureSettings.PixelFormat))
            return camera.QualityMode == CameraQualityMode.Specific;
        if (owner is FileSystemOperationSettings fileSystem)
        {
            if (name is nameof(FileSystemOperationSettings.SourceMode)
                or nameof(FileSystemOperationSettings.TargetMode))
                return false;
            if (name == nameof(FileSystemOperationSettings.SourcePath))
                return fileSystem.SourceMode == FileSystemPathSource.ExplicitPath;
            if (name == nameof(FileSystemOperationSettings.SourceResult))
                return fileSystem.SourceMode == FileSystemPathSource.TaskResult;
            if (name is nameof(FileSystemOperationSettings.TargetPath)
                or nameof(FileSystemOperationSettings.TargetResult))
            {
                if (fileSystem.Operation is not (FileSystemOperation.Copy or FileSystemOperation.Move))
                    return false;
                return name == nameof(FileSystemOperationSettings.TargetPath)
                    ? fileSystem.TargetMode == FileSystemPathSource.ExplicitPath
                    : fileSystem.TargetMode == FileSystemPathSource.TaskResult;
            }
            if (name == nameof(FileSystemOperationSettings.NewName))
                return fileSystem.Operation == FileSystemOperation.Rename;
            if (name == nameof(FileSystemOperationSettings.Filter))
                return fileSystem.Operation == FileSystemOperation.Delete
                    && !string.IsNullOrWhiteSpace(fileSystem.Filter);
            if (name == nameof(FileSystemOperationSettings.CreateParentDirectories))
                return fileSystem.Operation is FileSystemOperation.Copy or FileSystemOperation.Move;
            if (name is nameof(FileSystemOperationSettings.RetryCount)
                or nameof(FileSystemOperationSettings.RetryDelayMs))
                return fileSystem.RetryLockedFiles;
        }

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
        PixelPoint point => PixelGeometryFormatter.Format(point),
        PixelSize size => PixelGeometryFormatter.Format(size),
        PixelRegion rectangle => PixelGeometryFormatter.Format(rectangle),
        double number => number.ToString("0.###", CultureInfo.CurrentCulture),
        float number => number.ToString("0.###", CultureInfo.CurrentCulture),
        CameraQualityMode.Automatic => Loc.Get("Ui.Step.Camera.QualityAutomatic"),
        CameraQualityMode.HighestAvailable => Loc.Get("Ui.Step.Camera.QualityHighest"),
        CameraQualityMode.Specific => Loc.Get("Ui.Step.Camera.QualitySpecific"),
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
