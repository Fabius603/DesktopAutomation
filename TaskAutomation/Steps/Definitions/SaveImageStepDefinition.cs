using System.Text.Json;
using System.Text.Json.Nodes;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps.Definitions;

public sealed class SaveImageStepDefinition : StepDefinition<SaveImageStep>
{
    public override StepDescriptor Descriptor { get; } = OutputFileStepDefinitionSupport.CreateDescriptor(
        "save_image", "Step.Type.SaveImage", "Step.Description.SaveImage", "image.png",
        StepKnownDirectory.Pictures,
        "Ui.Step.SaveImage.FormatsHint");

    public override SaveImageStep CreateDefaultStep() => new();

    protected override StepDraft Read(SaveImageStep step)
    {
        var draft = new StepDraft(Descriptor.TypeId);
        draft.Values[OutputFileStepDefinitionSupport.SavePathFieldId] = JsonValue.Create(step.Settings.SavePath);
        draft.Values[OutputFileStepDefinitionSupport.FileNameFieldId] = JsonValue.Create(step.Settings.FileName);
        draft.Values[OutputFileStepDefinitionSupport.ImageSourceFieldId] =
            JsonSerializer.SerializeToNode(step.Settings.ImageSource);
        draft.Values[OutputFileStepDefinitionSupport.OverlayFieldId] =
            VisualOverlayDefinitionValue.Write(step.Settings.Overlay);
        return draft;
    }

    protected override void Apply(StepDraft draft, SaveImageStep step)
    {
        step.Settings.SavePath = DefinitionValueReader.String(draft, OutputFileStepDefinitionSupport.SavePathFieldId);
        step.Settings.FileName = DefinitionValueReader.String(draft, OutputFileStepDefinitionSupport.FileNameFieldId);
        step.Settings.ImageSource = DefinitionValueReader.Binding(draft, OutputFileStepDefinitionSupport.ImageSourceFieldId);
        step.Settings.Overlay = VisualOverlayDefinitionValue.Read(draft, OutputFileStepDefinitionSupport.OverlayFieldId);
    }

    protected override IReadOnlyList<StepValidationIssue> ValidateCustomDraft(StepDraft draft) =>
        OutputFileStepDefinitionSupport.Validate(draft, requireImageExtension: true);
}
