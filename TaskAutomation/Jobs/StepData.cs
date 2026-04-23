using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using OpenCvSharp;

namespace TaskAutomation.Jobs
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(TemplateMatchingStep), "template_matching")]
    [JsonDerivedType(typeof(ProcessDuplicationStep), "process_duplication")]
    [JsonDerivedType(typeof(DesktopDuplicationStep), "desktop_duplication")]
    [JsonDerivedType(typeof(ShowImageStep), "show_image")]
    [JsonDerivedType(typeof(VideoCreationStep), "video_creation")]
    [JsonDerivedType(typeof(MakroExecutionStep), "makro_execution")]
    [JsonDerivedType(typeof(ScriptExecutionStep), "script_execution")]
    [JsonDerivedType(typeof(KlickOnPointStep), "klick_on_point")]
    [JsonDerivedType(typeof(KlickOnPoint3DStep), "klick_on_point_3d")]
    [JsonDerivedType(typeof(JobExecutionStep), "job_execution")]
    [JsonDerivedType(typeof(YOLODetectionStep), "yolo_detection")]
    [JsonDerivedType(typeof(TimeoutStep), "timeout")]
    [JsonDerivedType(typeof(IfStep),       "if")]
    [JsonDerivedType(typeof(ElseIfStep),   "else_if")]
    [JsonDerivedType(typeof(ElseStep),     "else")]
    [JsonDerivedType(typeof(EndIfStep),    "end_if")]
    public abstract class JobStep
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();
    }

    // ---- TemplateMatching ----
    public sealed class TemplateMatchingStep : JobStep
    {
        [JsonPropertyName("settings")]
        public TemplateMatchingSettings Settings { get; set; } = new();
    }

    public sealed class TemplateMatchingSettings
    {
        [JsonPropertyName("template_path")]
        public string TemplatePath { get; set; } = string.Empty;

        [JsonPropertyName("template_match_mode")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public TemplateMatchModes TemplateMatchMode { get; set; } = TemplateMatchModes.CCoeffNormed;

        [JsonPropertyName("multiple_points")]
        public bool MultiplePoints { get; set; } = false;

        [JsonPropertyName("confidence_threshold")]
        public double ConfidenceThreshold { get; set; } = 0.90;

        [JsonPropertyName("roi")]
        [JsonConverter(typeof(OpenCvRectJsonConverter))]
        public Rect ROI { get; set; } = new Rect(0, 0, 0, 0);

        [JsonPropertyName("enable_roi")]
        public bool EnableROI { get; set; } = false;

        [JsonPropertyName("draw_results")]
        public bool DrawResults { get; set; } = true;

        [JsonPropertyName("source_capture_step_id")]
        public string SourceCaptureStepId { get; set; } = "";
    }

    // ---- DesktopDuplication ----
    public sealed class DesktopDuplicationStep : JobStep
    {
        [JsonPropertyName("settings")]
        public DesktopDuplicationSettings Settings { get; set; } = new();
    }

    public sealed class DesktopDuplicationSettings
    {
        [JsonPropertyName("desktop_idx")]
        public int DesktopIdx { get; set; } = 0;
    }

    // ---- ProcessDuplication ----
    public sealed class ProcessDuplicationStep : JobStep
    {
        [JsonPropertyName("settings")]
        public ProcessDuplicationSettings Settings { get; set; } = new();
    }

    public sealed class ProcessDuplicationSettings
    {
        [JsonPropertyName("process_name")]
        public string ProcessName { get; set; } = string.Empty;
    }

    // ---- ShowImage ----
    public sealed class ShowImageStep : JobStep
    {
        [JsonPropertyName("settings")]
        public ShowImageSettings Settings { get; set; } = new();
    }

    public sealed class ShowImageSettings
    {
        [JsonPropertyName("window_name")]
        public string WindowName { get; set; } = "MyWindow";

        [JsonPropertyName("show_raw_image")]
        public bool ShowRawImage { get; set; } = true;

        [JsonPropertyName("show_processed_image")]
        public bool ShowProcessedImage { get; set; } = true;

        [JsonPropertyName("source_capture_step_id")]
        public string SourceCaptureStepId { get; set; } = "";

        [JsonPropertyName("source_detection_step_id")]
        public string SourceDetectionStepId { get; set; } = "";
    }

    // ---- VideoCreation ----
    public sealed class VideoCreationStep : JobStep
    {
        [JsonPropertyName("settings")]
        public VideoCreationSettings Settings { get; set; } = new();
    }

    public sealed class VideoCreationSettings
    {
        [JsonPropertyName("save_path")]
        public string SavePath { get; set; } = string.Empty;

        [JsonPropertyName("file_name")]
        public string FileName { get; set; } = "output.mp4";

        [JsonPropertyName("use_raw_image")]
        public bool UseRawImage { get; set; } = true;

        [JsonPropertyName("use_processed_image")]
        public bool UseProcessedImage { get; set; } = true;

        [JsonPropertyName("source_capture_step_id")]
        public string SourceCaptureStepId { get; set; } = "";

        [JsonPropertyName("source_detection_step_id")]
        public string SourceDetectionStepId { get; set; } = "";
    }

    // ---- MakroExecution ----
    public sealed class MakroExecutionStep : JobStep
    {
        [JsonPropertyName("settings")]
        public MakroExecutionSettings Settings { get; set; } = new();
    }

    public sealed class MakroExecutionSettings
    {
        [JsonPropertyName("makro_name")]
        public string MakroName { get; set; } = string.Empty;

        [JsonPropertyName("makro_id")]
        public Guid? MakroId { get; set; }
    }

    public sealed class ScriptExecutionStep : JobStep
    {
        [JsonPropertyName("settings")]
        public ScriptExecutionSettings Settings { get; set; } = new();
    }

    public sealed class ScriptExecutionSettings
    {
        [JsonPropertyName("script_path")]
        public string ScriptPath { get; set; } = string.Empty;
        [JsonPropertyName("fire_and_forget")]
        public bool FireAndForget { get; set; } = false;
    }

    public sealed class KlickOnPointStep : JobStep
    {
        [JsonPropertyName("settings")]
        public KlickOnPointSettings Settings { get; set; } = new();
    }

    public sealed class KlickOnPointSettings
    {
        [JsonPropertyName("double_click")]
        public bool DoubleClick { get; set; } = false;
        [JsonPropertyName("click_type")]
        public string ClickType { get; set; } = "left";
        [JsonPropertyName("timeout_ms")]
        public int TimeoutMs { get; set; } = 1000;

        [JsonPropertyName("source_detection_step_id")]
        public string SourceDetectionStepId { get; set; } = "";
    }

    public sealed class KlickOnPoint3DStep : JobStep
    {
        [JsonPropertyName("settings")]
        public KlickOnPoint3DSettings Settings { get; set; } = new();
    }

    public sealed class KlickOnPoint3DSettings
    {
        [JsonPropertyName("fov")]
        public float FOV { get; set; } = 90.0f;
        [JsonPropertyName("maus_sensitivity_x")]
        public float MausSensitivityX { get; set; } = 1.0f;
        [JsonPropertyName("maus_sensitivity_y")]
        public float MausSensitivityY { get; set; } = 1.0f;
        [JsonPropertyName("double_click")]
        public bool DoubleClick { get; set; } = false;
        [JsonPropertyName("click_type")]
        public string ClickType { get; set; } = "left";
        [JsonPropertyName("timeout_ms")]
        public int TimeoutMs { get; set; } = 1000;
        [JsonPropertyName("Invert MouseMovement Y")]
        public bool InvertMouseMovementY { get; set; } = false;
        [JsonPropertyName("Invert MouseMovement X")]
        public bool InvertMouseMovementX { get; set; } = false;

        [JsonPropertyName("source_detection_step_id")]
        public string SourceDetectionStepId { get; set; } = "";

        [JsonPropertyName("source_capture_step_id")]
        public string SourceCaptureStepId { get; set; } = "";
    }

    public sealed class JobExecutionStep : JobStep
    {
        [JsonPropertyName("settings")]
        public JobExecutionStepSettings Settings { get; set; } = new();
    }

    // ---- Timeout ----
    public sealed class TimeoutStep : JobStep
    {
        [JsonPropertyName("settings")]
        public TimeoutSettings Settings { get; set; } = new();
    }

    public sealed class TimeoutSettings
    {
        [JsonPropertyName("delay_ms")]
        public int DelayMs { get; set; } = 1000;
    }

    public sealed class JobExecutionStepSettings
    {
        [JsonPropertyName("job_name")]
        public string JobName { get; set; } = string.Empty;

        [JsonPropertyName("job_id")]
        public Guid? JobId { get; set; }

        [JsonPropertyName("wait_for_completion")]
        public bool WaitForCompletion { get; set; } = true;
    }

    public sealed class YOLODetectionStep : JobStep
    {
        [JsonPropertyName("settings")]
        public YOLODetectionStepSettings Settings { get; set; } = new();
    }

    public sealed class YOLODetectionStepSettings
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;
        [JsonPropertyName("confidence_threshold")]
        public float ConfidenceThreshold { get; set; } = 0.5f;
        [JsonPropertyName("class_name")]
        public string ClassName { get; set; } = string.Empty;
        [JsonPropertyName("roi")]
        [JsonConverter(typeof(OpenCvRectJsonConverter))]
        public Rect ROI { get; set; } = new Rect(0, 0, 0, 0);

        [JsonPropertyName("enable_roi")]
        public bool EnableROI { get; set; } = false;

        [JsonPropertyName("draw_results")]
        public bool DrawResults { get; set; } = true;

        [JsonPropertyName("source_capture_step_id")]
        public string SourceCaptureStepId { get; set; } = "";
    }

    // ---- If / ElseIf / Else / EndIf ----

    public enum ConditionOperator
    {
        IsTrue,
        IsFalse,
        Equals,
        NotEquals,
        GreaterThan,
        LessThan,
        GreaterThanOrEqual,
        LessThanOrEqual,
    }

    public enum ConditionMatchMode { All, Any }

    public sealed class StepCondition : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        [JsonPropertyName("source_step_id")]
        public string SourceStepId { get; set; } = "";

        private string _sourceStepDisplayName = "";
        [JsonIgnore]
        public string SourceStepDisplayName
        {
            get => _sourceStepDisplayName;
            set { if (_sourceStepDisplayName != value) { _sourceStepDisplayName = value; OnPropertyChanged(); } }
        }

        [JsonPropertyName("property")]
        public string Property { get; set; } = "";

        [JsonPropertyName("property_display_name")]
        public string PropertyDisplayName { get; set; } = "";

        [JsonPropertyName("operator")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ConditionOperator Operator { get; set; } = ConditionOperator.IsTrue;

        [JsonPropertyName("comparison_value")]
        public string? ComparisonValue { get; set; }
    }

    public sealed class IfConditionSettings
    {
        [JsonPropertyName("match_mode")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ConditionMatchMode MatchMode { get; set; } = ConditionMatchMode.All;

        [JsonPropertyName("conditions")]
        public List<StepCondition> Conditions { get; set; } = new();
    }

    public sealed class IfStep : JobStep
    {
        [JsonPropertyName("settings")]
        public IfConditionSettings Settings { get; set; } = new();
    }

    public sealed class ElseIfStep : JobStep
    {
        [JsonPropertyName("settings")]
        public IfConditionSettings Settings { get; set; } = new();
    }

    /// <summary>Marks the start of the else block. No configuration needed.</summary>
    public sealed class ElseStep : JobStep { }

    /// <summary>Marks the end of an if/elseif/else block. No configuration needed.</summary>
    public sealed class EndIfStep : JobStep { }
}
