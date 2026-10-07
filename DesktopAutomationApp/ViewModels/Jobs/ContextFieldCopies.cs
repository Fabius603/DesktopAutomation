namespace DesktopAutomationApp.ViewModels;

/// <summary>Copies the editor draft and delegates reference semantics to the existing picker.</summary>
internal static class ContextFieldCopies
{
    public static void Copy(GeneratedStepFieldViewModel? source, GeneratedStepFieldViewModel? target)
    {
        if (source is null || target is null) return;
        target.InputText = source.InputText;
        if (source.InputReferenceEditor?.Picker is { } picker && target.InputReferenceEditor?.Picker is { } destination)
            destination.Load(picker.ToBinding());
    }
}
