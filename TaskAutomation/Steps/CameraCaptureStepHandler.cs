using Microsoft.Extensions.Logging;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps;

public sealed class CameraCaptureStepHandler
    : JobStepHandler<CameraCaptureStep, CameraCaptureResult>
{
    protected override async Task<CameraCaptureResult> ExecuteCoreAsync(
        CameraCaptureStep step,
        IStepPipelineContext context,
        CancellationToken cancellationToken)
    {
        context.Logger.LogDebug(
            "CameraCaptureStepHandler: Aufnahme von Kamera {CameraName}.",
            step.Settings.CameraName);

        var capture = await context.CameraCaptureService
            .CaptureAsync(step.Settings.CameraId, cancellationToken)
            .ConfigureAwait(false);
        var bounds = new System.Drawing.Rectangle(0, 0, capture.Image.Width, capture.Image.Height);

        context.Logger.LogInformation(
            "CameraCaptureStepHandler: Kamera {CameraName} mit {Width}x{Height} aufgenommen.",
            step.Settings.CameraName, capture.Image.Width, capture.Image.Height);

        return new CameraCaptureResult
        {
            WasExecuted = true,
            Image = capture.Image,
            Bounds = bounds,
            Offset = System.Drawing.Point.Empty,
            IsFresh = true,
            CaptureTimestampUtc = capture.CaptureTimestampUtc
        };
    }

    protected override CameraCaptureResult CreateDefault() => CameraCaptureResult.Default;
}
