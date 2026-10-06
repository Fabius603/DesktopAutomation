using System.Globalization;
using TaskAutomation.Steps;

namespace TaskAutomation.Logging;

/// <summary>Explicit, privacy-preserving summaries for every built-in result family. Never reflects values.</summary>
public static class StepLogResults
{
    public static bool Supports(Type type) => typeof(IActionExecutionResult).IsAssignableFrom(type)
        || typeof(ICaptureStepResult).IsAssignableFrom(type) || typeof(IDetectionStepResult).IsAssignableFrom(type)
        || typeof(WindowsStateQueryResult).IsAssignableFrom(type) || typeof(IProcessReferenceResult).IsAssignableFrom(type)
        || type == typeof(OcrResult) || type == typeof(FileSystemOperationResult) || type == typeof(SaveImageResult)
        || type == typeof(UserChoiceResult) || type == typeof(DynamicRoiResult) || type == typeof(PointComparisonResult);

    public static Dictionary<string, string?> Summarize(object? result)
    {
        var values = new Dictionary<string, string?>();
        void Add(string key, object? value) => values[key] = Convert.ToString(value, CultureInfo.InvariantCulture);
        if (result is null) return values;
        Add("ResultKind", result.GetType().Name);
        Add("SummarySupported", Supports(result.GetType()));
        if (result is StepResultBase common) Add("WasExecuted", common.WasExecuted);
        if (result is IActionExecutionResult action) { Add("Success", action.Success); if (action.SkipReason is not null) Add("SkipReason", action.SkipReason); }
        if (result is ICaptureStepResult capture)
        {
            Add("HasImage", capture.Image is not null); Add("Width", capture.Bounds.Width); Add("Height", capture.Bounds.Height);
            Add("IsFresh", capture.IsFresh);
            Add("FrameVersion", capture.FrameVersion); Add("FrameTimestamp", capture.FrameTimestamp);
        }
        if (result is IDetectionStepResult detection)
        {
            Add("Found", detection.Found); Add("Confidence", detection.Confidence);
            Add("DetectionCount", detection.AllDetections.Count); Add("SourceCaptureIsFresh", detection.SourceCaptureIsFresh);
            Add("SourceFrameVersion", detection.SourceFrameVersion); Add("SourceFrameTimestamp", detection.SourceFrameTimestamp);
            if (detection.Point is { } point) { Add("PointX", point.X); Add("PointY", point.Y); }
        }
        if (result is IProcessReferenceResult processReference)
        {
            Add("HasProcess", processReference.Process is not null);
            if (processReference.Process is { } reference) { Add("ProcessId", reference.ProcessId); Add("ProcessStartUtc", reference.StartTimeUtc.ToString("O")); }
        }
        switch (result)
        {
            case OcrResult ocr:
                Add("Found", ocr.Found); Add("WordCount", ocr.WordCount); Add("LineCount", ocr.LineCount);
                Add("Confidence", ocr.Confidence); Add("SourceCaptureIsFresh", ocr.SourceCaptureIsFresh); break;
            case FileSystemOperationResult files:
                Add("Operation", files.Operation); Add("ItemType", files.ItemType); Add("AffectedCount", files.AffectedCount);
                Add("AffectedFileCount", files.AffectedFileCount); Add("AffectedDirectoryCount", files.AffectedDirectoryCount);
                Add("AffectedBytes", files.AffectedBytes); Add("Path", string.IsNullOrEmpty(files.TargetPath) ? files.SourcePath : files.TargetPath); break;
            case SaveImageResult image:
                Add("Path", image.FilePath); Add("FileSizeBytes", image.FileSizeBytes); Add("Format", image.Format);
                Add("Width", image.Width); Add("Height", image.Height); break;
            case UserChoiceResult choice:
                Add("SelectedIndex", choice.SelectedIndex); Add("WasCancelled", choice.WasCancelled); break;
            case DynamicRoiResult roi:
                Add("RoiUpdated", roi.RoiUpdated); Add("RoiReset", roi.RoiReset);
                Add("ConsecutiveMisses", roi.ConsecutiveMisses); Add("FullSearchInterval", roi.FullSearchInterval); break;
            case PointComparisonResult comparison:
                Add("Matches", comparison.Matches); Add("MatchCount", comparison.MatchCount); Add("TotalCount", comparison.TotalCount); break;
            case ActiveProcessResult active: Add("IsRunning", active.IsRunning); Add("MatchCount", active.MatchCount); break;
            case ActiveWindowResult active: Add("IsActive", active.IsActive); break;
            case GetProcessResult process: Add("Found", process.Found); break;
            case WindowsSettingChangeResult setting:
                Add("Status", setting.Status); Add("RestartRequired", setting.RestartRequired); break;
            case KlickOnPoint3DResult click:
                Add("AppliedDeltaX", click.AppliedDeltaX); Add("AppliedDeltaY", click.AppliedDeltaY);
                Add("MovementBlocked", click.MovementBlocked); break;
            case PredictMovementResult movement: Add("IsPredicted", movement.IsPredicted); break;
        }
        if (result is WindowsStateQueryResult query)
        {
            Add("Status", query.Status);
            // Never persist clipboard text, window titles, device/session names, arbitrary values, or item lists.
            switch (query)
            {
                case NetworkConnectivityQueryResult network: Add("IsConnected", network.IsConnected); Add("Connectivity", network.Connectivity); Add("ConnectionType", network.ConnectionType); Add("Count", network.Count); break;
                case AudioDevicesQueryResult audio: Add("Exists", audio.Exists); Add("Count", audio.Count); Add("DeviceState", audio.DeviceState); break;
                case AudioVolumeQueryResult audio: Add("Exists", audio.Exists); Add("IsMuted", audio.IsMuted); Add("Percentage", audio.Percentage); break;
                case SessionStateQueryResult session: Add("IsActive", session.IsActive); Add("SessionState", session.SessionState); break;
                case PowerStatusQueryResult power: Add("IsConnected", power.IsConnected); Add("IsCharging", power.IsCharging); Add("Percentage", power.Percentage); Add("PowerSource", power.PowerSource); break;
                case MonitorCollectionQueryResult monitors: Add("IsConnected", monitors.IsConnected); Add("Count", monitors.Count); break;
                case DeviceCollectionQueryResult devices: Add("Exists", devices.Exists); Add("IsConnected", devices.IsConnected); Add("Count", devices.Count); Add("DeviceState", devices.DeviceState); break;
                case FileSystemPathQueryResult file: Add("Exists", file.Exists); Add("Count", file.Count); Add("Value", file.Value); Add("Path", file.Path); break;
                case ProcessRunningQueryResult process: Add("Exists", process.Exists); Add("Count", process.Count); break;
                case ForegroundWindowQueryResult window: Add("Exists", window.Exists); break;
                case InputIdleQueryResult idle: Add("IdleMilliseconds", idle.IdleMilliseconds); break;
                case ClipboardContentQueryResult clipboard: Add("Exists", clipboard.Exists); break;
                case PrinterStatusQueryResult printer: Add("Exists", printer.Exists); Add("Count", printer.Count); Add("DeviceState", printer.DeviceState); break;
                case StorageDrivesQueryResult drives: Add("Exists", drives.Exists); Add("IsConnected", drives.IsConnected); Add("Count", drives.Count); Add("FreeSpaceGb", drives.FreeSpaceGb); break;
                case SystemSettingsQueryResult setting: Add("IsEnabled", setting.IsEnabled); break;
                case SecurityStatusQueryResult security: Add("Exists", security.Exists); Add("IsEnabled", security.IsEnabled); Add("OnOffState", security.OnOffState); break;
                case WindowsUpdateStatusQueryResult update: Add("PendingRestart", update.PendingRestart); break;
                case SystemLifecycleQueryResult system: Add("UptimeMilliseconds", system.UptimeMilliseconds); break;
            }
        }
        return values;
    }
}
