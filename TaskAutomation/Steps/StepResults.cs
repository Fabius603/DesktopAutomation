using System;
using System.Drawing;
using TaskAutomation.WindowsIntegration;

namespace TaskAutomation.Steps;

[AttributeUsage(AttributeTargets.Property)]
public sealed class ResultHiddenAttribute : Attribute;

/// <summary>Technical execution state shared by all step results. It is not a selectable payload.</summary>
public abstract record StepResultBase
{
    [ResultHidden]
    public bool WasExecuted { get; init; }
}

public sealed record DetectionItem
{
    [ResultProperty("center")]
    public Point Center { get; init; }
    [ResultProperty("bounding_box")]
    public Rectangle? BoundingBox { get; init; }
    [ResultProperty("confidence")]
    public double Confidence { get; init; }
}

/// <summary>Stable identity for one process instance. Start time prevents PID reuse.</summary>
public sealed record RuntimeProcessReference
{
    [ResultProperty("process_id")]
    public int ProcessId { get; init; }
    [ResultProperty("start_time_utc")]
    public DateTime StartTimeUtc { get; init; }
    [ResultProperty("process_name")]
    public string ProcessName { get; init; } = string.Empty;
    [ResultProperty("executable_path")]
    public string ExecutablePath { get; init; } = string.Empty;
    [ResultProperty("window_handle")]
    public long WindowHandle { get; init; }
}

/// <summary>Internal capture data contract used for coordinate conversion.</summary>
public interface ICaptureStepResult
{
    [ResultProperty("image")]
    Bitmap? Image { get; }
    [ResultProperty("bounds")]
    Rectangle Bounds { get; }
    [ResultProperty("offset")]
    Point Offset { get; }
    [ResultProperty("is_fresh")]
    bool IsFresh { get; }
    [ResultProperty("capture_timestamp_utc")]
    DateTime CaptureTimestampUtc { get; }
}

/// <summary>Internal detection data contract. The concrete result still belongs to one step type.</summary>
public interface IDetectionStepResult
{
    [ResultProperty("found")]
    bool Found { get; }
    [ResultProperty("point")]
    Point? Point { get; }
    [ResultProperty("bounding_box")]
    Rectangle? BoundingBox { get; }
    [ResultProperty("confidence")]
    double Confidence { get; }
    [ResultProperty("source_capture_is_fresh")]
    bool SourceCaptureIsFresh { get; }
    [ResultProperty("source_capture_timestamp_utc")]
    DateTime SourceCaptureTimestampUtc { get; }
    [ResultProperty("all_detections")]
    IReadOnlyList<DetectionItem> AllDetections { get; }
}

/// <summary>Technical action outcome; these fields are logged but never offered as user data.</summary>
public interface IActionExecutionResult
{
    [ResultHidden] bool Success { get; }
    [ResultHidden] string? ErrorMessage { get; }
}

/// <summary>Common output contract for steps that identify or operate on one process/window.</summary>
public interface IProcessReferenceResult
{
    [ResultProperty("process")]
    RuntimeProcessReference? Process { get; }
}

public sealed record DesktopDuplicationResult : StepResultBase, ICaptureStepResult
{
    public Bitmap? Image { get; init; }
    public Rectangle Bounds { get; init; }
    public Point Offset { get; init; }
    public bool IsFresh { get; init; } = true;
    public DateTime CaptureTimestampUtc { get; init; } = DateTime.UtcNow;
    [ResultProperty("has_image")]
    public bool HasImage => Image is not null;
    public static readonly DesktopDuplicationResult Default = new();
}

public sealed record CameraCaptureResult : StepResultBase, ICaptureStepResult
{
    public Bitmap? Image { get; init; }
    public Rectangle Bounds { get; init; }
    public Point Offset { get; init; }
    public bool IsFresh { get; init; } = true;
    public DateTime CaptureTimestampUtc { get; init; } = DateTime.UtcNow;
    [ResultProperty("has_image")]
    public bool HasImage => Image is not null;
    public static readonly CameraCaptureResult Default = new();
}

public sealed record ProcessDuplicationResult : StepResultBase, ICaptureStepResult
{
    public Bitmap? Image { get; init; }
    public Rectangle Bounds { get; init; }
    public Point Offset { get; init; }
    public bool IsFresh { get; init; } = true;
    public DateTime CaptureTimestampUtc { get; init; } = DateTime.UtcNow;
    [ResultProperty("has_image")]
    public bool HasImage => Image is not null;
    public static readonly ProcessDuplicationResult Default = new();
}

public sealed record TemplateMatchingResult : StepResultBase, IDetectionStepResult
{
    public bool Found { get; init; } public Point? Point { get; init; } public Rectangle? BoundingBox { get; init; }
    public double Confidence { get; init; } public bool SourceCaptureIsFresh { get; init; } = true;
    public DateTime SourceCaptureTimestampUtc { get; init; } = DateTime.UtcNow;
    [ResultProperty("applied_roi")] public string? AppliedRoi { get; init; }
    [ResultProperty("used_dynamic_roi")] public bool UsedDynamicRoi { get; init; }
    public IReadOnlyList<DetectionItem> AllDetections { get; init; } = Array.Empty<DetectionItem>();
    public static readonly TemplateMatchingResult Default = new();
}

public sealed record ColorDetectionResult : StepResultBase, IDetectionStepResult
{
    public bool Found { get; init; } public Point? Point { get; init; } public Rectangle? BoundingBox { get; init; }
    public double Confidence { get; init; } public bool SourceCaptureIsFresh { get; init; } = true;
    public DateTime SourceCaptureTimestampUtc { get; init; } = DateTime.UtcNow;
    [ResultProperty("applied_roi")] public string? AppliedRoi { get; init; }
    [ResultProperty("used_dynamic_roi")] public bool UsedDynamicRoi { get; init; }
    public IReadOnlyList<DetectionItem> AllDetections { get; init; } = Array.Empty<DetectionItem>();
    public static readonly ColorDetectionResult Default = new();
}

public sealed record YOLODetectionResult : StepResultBase, IDetectionStepResult
{
    public bool Found { get; init; } public Point? Point { get; init; } public Rectangle? BoundingBox { get; init; }
    public double Confidence { get; init; } public bool SourceCaptureIsFresh { get; init; } = true;
    public DateTime SourceCaptureTimestampUtc { get; init; } = DateTime.UtcNow;
    [ResultProperty("applied_roi")] public string? AppliedRoi { get; init; }
    [ResultProperty("used_dynamic_roi")] public bool UsedDynamicRoi { get; init; }
    public IReadOnlyList<DetectionItem> AllDetections { get; init; } = Array.Empty<DetectionItem>();
    public static readonly YOLODetectionResult Default = new();
}

public sealed record KeyPointMatchingResult : StepResultBase, IDetectionStepResult
{
    public bool Found { get; init; } public Point? Point { get; init; } public Rectangle? BoundingBox { get; init; }
    public double Confidence { get; init; } public bool SourceCaptureIsFresh { get; init; } = true;
    public DateTime SourceCaptureTimestampUtc { get; init; } = DateTime.UtcNow;
    [ResultProperty("applied_roi")] public string? AppliedRoi { get; init; }
    [ResultProperty("used_dynamic_roi")] public bool UsedDynamicRoi { get; init; }
    public IReadOnlyList<DetectionItem> AllDetections { get; init; } = Array.Empty<DetectionItem>();
    public static readonly KeyPointMatchingResult Default = new();
}

public sealed record PredictMovementResult : StepResultBase, IDetectionStepResult
{
    public bool Found { get; init; } public Point? Point { get; init; } public Rectangle? BoundingBox { get; init; }
    public double Confidence { get; init; } public bool SourceCaptureIsFresh { get; init; } = true;
    public DateTime SourceCaptureTimestampUtc { get; init; } = DateTime.UtcNow;
    [ResultProperty("is_predicted")]
    public bool IsPredicted { get; init; }
    [ResultProperty("predicted_for_utc")]
    public DateTime? PredictedForUtc { get; init; }
    public IReadOnlyList<DetectionItem> AllDetections { get; init; } = Array.Empty<DetectionItem>();
    public static readonly PredictMovementResult Default = new();
}

public sealed record KlickOnPointResult : StepResultBase, IActionExecutionResult { public bool Success { get; init; } public string? ErrorMessage { get; init; } public static readonly KlickOnPointResult Default = new(); }
public sealed record KlickOnPoint3DResult : StepResultBase, IActionExecutionResult { public bool Success { get; init; } public string? ErrorMessage { get; init; } public static readonly KlickOnPoint3DResult Default = new(); }
public sealed record MakroExecutionResult : StepResultBase, IActionExecutionResult { public bool Success { get; init; } public string? ErrorMessage { get; init; } public static readonly MakroExecutionResult Default = new(); }
public sealed record ScriptExecutionResult : StepResultBase, IActionExecutionResult { public bool Success { get; init; } public string? ErrorMessage { get; init; } public static readonly ScriptExecutionResult Default = new(); }
public sealed record JobExecutionResult : StepResultBase, IActionExecutionResult { public bool Success { get; init; } public string? ErrorMessage { get; init; } public static readonly JobExecutionResult Default = new(); }
public sealed record TimeoutResult : StepResultBase, IActionExecutionResult { public bool Success { get; init; } public string? ErrorMessage { get; init; } public static readonly TimeoutResult Default = new(); }
public sealed record InputControlResult : StepResultBase, IActionExecutionResult { public bool Success { get; init; } public string? ErrorMessage { get; init; } public static readonly InputControlResult Default = new(); }
public sealed record FocusProcessResult : StepResultBase, IActionExecutionResult, IProcessReferenceResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public RuntimeProcessReference? Process { get; init; }
    public static readonly FocusProcessResult Default = new();
}
public sealed record ShowTextResult : StepResultBase, IActionExecutionResult { public bool Success { get; init; } public string? ErrorMessage { get; init; } public static readonly ShowTextResult Default = new(); }
public sealed record ShowImageResult : StepResultBase, IActionExecutionResult { public bool Success { get; init; } public string? ErrorMessage { get; init; } public static readonly ShowImageResult Default = new(); }
public sealed record ShowOnDesktopResult : StepResultBase, IActionExecutionResult { public bool Success { get; init; } public string? ErrorMessage { get; init; } public static readonly ShowOnDesktopResult Default = new(); }
public sealed record VideoCreationResult : StepResultBase, IActionExecutionResult { public bool Success { get; init; } public string? ErrorMessage { get; init; } public static readonly VideoCreationResult Default = new(); }

public sealed record StartProcessResult : StepResultBase, IActionExecutionResult, IProcessReferenceResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public RuntimeProcessReference? Process { get; init; }
    public static readonly StartProcessResult Default = new();
}

public sealed record TerminateProcessResult : StepResultBase, IActionExecutionResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public static readonly TerminateProcessResult Default = new();
}

public sealed record DynamicRoiResult : StepResultBase
{
    [ResultProperty("roi_updated")]
    public bool RoiUpdated { get; init; }
    [ResultProperty("roi_reset")]
    public bool RoiReset { get; init; }
    [ResultProperty("global_bounds")]
    public Rectangle? GlobalBounds { get; init; }
    [ResultProperty("consecutive_misses")]
    public int ConsecutiveMisses { get; init; }
    [ResultProperty("full_search_interval")]
    public int FullSearchInterval { get; init; }
    public static readonly DynamicRoiResult Default = new();
}

public sealed record GetProcessResult : StepResultBase, IProcessReferenceResult
{
    [ResultProperty("found")]
    public bool Found { get; init; }
    public RuntimeProcessReference? Process { get; init; }
    public static readonly GetProcessResult Default = new();
}

public sealed record ActiveProcessResult : StepResultBase, IProcessReferenceResult
{
    [ResultProperty("is_running")]
    public bool IsRunning { get; init; }
    [ResultProperty("match_count")]
    public int MatchCount { get; init; }
    public RuntimeProcessReference? Process { get; init; }
    public static readonly ActiveProcessResult Default = new();
}

public sealed record ActiveWindowResult : StepResultBase, IProcessReferenceResult
{
    [ResultProperty("is_active")]
    public bool IsActive { get; init; }
    public RuntimeProcessReference? Process { get; init; }
    [ResultProperty("window_handle")]
    public long WindowHandle { get; init; }
    public static readonly ActiveWindowResult Default = new();
}

public sealed record PointComparisonResult : StepResultBase
{
    [ResultProperty("matches")]
    public bool Matches { get; init; }
    [ResultProperty("match_count")]
    public int MatchCount { get; init; }
    [ResultProperty("total_count")]
    public int TotalCount { get; init; }
    public static readonly PointComparisonResult Default = new();
}
