using System.Text.Json;
using System.Text.Json.Nodes;
using OpenCvSharp;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps.Definitions;

internal static class ImageDetectionStepDefinitionSupport
{
    public const string ImageSourceFieldId = "image_source";
    public const string RoiFieldId = "roi";

    public static StepFieldDescriptor ImageSource(int order = 0) => new(
        ImageSourceFieldId,
        "Ui.Step.Settings.CaptureStep",
        StepValueKind.ResultBinding,
        Required: true,
        EditorHint: StepEditorHints.ResultBindingPicker,
        Order: order,
        InputContractId: "image");

    public static StepFieldDescriptor Roi(int order) => new(
        RoiFieldId,
        "Ui.Step.Settings.ROI",
        StepValueKind.Object,
        DefaultValue: JsonSerializer.SerializeToNode(new StepRoiSelectionValue(false, 0, 0, 0, 0, null)),
        EditorHint: StepEditorHints.RoiPicker,
        Advanced: true,
        Order: order,
        RoiPickerOptions: new StepRoiPickerOptions("dynamicRoi"));

    public static JsonNode? WriteRoi(bool enabled, Rect roi, ResultBinding dynamicSource) =>
        JsonSerializer.SerializeToNode(new StepRoiSelectionValue(
            enabled, roi.X, roi.Y, roi.Width, roi.Height, JsonSerializer.SerializeToNode(dynamicSource)));

    public static (bool Enabled, Rect Roi, ResultBinding DynamicSource) ReadRoi(StepDraft draft)
    {
        try
        {
            var value = draft.Values.GetValueOrDefault(RoiFieldId)?.Deserialize<StepRoiSelectionValue>();
            if (value is null) return (false, new Rect(), new ResultBinding());
            ResultBinding dynamicSource;
            try { dynamicSource = value.DynamicSource?.Deserialize<ResultBinding>() ?? new ResultBinding(); }
            catch (JsonException) { dynamicSource = new ResultBinding(); }
            return (value.Enabled, new Rect(value.X, value.Y, value.Width, value.Height), dynamicSource);
        }
        catch (JsonException) { return (false, new Rect(), new ResultBinding()); }
        catch (InvalidOperationException) { return (false, new Rect(), new ResultBinding()); }
    }

    public static StepValidationIssue? ValidateCommon(StepDraft draft)
    {
        var roi = ReadRoi(draft);
        if (roi.Enabled && (roi.Roi.X < 0 || roi.Roi.Y < 0 || roi.Roi.Width <= 0 || roi.Roi.Height <= 0))
            return new("StepValidation.Invalid", RoiFieldId);
        return null;
    }

}
