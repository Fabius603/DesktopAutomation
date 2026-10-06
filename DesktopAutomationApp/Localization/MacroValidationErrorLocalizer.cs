using TaskAutomation.Makros;
namespace DesktopAutomationApp.Localization;
public static class MacroValidationErrorLocalizer
{
    public static string Localize(MakroValidationError error) => error switch
    {
        MakroValidationError.TextRequired => Loc.Get("Ui.Macro.Validation.TextRequired"),
        MakroValidationError.KeyCombinationInvalid => Loc.Get("Ui.Macro.Validation.KeyCombinationInvalid"),
        MakroValidationError.GroupStructureInvalid => Loc.Get("Ui.Macro.Validation.GroupStructureInvalid"),
        _ => MakroValidation.Describe(error)
    };
}
