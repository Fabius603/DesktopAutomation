using System.Collections;
using System.IO;
using System.Reflection;
using TaskAutomation.Steps;

namespace TaskAutomation.Jobs;

public sealed record StepValidationResult(JobStep Step, bool IsValid, string? Error);
public sealed record JobValidationResult(bool IsValid, IReadOnlyList<StepValidationResult> Steps);

/// <summary>Zentrale Regeln fuer Step-Abhaengigkeiten. UI-Code darf diese Regeln nur anzeigen.</summary>
public static class JobValidation
{
    public static IReadOnlyList<JobStep> GetAllowedSourceSteps(IReadOnlyList<JobStep> steps, int consumerIndex, string resultTypeName)
        => steps.Take(Math.Clamp(consumerIndex, 0, steps.Count))
            .Where(s => s.IsEnabled && StepPipelineRegistry.Get(s.GetType())?.Output == resultTypeName)
            .ToList();

    public static bool IsStepAllowed(Job job, JobStep step)
        => ValidateStep(job.Steps, step).IsValid;

    public static bool IsJobAllowed(Job job) => ValidateJob(job).IsValid;

    public static bool CanConfirm(IReadOnlyList<JobStep> precedingSteps, JobStep? candidate)
    {
        if (candidate == null) return false;
        var steps = precedingSteps.Concat([candidate]).ToList();
        return ValidateStep(steps, candidate).IsValid;
    }

    public static StepValidationResult ValidateCandidate(IReadOnlyList<JobStep> precedingSteps, JobStep? candidate, IReadOnlyList<JobStep>? allSteps = null)
    {
        if (candidate == null) return new(null!, false, "Es konnte kein Step erstellt werden.");
        var steps = precedingSteps.Concat([candidate]).ToList();
        return ValidateStep(steps, candidate, allSteps);
    }

    public static bool IsSourceStepAllowed(IReadOnlyList<JobStep> steps, JobStep consumer, JobStep source)
    {
        var consumerIndex = IndexOf(steps, consumer);
        var sourceIndex = IndexOf(steps, source);
        return source.IsEnabled && sourceIndex >= 0 && consumerIndex >= 0 && sourceIndex < consumerIndex;
    }

    public static JobValidationResult ValidateJob(Job job)
    {
        var results = job.Steps.Select(s => ValidateStep(job.Steps, s)).ToList();
        var structureErrors = GetIfStructureErrors(job.Steps);
        results = results.Select(r => structureErrors.TryGetValue(r.Step, out var error)
            ? new StepValidationResult(r.Step, false, error) : r).ToList();
        return new JobValidationResult(results.All(r => r.IsValid), results);
    }

    public static bool IsIfStructureAllowed(IReadOnlyList<JobStep> steps)
        => GetIfStructureErrors(steps).Count == 0;

    private static Dictionary<JobStep, string> GetIfStructureErrors(IReadOnlyList<JobStep> steps)
    {
        var errors = new Dictionary<JobStep, string>();
        var blocks = new Stack<(IfStep Step, bool SeenElse)>();
        foreach (var step in steps)
        {
            switch (step)
            {
                case IfStep current:
                    if (blocks.Count > 0) errors[current] = "Verschachtelte If-Bloecke sind nicht erlaubt.";
                    blocks.Push((current, false));
                    break;
                case ElseIfStep:
                    if (blocks.Count == 0) errors[step] = "ElseIf besitzt keinen zugehoerigen If-Step.";
                    else if (blocks.Peek().SeenElse) errors[step] = "ElseIf darf nicht hinter Else stehen.";
                    break;
                case ElseStep:
                    if (blocks.Count == 0) errors[step] = "Else besitzt keinen zugehoerigen If-Step.";
                    else if (blocks.Peek().SeenElse) errors[step] = "Der If-Block enthaelt mehr als einen Else-Step.";
                    else { var block = blocks.Pop(); blocks.Push((block.Step, true)); }
                    break;
                case EndIfStep:
                    if (blocks.Count == 0) errors[step] = "EndIf besitzt keinen zugehoerigen If-Step.";
                    else blocks.Pop();
                    break;
            }
        }
        foreach (var block in blocks) errors[block.Step] = "Fuer diesen If-Step fehlt ein EndIf-Step.";
        return errors;
    }

    public static StepValidationResult ValidateStep(IReadOnlyList<JobStep> steps, JobStep step, IReadOnlyList<JobStep>? referenceSteps = null)
    {
        if (!step.IsEnabled)
            return new(step, true, null);

        var valueError = ValidateValues(step);
        if (valueError != null)
            return new(step, false, valueError);

        var index = IndexOf(steps, step);
        if (GetRequiredSourceIds(step).Any(string.IsNullOrWhiteSpace))
            return new(step, false, "Es wurde kein gueltiger Quell-Step ausgewaehlt.");
        foreach (var sourceId in EnumerateSourceIds(step))
        {
            if (string.IsNullOrWhiteSpace(sourceId)) continue;
            var source = steps.FirstOrDefault(s => s.Id == sourceId);
            if (source == null || !source.IsEnabled || IndexOf(steps, source) >= index)
                return new(step, false, "Der ausgewaehlte Quell-Step ist deaktiviert, nicht vorhanden oder steht nicht davor.");
        }

        if (step is IfStep ifStep && ValidateConditions(steps, index, ifStep.Settings.Conditions) is { } ifError)
            return new(step, false, ifError);
        if (step is ElseIfStep elseIfStep && ValidateConditions(steps, index, elseIfStep.Settings.Conditions) is { } elseIfError)
            return new(step, false, elseIfError);

        var dynamicRoiId = GetDynamicRoiStepId(step);
        if (!string.IsNullOrWhiteSpace(dynamicRoiId)
            && (referenceSteps ?? steps).FirstOrDefault(s => s.Id == dynamicRoiId) is not DynamicRoiStep)
            return new(step, false, "Der ausgewählte dynamische ROI-Step ist nicht vorhanden oder ungültig.");

        var chainError = StepPipelineRegistry.ValidateStepChain(steps.Take(index + 1).Where(s => s.IsEnabled))
            .LastOrDefault(e => e.StepTypeName == step.GetType().Name);
        return chainError == null
            ? new(step, true, null)
            : new(step, false, $"Fehlende Voraussetzung: {chainError.MissingPrerequisite}");
    }

    private static string? ValidateValues(JobStep step)
    {
        const string invalid = "Der Step enthaelt ungueltige oder unvollstaendige Werte.";
        var valid = step switch
        {
            TemplateMatchingStep s => ExistingFile(s.Settings.TemplatePath) && Unit(s.Settings.ConfidenceThreshold) && Roi(s.Settings.EnableROI, s.Settings.ROI),
            ColorDetectionStep s => Unit(s.Settings.ConfidenceThreshold) && s.Settings.MinSize > 0 && s.Settings.MaxSize >= s.Settings.MinSize && s.Settings.MinWidth > 0 && s.Settings.MinHeight > 0 && s.Settings.DownscaleFactor > 0 && Roi(s.Settings.EnableROI, s.Settings.ROI),
            PredictMovementStep s => s.Settings.MinSamples >= 2 && s.Settings.ResetDistanceThreshold >= 0 && s.Settings.MaxSampleAgeMs >= 0
                && s.Settings.MaxPredictionDistance >= 0 && s.Settings.MaxFitError >= 0 && Unit(s.Settings.MinimumConfidence)
                && new[] { "Linear", "Acceleration", "Kalman", "Automatic" }.Contains(s.Settings.PredictionModel),
            DesktopDuplicationStep s => s.Settings.DesktopIdx >= 0,
            ShowImageStep s => Text(s.Settings.WindowName),
            VideoCreationStep s => DirectoryPath(s.Settings.SavePath) && FileName(s.Settings.FileName),
            MakroExecutionStep s => s.Settings.MakroId != null,
            JobExecutionStep s => s.Settings.JobId != null,
            ScriptExecutionStep s => ExistingFile(s.Settings.ScriptPath),
            KlickOnPointStep s => Text(s.Settings.ClickType) && s.Settings.TimeoutMs >= 0,
            KlickOnPoint3DStep s => Text(s.Settings.ClickType) && s.Settings.TimeoutMs >= 0,
            YOLODetectionStep s => Text(s.Settings.Model) && Text(s.Settings.ClassName) && Unit(s.Settings.ConfidenceThreshold) && Roi(s.Settings.EnableROI, s.Settings.ROI),
            TimeoutStep s => s.Settings.DelayMs >= 0,
            ActiveProcessStep s => Text(s.Settings.ProcessName),
            StartProcessStep s => s.Settings.Action == StartProcessAction.Terminate
                ? Text(s.Settings.ProcessName)
                : ExecutablePathResolver.CanResolve(s.Settings.ExecutablePath) && s.Settings.MonitorIndex >= 0,
            FocusProcessStep s => ExecutablePathResolver.CanResolve(s.Settings.ExecutablePath),
            ShowTextStep s => Text(s.Settings.Text) && s.Settings.FontSize > 0 && Unit(s.Settings.Opacity) && s.Settings.DesktopIndex >= 0 && s.Settings.DurationMs >= 0,
            ActiveWindowStep s => Text(s.Settings.ProcessName) && s.Settings.CacheMs >= 0,
            KeyPointMatchingStep s => ExistingFile(s.Settings.TemplatePath) && s.Settings.MinMatchCount > 0 && s.Settings.LowesRatioThreshold is > 0 and <= 1 && Roi(s.Settings.EnableROI, s.Settings.ROI),
            IfStep s => Conditions(s.Settings.Conditions),
            ElseIfStep s => Conditions(s.Settings.Conditions),
            PointComparisonStep s => PointComparison(s.Settings),
            DynamicRoiStep s => s.Settings.Padding >= 0 && Unit(s.Settings.MinimumConfidence)
                && s.Settings.FullSearchInterval >= 0 && s.Settings.ResetAfterMisses >= 0,
            _ => true
        };
        return valid ? null : invalid;
    }

    private static bool Conditions(IEnumerable<StepCondition> conditions)
    {
        var rows = conditions.ToList();
        return rows.Count > 0 && rows.All(c =>
            Text(c.SourceStepId) && Text(c.PropertyPath));
    }

    private static string? ValidateConditions(IReadOnlyList<JobStep> steps, int conditionStepIndex, IEnumerable<StepCondition> conditions)
    {
        foreach (var condition in conditions)
        {
            var source = steps.Take(conditionStepIndex).FirstOrDefault(s => s.Id == condition.SourceStepId && s.IsEnabled);
            if (source is null) return "Eine Bedingung verweist nicht auf einen gültigen vorherigen Step.";
            var output = StepPipelineRegistry.Get(source.GetType())?.Output;
            if (string.IsNullOrWhiteSpace(output) || output == "–") return "Der ausgewählte Step besitzt keinen auswertbaren Rückgabewert.";
            if (!StepResultMetadata.TryGetProperty(output, condition.PropertyPath, out var property)) return "Die ausgewählte Rückgabeeigenschaft existiert nicht mehr.";
            if (!ConditionRules.IsOperatorAllowed(property.PropertyType, condition.Operator))
                return "Der Operator passt nicht zum Datentyp der ausgewählten Eigenschaft.";
            if (!ConditionRules.RequiresComparisonValue(condition.Operator)) continue;

            var comparison = condition.EffectiveComparison;
            if (comparison.Kind == ComparisonOperandKind.Literal)
            {
                if (!ConditionRules.IsComparisonValueValid(property, condition.Operator, comparison.Value))
                    return "Der Vergleichswert besitzt nicht den erwarteten Datentyp.";
                continue;
            }

            var comparisonSource = steps.Take(conditionStepIndex)
                .FirstOrDefault(s => s.Id == comparison.SourceStepId && s.IsEnabled);
            if (comparisonSource is null)
                return "Der JobResult-Vergleich verweist nicht auf einen gültigen vorherigen Step.";
            var comparisonOutput = StepPipelineRegistry.Get(comparisonSource.GetType())?.Output;
            if (string.IsNullOrWhiteSpace(comparisonOutput) || comparisonOutput == "–")
                return "Der ausgewählte JobResult-Step besitzt keinen auswertbaren Rückgabewert.";
            if (string.IsNullOrWhiteSpace(comparison.PropertyPath)
                || !StepResultMetadata.TryGetProperty(comparisonOutput, comparison.PropertyPath, out var comparisonProperty))
                return "Die ausgewählte JobResult-Vergleichseigenschaft existiert nicht mehr.";
            if (comparisonProperty.PropertyType != property.PropertyType)
                return "Beide Vergleichswerte müssen denselben Datentyp besitzen.";
        }
        return null;
    }

    private static bool PointComparison(PointComparisonSettings s)
        => s.Points.Count > 0
           && s.Points.All(p => p.Source == PointEntrySource.Manual || Text(p.SourceDetectionStepId))
           && (s.Mode == PointComparisonMode.Offset
               ? (s.OffsetSettings.ReferenceSource == PointEntrySource.Manual || Text(s.OffsetSettings.ReferenceDetectionStepId))
                 && s.OffsetSettings.OffsetX >= 0 && s.OffsetSettings.OffsetY >= 0
               : s.ExpressionSettings.Expressions.Count > 0
                 && s.ExpressionSettings.Expressions.All(e => e.Axis is "X" or "Y"));

    private static bool Text(string? value) => !string.IsNullOrWhiteSpace(value);
    private static bool Unit(double value) => value is >= 0 and <= 1;
    private static bool ExistingFile(string? path) => Text(path) && File.Exists(path);
    private static bool Roi(bool enabled, OpenCvSharp.Rect roi) => !enabled || (roi.X >= 0 && roi.Y >= 0 && roi.Width > 0 && roi.Height > 0);
    private static bool FileName(string? value) => Text(value) && value!.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 && Path.GetFileName(value) == value;
    private static bool DirectoryPath(string? value)
    {
        if (!Text(value)) return false;
        try { _ = Path.GetFullPath(value!); return value!.IndexOfAny(Path.GetInvalidPathChars()) < 0; }
        catch { return false; }
    }

    private static IEnumerable<string> GetRequiredSourceIds(JobStep step) => step switch
    {
        TemplateMatchingStep s => [s.Settings.SourceCaptureStepId],
        ColorDetectionStep s => [s.Settings.SourceCaptureStepId],
        PredictMovementStep s => [s.Settings.SourceDetectionStepId],
        ShowImageStep s => [s.Settings.SourceCaptureStepId],
        ShowOnDesktopStep s => [s.Settings.SourceDetectionStepId],
        VideoCreationStep s => [s.Settings.SourceCaptureStepId],
        KlickOnPointStep s => [s.Settings.SourceDetectionStepId],
        KlickOnPoint3DStep s => [s.Settings.SourceDetectionStepId],
        YOLODetectionStep s => [s.Settings.SourceCaptureStepId],
        KeyPointMatchingStep s => [s.Settings.SourceCaptureStepId],
        DynamicRoiStep s => [s.Settings.SourceDetectionStepId],
        IfStep s => ConditionSourceIds(s.Settings.Conditions),
        ElseIfStep s => ConditionSourceIds(s.Settings.Conditions),
        PointComparisonStep s => s.Settings.Points
            .Where(p => p.Source == PointEntrySource.JobResult)
            .Select(p => p.SourceDetectionStepId)
            .Concat(s.Settings.Mode == PointComparisonMode.Offset && s.Settings.OffsetSettings.ReferenceSource == PointEntrySource.JobResult
                ? [s.Settings.OffsetSettings.ReferenceDetectionStepId] : []),
        _ => []
    };

    private static IEnumerable<string> ConditionSourceIds(IEnumerable<StepCondition> conditions) =>
        conditions.SelectMany(condition => condition.EffectiveComparison is { Kind: ComparisonOperandKind.JobResult } comparison
            ? new[] { condition.SourceStepId, comparison.SourceStepId ?? string.Empty }
            : new[] { condition.SourceStepId });

    private static string? GetDynamicRoiStepId(JobStep step) => step switch
    {
        TemplateMatchingStep s => s.Settings.DynamicRoiStepId,
        ColorDetectionStep s => s.Settings.DynamicRoiStepId,
        YOLODetectionStep s => s.Settings.DynamicRoiStepId,
        KeyPointMatchingStep s => s.Settings.DynamicRoiStepId,
        _ => null
    };

    /// <summary>
    /// Entfernt nur Referenzen auf Steps, die nicht mehr existieren.
    /// Voruebergehend ungueltige Referenzen (deaktivierter Step oder falsche Reihenfolge)
    /// bleiben erhalten, damit sie nach Reaktivieren oder Zurueckverschieben wieder gueltig werden.
    /// </summary>
    public static void RemoveInvalidSourceSelections(IReadOnlyList<JobStep> steps)
    {
        var existingIds = steps.Select(s => s.Id).ToHashSet(StringComparer.Ordinal);
        for (var i = 0; i < steps.Count; i++)
        {
            VisitSourceProperties(steps[i], (owner, property) =>
            {
                if (property.GetValue(owner) is string id && id.Length > 0 && !existingIds.Contains(id) && property.CanWrite)
                    property.SetValue(owner, string.Empty);
            });
        }
    }

    private static IEnumerable<string> EnumerateSourceIds(JobStep step)
    {
        var ids = new List<string>();
        VisitSourceProperties(step, (owner, property) => ids.Add((string?)property.GetValue(owner) ?? ""));
        if (step is PointComparisonStep p && p.Settings.Mode == PointComparisonMode.Offset && p.Settings.OffsetSettings.ReferenceSource == PointEntrySource.JobResult)
            ids.Add(p.Settings.OffsetSettings.ReferenceDetectionStepId);
        return ids;
    }

    private static void VisitSourceProperties(object? value, Action<object, PropertyInfo> visitor, HashSet<object>? seen = null)
    {
        if (value == null || value is string || value.GetType().IsPrimitive || value.GetType().IsEnum) return;
        seen ??= new(ReferenceEqualityComparer.Instance);
        if (!seen.Add(value)) return;
        if (value is IEnumerable sequence) { foreach (var item in sequence) VisitSourceProperties(item, visitor, seen); return; }
        if (value.GetType().Namespace != typeof(JobStep).Namespace) return;
        foreach (var property in value.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!property.CanRead || property.GetIndexParameters().Length != 0) continue;
            if (property.PropertyType == typeof(string) && property.Name.StartsWith("Source", StringComparison.Ordinal) && property.Name.EndsWith("StepId", StringComparison.Ordinal))
                visitor(value, property);
            else if (property.PropertyType != typeof(string))
                VisitSourceProperties(property.GetValue(value), visitor, seen);
        }
    }

    private static int IndexOf(IReadOnlyList<JobStep> steps, JobStep step)
    {
        for (var i = 0; i < steps.Count; i++) if (ReferenceEquals(steps[i], step)) return i;
        return -1;
    }
}
