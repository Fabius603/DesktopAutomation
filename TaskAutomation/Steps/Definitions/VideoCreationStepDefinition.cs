using System.Text.Json;
using System.Text.Json.Nodes;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps.Definitions;

public sealed class VideoCreationStepDefinition : StepDefinition<VideoCreationStep>
{
    public override StepDescriptor Descriptor { get; } = OutputFileStepDefinitionSupport.CreateDescriptor(
        "video_creation", "Step.Type.VideoCreation", "Step.Description.VideoCreation", "output.mp4",
        StepKnownDirectory.Videos);

    public override VideoCreationStep CreateDefaultStep() => new();

    protected override StepDraft Read(VideoCreationStep step)
    {
        var draft = new StepDraft(Descriptor.TypeId);
        draft.Values[OutputFileStepDefinitionSupport.SavePathFieldId] = JsonValue.Create(step.Settings.SavePath);
        draft.Values[OutputFileStepDefinitionSupport.FileNameFieldId] = JsonValue.Create(step.Settings.FileName);
        draft.Values[OutputFileStepDefinitionSupport.ImageSourceFieldId] =
            JsonSerializer.SerializeToNode(step.Settings.ImageSource);
        draft.Values[OutputFileStepDefinitionSupport.OverlayFieldId] =
            VisualOverlayDefinitionValue.Write(step.Settings.Overlay, step.Settings.DetectionsSource);
        return draft;
    }

    protected override void Apply(StepDraft draft, VideoCreationStep step)
    {
        step.Settings.SavePath = DefinitionValueReader.String(draft, OutputFileStepDefinitionSupport.SavePathFieldId);
        step.Settings.FileName = DefinitionValueReader.String(draft, OutputFileStepDefinitionSupport.FileNameFieldId);
        step.Settings.ImageSource = DefinitionValueReader.Binding(draft, OutputFileStepDefinitionSupport.ImageSourceFieldId);
        step.Settings.Overlay = VisualOverlayDefinitionValue.Read(draft, OutputFileStepDefinitionSupport.OverlayFieldId);
        step.Settings.DetectionsSource = new ResultBinding();
    }

    protected override IReadOnlyList<StepValidationIssue> ValidateCustomDraft(StepDraft draft) =>
        OutputFileStepDefinitionSupport.Validate(draft, requireImageExtension: false);
}
