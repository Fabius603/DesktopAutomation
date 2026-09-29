using TaskAutomation.Events;
using TaskAutomation.Steps;

namespace TaskAutomation.Tests.TestDoubles;

internal sealed record TextOverlayCall(string StepKey, string Text, float FontSize, byte R, byte G, byte B, byte A,
    int DesktopIndex, int OffsetX, int OffsetY, int DurationMs, bool ClearOnJobEnd);

internal sealed class RecordingDesktopResultOverlay : IDesktopResultOverlay
{
    public Action<TextOverlayCall>? OnShowText { get; set; }
    public List<TextOverlayCall> TextCalls { get; } = [];
    public List<IReadOnlyList<DetectionItem>> ResultCalls { get; } = [];
    public List<(string StepKey, ResolvedVisualOverlay Overlay)> OverlayCalls { get; } = [];
    public List<string> ClearedOverlayKeys { get; } = [];
    public int ClearCalls { get; private set; }
    public int ClearTextCalls { get; private set; }
    public int JobEndedCalls { get; private set; }
    public List<IReadOnlyCollection<string>> JobEndedStepKeys { get; } = [];

    public void ShowResult(IReadOnlyList<DetectionItem> allDetections) => ResultCalls.Add(allDetections);
    public void Clear() => ClearCalls++;
    public void ShowOverlay(string stepKey, ResolvedVisualOverlay overlay) => OverlayCalls.Add((stepKey, overlay));
    public void ClearOverlay(string stepKey) => ClearedOverlayKeys.Add(stepKey);
    public void ShowText(string stepKey, string text, float fontSize, byte r, byte g, byte b, byte a,
        int desktopIndex, int offsetX, int offsetY, int durationMs, bool clearOnJobEnd) =>
        RecordText(new(stepKey, text, fontSize, r, g, b, a, desktopIndex, offsetX, offsetY, durationMs, clearOnJobEnd));
    public void ClearText() => ClearTextCalls++;
    public void OnJobEnded() => JobEndedCalls++;
    public void OnJobEnded(IReadOnlyCollection<string> stepKeys) => JobEndedStepKeys.Add(stepKeys);
    public void Dispose() { }

    private void RecordText(TextOverlayCall call)
    {
        TextCalls.Add(call);
        OnShowText?.Invoke(call);
    }
}
