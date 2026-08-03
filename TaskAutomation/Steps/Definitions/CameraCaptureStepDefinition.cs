using System.Text.Json;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps.Definitions;

public sealed class CameraCaptureStepDefinition : StepDefinition<CameraCaptureStep>
{
    public const string CameraFieldId = "camera";

    public override StepDescriptor Descriptor { get; } = new(
        TypeId: "camera_capture",
        CategoryId: "BildAufnehmen",
        DisplayNameKey: "Step.Type.CameraCapture",
        DescriptionKey: "Step.Description.CameraCapture",
        IconKey: "camera",
        Fields:
        [
            new StepFieldDescriptor(
                CameraFieldId,
                "Ui.Step.Camera.Camera",
                StepValueKind.Object,
                Required: true,
                DefaultValue: JsonSerializer.SerializeToNode(EmptySelection),
                EditorHint: StepEditorHints.CameraPicker)
        ],
        Presentation: new StepPresentationDescriptor(
            EditorSections: [new("general", null, [CameraFieldId])],
            SummaryItems: [new(CameraFieldId)],
            DetailFieldIds: [CameraFieldId]));

    public override CameraCaptureStep CreateDefaultStep() => new();

    protected override StepDraft Read(CameraCaptureStep step)
    {
        var settings = step.Settings;
        var draft = new StepDraft(Descriptor.TypeId);
        draft.Values[CameraFieldId] = JsonSerializer.SerializeToNode(new StepCameraSelectionValue(
            settings.CameraId,
            settings.CameraName,
            settings.QualityMode.ToString(),
            settings.Width,
            settings.Height,
            settings.FramesPerSecond,
            settings.PixelFormat));
        return draft;
    }

    protected override void Apply(StepDraft draft, CameraCaptureStep step)
    {
        var value = ReadSelection(draft);
        step.Settings.CameraId = value.CameraId;
        step.Settings.CameraName = value.CameraName;
        step.Settings.QualityMode = Enum.TryParse<CameraQualityMode>(value.QualityMode, out var mode)
            ? mode
            : CameraQualityMode.Automatic;
        step.Settings.Width = value.Width;
        step.Settings.Height = value.Height;
        step.Settings.FramesPerSecond = value.FramesPerSecond;
        step.Settings.PixelFormat = value.PixelFormat;
    }

    protected override IReadOnlyList<StepValidationIssue> ValidateCustomDraft(StepDraft draft)
    {
        var value = ReadSelection(draft);
        if (string.IsNullOrWhiteSpace(value.CameraId))
            return [new("StepValidation.Required", CameraFieldId)];
        if (!Enum.TryParse<CameraQualityMode>(value.QualityMode, out var mode))
            return [new("StepValidation.Invalid", CameraFieldId)];
        if (mode == CameraQualityMode.Specific
            && (value.Width <= 0 || value.Height <= 0 || value.FramesPerSecond < 0
                || string.IsNullOrWhiteSpace(value.PixelFormat)))
            return [new("StepValidation.Invalid", CameraFieldId)];
        return [];
    }

    private static StepCameraSelectionValue ReadSelection(StepDraft draft)
    {
        try
        {
            return draft.Values.GetValueOrDefault(CameraFieldId)?.Deserialize<StepCameraSelectionValue>()
                ?? EmptySelection;
        }
        catch (JsonException)
        {
            return EmptySelection;
        }
        catch (InvalidOperationException)
        {
            return EmptySelection;
        }
    }

    private static readonly StepCameraSelectionValue EmptySelection = new(
        string.Empty, string.Empty, nameof(CameraQualityMode.Automatic), 0, 0, 0, string.Empty);
}
