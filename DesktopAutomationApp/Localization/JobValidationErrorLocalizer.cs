using System;

namespace DesktopAutomationApp.Localization;

internal static class JobValidationErrorLocalizer
{
    private const string InputRequiredPrefix = "Für die Eingabe '";
    private const string InputRequiredSuffix = "' wurde keine Variable ausgewählt.";

    public static string? Localize(string? error)
    {
        if (string.IsNullOrEmpty(error)) return error;

        return error.StartsWith(InputRequiredPrefix, StringComparison.Ordinal)
               && error.EndsWith(InputRequiredSuffix, StringComparison.Ordinal)
            ? Loc.Format(
                "Ui.Job.Validation.InputRequired",
                error[InputRequiredPrefix.Length..^InputRequiredSuffix.Length])
            : error;
    }
}
