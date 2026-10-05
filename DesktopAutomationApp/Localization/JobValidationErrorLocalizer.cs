using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;
using TaskAutomation.Steps.Definitions;

namespace DesktopAutomationApp.Localization;

internal static class JobValidationErrorLocalizer
{
    private const string InputRequiredPrefix = "Für die Eingabe '";
    private const string InputRequiredSuffix = "' wurde keine Variable ausgewählt.";

    public static string? Localize(string? error, JobStep? step = null)
    {
        if (string.IsNullOrEmpty(error)) return error;

        error = error.Replace("StepValidation.DuplicateStepId", Loc.Get("Ui.Job.Validation.DuplicateStepId"), StringComparison.Ordinal)
            .Replace("StepValidation.DuplicateValueId", Loc.Get("Ui.Job.Validation.DuplicateValueId"), StringComparison.Ordinal)
            .Replace("StepValidation.LocalValueOwnership", Loc.Get("Ui.Job.Validation.LocalValueOwnership"), StringComparison.Ordinal);

        foreach (var (original, key) in new[]
                 {
                     ("ElseIf besitzt keinen zugehoerigen If-Step.", "Ui.Job.Validation.OrphanElseIf"),
                     ("Else besitzt keinen zugehoerigen If-Step.", "Ui.Job.Validation.OrphanElse"),
                     ("ElseIf darf nicht hinter Else stehen.", "Ui.Job.Validation.ElseIfAfterElse"),
                     ("Der If-Block enthaelt mehr als einen Else-Step.", "Ui.Job.Validation.DuplicateElse"),
                     ("EndIf besitzt keinen zugehoerigen If-Step.", "Ui.Job.Validation.OrphanBlockEnd"),
                     ("Fuer diesen If-Step fehlt ein EndIf-Step.", "Ui.Job.Validation.MissingBlockEnd")
                 })
            error = error.Replace(original, Loc.Get(key), StringComparison.Ordinal);

        var descriptor = step is not null && BuiltInStepDefinitions.Instance.TryGetByType(step.GetType(), out var definition)
            ? definition.Descriptor : null;
        error = Regex.Replace(error, @"(?m)^(?<field>[^:\r\n]+): (?<code>StepValidation\.[A-Za-z]+)\r?$", match =>
        {
            var fieldId = match.Groups["field"].Value;
            var field = descriptor?.Fields.FirstOrDefault(candidate => candidate.Id == fieldId);
            var label = field is null ? StepLocalization.Type(step?.GetType() ?? typeof(JobStep)) : Loc.Get(field.LabelKey);
            return FormatIssue(new StepValidationIssue(match.Groups["code"].Value, fieldId), label);
        });

        return error.StartsWith(InputRequiredPrefix, StringComparison.Ordinal)
               && error.EndsWith(InputRequiredSuffix, StringComparison.Ordinal)
            ? Loc.Format(
                "Ui.Job.Validation.InputRequired",
                error[InputRequiredPrefix.Length..^InputRequiredSuffix.Length])
            : error;
    }
    public static string FormatIssue(StepValidationIssue issue, string label) => issue.Code switch
    {
        "StepValidation.Required" => Loc.Format("Ui.Step.Generated.Validation.Required", label),
        "StepValidation.Integer" => Loc.Format("Ui.Step.Generated.Validation.Integer", label),
        "StepValidation.Boolean" => Loc.Format("Ui.Step.Generated.Validation.Boolean", label),
        "StepValidation.Number" => Loc.Format("Ui.Step.Generated.Validation.Number", label),
        "StepValidation.Minimum" when issue.Arguments?.GetValueOrDefault("minimum") is { } minimum
            => Loc.Format("Ui.Step.Generated.Validation.Minimum", label, minimum),
        "StepValidation.Maximum" when issue.Arguments?.GetValueOrDefault("maximum") is { } maximum
            => Loc.Format("Ui.Step.Generated.Validation.Maximum", label, maximum),
        _ => Loc.Get("Ui.Step.Generated.Validation.Invalid")
    };
}
