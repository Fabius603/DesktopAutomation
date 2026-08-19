using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps.Definitions;

internal static class OutputFileStepDefinitionSupport
{
    public const string SavePathFieldId = "save_path";
    public const string FileNameFieldId = "file_name";
    public const string ImageSourceFieldId = "image_source";
    public const string OverlayFieldId = "overlay";

    private static readonly string[] ImageExtensions =
        [".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tif", ".tiff"];

    public static StepDescriptor CreateDescriptor(
        string typeId,
        string displayNameKey,
        string descriptionKey,
        string defaultFileName,
        StepKnownDirectory suggestedDirectory,
        string? editorDescriptionKey = null) => new(
        typeId,
        "AnzeigenSpeichern",
        displayNameKey,
        descriptionKey,
        "file-output",
        [
            new(ImageSourceFieldId, "Ui.Step.Settings.ImageSource", StepValueKind.ResultBinding, true,
                EditorHint: StepEditorHints.ValueReferencePicker, InputContractId: "image", Order: 0),
            new(SavePathFieldId, "Ui.Step.Settings.SavePath", StepValueKind.DirectoryPath, true,
                DefaultValue: JsonValue.Create(string.Empty), EditorHint: StepEditorHints.DirectoryPicker, Order: 1,
                DirectoryPickerOptions: new(suggestedDirectory, "DesktopAutomation")),
            new(FileNameFieldId, "Ui.Step.Settings.FileName", StepValueKind.Text, true,
                DefaultValue: JsonValue.Create(defaultFileName), Order: 2),
            new(OverlayFieldId, "Ui.Step.Settings.Overlays", StepValueKind.Object,
                DefaultValue: JsonSerializer.SerializeToNode(new VisualOverlaySettings()),
                EditorHint: StepEditorHints.VisualOverlay, Advanced: true, Order: 3,
                VisualOverlayOptions: new("detections", "text"))
        ],
        new(
            [new("general", null, [ImageSourceFieldId, SavePathFieldId, FileNameFieldId]),
             new("overlay", "Ui.Step.Settings.Overlays", [OverlayFieldId], 1, true, false)],
            [new(FileNameFieldId, StepSummaryValueFormat.FileName), new(ImageSourceFieldId)],
            [ImageSourceFieldId, SavePathFieldId, FileNameFieldId, OverlayFieldId],
            editorDescriptionKey));

    public static IReadOnlyList<StepValidationIssue> Validate(
        StepDraft draft,
        bool requireImageExtension)
    {
        if (!DefinitionValueReader.Binding(draft, ImageSourceFieldId).IsConfigured)
            return [new("StepValidation.Required", ImageSourceFieldId)];
        if (!IsDirectoryPath(DefinitionValueReader.String(draft, SavePathFieldId)))
            return [new("StepValidation.Invalid", SavePathFieldId)];
        var fileName = DefinitionValueReader.String(draft, FileNameFieldId);
        if (!(requireImageExtension ? IsImageFileName(fileName) : IsFileName(fileName)))
            return [new("StepValidation.Invalid", FileNameFieldId)];
        return VisualOverlayDefinitionValue.IsValid(
            VisualOverlayDefinitionValue.Read(draft, OverlayFieldId), false)
            ? []
            : [new("StepValidation.Invalid", OverlayFieldId)];
    }

    private static bool IsDirectoryPath(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        try
        {
            _ = Path.GetFullPath(value);
            return value.IndexOfAny(Path.GetInvalidPathChars()) < 0;
        }
        catch { return false; }
    }

    private static bool IsFileName(string value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.IndexOfAny(Path.GetInvalidFileNameChars()) < 0
        && string.Equals(Path.GetFileName(value), value, StringComparison.Ordinal);

    private static bool IsImageFileName(string value) =>
        IsFileName(value)
        && ImageExtensions.Contains(Path.GetExtension(value), StringComparer.OrdinalIgnoreCase);
}
