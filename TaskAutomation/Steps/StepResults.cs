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

public sealed record WindowsStateQueryResult : StepResultBase
{
    public WindowsCapabilityStatus Status { get; init; } = WindowsCapabilityStatus.Success;
    public bool IsAvailable { get; init; }
    public DateTime CapturedAt { get; init; } = DateTime.UtcNow;
    public string ErrorCode { get; init; } = string.Empty;
    public string ErrorMessage { get; init; } = string.Empty;
    public bool Exists { get; init; }
    public bool IsActive { get; init; }
    public bool IsConnected { get; init; }
    public bool IsEnabled { get; init; }
    public bool IsMuted { get; init; }
    public bool IsCharging { get; init; }
    public bool PendingRestart { get; init; }
    public long Count { get; init; }
    public long Value { get; init; }
    public double Percentage { get; init; }
    public double FreeSpaceGb { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Id { get; init; } = string.Empty;
    public string Text { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public WindowsConnectivity Connectivity { get; init; } = WindowsConnectivity.Unknown;
    public WindowsConnectionType ConnectionType { get; init; } = WindowsConnectionType.Unknown;
    public WindowsPowerSource PowerSource { get; init; } = WindowsPowerSource.Unknown;
    public WindowsSessionState SessionState { get; init; } = WindowsSessionState.Unknown;
    public WindowsDeviceState DeviceState { get; init; } = WindowsDeviceState.Unknown;
    public WindowsOnOffState OnOffState { get; init; } = WindowsOnOffState.Unknown;
    public IReadOnlyList<string> Items { get; init; } = [];
    public static readonly WindowsStateQueryResult Default = new();
}

public sealed record DetectionItem
{
    public Point Center { get; init; }
    public Rectangle? BoundingBox { get; init; }
    public double Confidence { get; init; }
}

/// <summary>Stable identity for one process instance. Start time prevents PID reuse.</summary>
public sealed record RuntimeProcessReference
{
    public int ProcessId { get; init; }
    public DateTime StartTimeUtc { get; init; }
    public string ProcessName { get; init; } = string.Empty;
    public string ExecutablePath { get; init; } = string.Empty;
    public long WindowHandle { get; init; }
}

/// <summary>Internal capture data contract used for coordinate conversion.</summary>
public interface ICaptureStepResult
{
    Bitmap? Image { get; }
    Rectangle Bounds { get; }
    Point Offset { get; }
    bool IsFresh { get; }
    DateTime CaptureTimestampUtc { get; }
}

/// <summary>Internal detection data contract. The concrete result still belongs to one step type.</summary>
public interface IDetectionStepResult
{
    bool Found { get; }
    Point? Point { get; }
    Rectangle? BoundingBox { get; }
    double Confidence { get; }
    bool SourceCaptureIsFresh { get; }
    DateTime SourceCaptureTimestampUtc { get; }
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
    RuntimeProcessReference? Process { get; }
}

public sealed record DesktopDuplicationResult : StepResultBase, ICaptureStepResult
{
    public Bitmap? Image { get; init; }
    public Rectangle Bounds { get; init; }
    public Point Offset { get; init; }
    public bool IsFresh { get; init; } = true;
    public DateTime CaptureTimestampUtc { get; init; } = DateTime.UtcNow;
    public bool HasImage => Image is not null;
    public static readonly DesktopDuplicationResult Default = new();
}

public sealed record ProcessDuplicationResult : StepResultBase, ICaptureStepResult
{
    public Bitmap? Image { get; init; }
    public Rectangle Bounds { get; init; }
    public Point Offset { get; init; }
    public bool IsFresh { get; init; } = true;
    public DateTime CaptureTimestampUtc { get; init; } = DateTime.UtcNow;
    public bool HasImage => Image is not null;
    public static readonly ProcessDuplicationResult Default = new();
}

public sealed record TemplateMatchingResult : StepResultBase, IDetectionStepResult
{
    public bool Found { get; init; } public Point? Point { get; init; } public Rectangle? BoundingBox { get; init; }
    public double Confidence { get; init; } public bool SourceCaptureIsFresh { get; init; } = true;
    public DateTime SourceCaptureTimestampUtc { get; init; } = DateTime.UtcNow;
    public string? AppliedRoi { get; init; } public bool UsedDynamicRoi { get; init; }
    public IReadOnlyList<DetectionItem> AllDetections { get; init; } = Array.Empty<DetectionItem>();
    public static readonly TemplateMatchingResult Default = new();
}

public sealed record ColorDetectionResult : StepResultBase, IDetectionStepResult
{
    public bool Found { get; init; } public Point? Point { get; init; } public Rectangle? BoundingBox { get; init; }
    public double Confidence { get; init; } public bool SourceCaptureIsFresh { get; init; } = true;
    public DateTime SourceCaptureTimestampUtc { get; init; } = DateTime.UtcNow;
    public string? AppliedRoi { get; init; } public bool UsedDynamicRoi { get; init; }
    public IReadOnlyList<DetectionItem> AllDetections { get; init; } = Array.Empty<DetectionItem>();
    public static readonly ColorDetectionResult Default = new();
}

public sealed record YOLODetectionResult : StepResultBase, IDetectionStepResult
{
    public bool Found { get; init; } public Point? Point { get; init; } public Rectangle? BoundingBox { get; init; }
    public double Confidence { get; init; } public bool SourceCaptureIsFresh { get; init; } = true;
    public DateTime SourceCaptureTimestampUtc { get; init; } = DateTime.UtcNow;
    public string? AppliedRoi { get; init; } public bool UsedDynamicRoi { get; init; }
    public IReadOnlyList<DetectionItem> AllDetections { get; init; } = Array.Empty<DetectionItem>();
    public static readonly YOLODetectionResult Default = new();
}

public sealed record KeyPointMatchingResult : StepResultBase, IDetectionStepResult
{
    public bool Found { get; init; } public Point? Point { get; init; } public Rectangle? BoundingBox { get; init; }
    public double Confidence { get; init; } public bool SourceCaptureIsFresh { get; init; } = true;
    public DateTime SourceCaptureTimestampUtc { get; init; } = DateTime.UtcNow;
    public string? AppliedRoi { get; init; } public bool UsedDynamicRoi { get; init; }
    public IReadOnlyList<DetectionItem> AllDetections { get; init; } = Array.Empty<DetectionItem>();
    public static readonly KeyPointMatchingResult Default = new();
}

public sealed record PredictMovementResult : StepResultBase, IDetectionStepResult
{
    public bool Found { get; init; } public Point? Point { get; init; } public Rectangle? BoundingBox { get; init; }
    public double Confidence { get; init; } public bool SourceCaptureIsFresh { get; init; } = true;
    public DateTime SourceCaptureTimestampUtc { get; init; } = DateTime.UtcNow;
    public bool IsPredicted { get; init; }
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
    public bool RoiUpdated { get; init; }
    public bool RoiReset { get; init; }
    public Rectangle? GlobalBounds { get; init; }
    public int ConsecutiveMisses { get; init; }
    public int FullSearchInterval { get; init; }
    public static readonly DynamicRoiResult Default = new();
}

public sealed record GetProcessResult : StepResultBase, IProcessReferenceResult
{
    public bool Found { get; init; }
    public RuntimeProcessReference? Process { get; init; }
    public static readonly GetProcessResult Default = new();
}

public sealed record ActiveProcessResult : StepResultBase, IProcessReferenceResult
{
    public bool IsRunning { get; init; }
    public int MatchCount { get; init; }
    public RuntimeProcessReference? Process { get; init; }
    public static readonly ActiveProcessResult Default = new();
}

public sealed record ActiveWindowResult : StepResultBase, IProcessReferenceResult
{
    public bool IsActive { get; init; }
    public RuntimeProcessReference? Process { get; init; }
    public long WindowHandle { get; init; }
    public static readonly ActiveWindowResult Default = new();
}

public sealed record PointComparisonResult : StepResultBase
{
    public bool Matches { get; init; }
    public int MatchCount { get; init; }
    public int TotalCount { get; init; }
    public static readonly PointComparisonResult Default = new();
}
