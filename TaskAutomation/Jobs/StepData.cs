using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using OpenCvSharp;

namespace TaskAutomation.Jobs
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(TemplateMatchingStep), "template_matching")]
    [JsonDerivedType(typeof(ColorDetectionStep), "color_detection")]
    [JsonDerivedType(typeof(PredictMovementStep), "predict_movement")]
    [JsonDerivedType(typeof(ProcessDuplicationStep), "process_duplication")]
    [JsonDerivedType(typeof(DesktopDuplicationStep), "desktop_duplication")]
    [JsonDerivedType(typeof(ShowImageStep), "show_image")]
    [JsonDerivedType(typeof(ShowOnDesktopStep), "show_on_desktop")]
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
    [JsonDerivedType(typeof(EndJobStep),   "end_job")]
    [JsonDerivedType(typeof(ActiveProcessStep), "active_process")]
    [JsonDerivedType(typeof(StartProcessStep),  "start_process")]
    [JsonDerivedType(typeof(FocusProcessStep),   "focus_process")]
    [JsonDerivedType(typeof(ShowTextStep),         "show_text")]
    [JsonDerivedType(typeof(ActiveWindowStep),     "active_window")]
    [JsonDerivedType(typeof(KeyPointMatchingStep), "keypoint_matching")]
    [JsonDerivedType(typeof(PointComparisonStep),  "point_comparison")]
    [JsonDerivedType(typeof(DynamicRoiStep), "dynamic_roi")]
    public abstract class JobStep : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        private bool _isEnabled = true;

        /// <summary>Gibt an, ob dieser Step beim Ausführen des Jobs berücksichtigt wird.</summary>
        [JsonPropertyName("is_enabled")]
        public bool IsEnabled
        {
            get => _isEnabled;
            set { if (_isEnabled != value) { _isEnabled = value; OnPropertyChanged(); } }
        }

        /// <summary>Gibt an, ob der Benutzer diesen Step deaktivieren darf.
        /// Bei Flow-Control-Steps (If/Else/EndIf) ist das nicht erlaubt.</summary>
        [JsonIgnore]
        public virtual bool CanBeDisabled => true;

        private bool _isValid = true;
        [JsonIgnore]
        public bool IsValid
        {
            get => _isValid;
            internal set { if (_isValid != value) { _isValid = value; OnPropertyChanged(); } }
        }

        private string? _validationError;
        [JsonIgnore]
        public string? ValidationError
        {
            get => _validationError;
            internal set { if (_validationError != value) { _validationError = value; OnPropertyChanged(); } }
        }

        public void SetValidationResult(bool isValid, string? error)
        {
            ValidationError = error;
            IsValid = isValid;
        }
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

        [JsonPropertyName("source_capture_step_id")]
        public string SourceCaptureStepId { get; set; } = "";

        [JsonPropertyName("dynamic_roi_step_id")]
        public string DynamicRoiStepId { get; set; } = "";
    }

    // ---- ColorDetection ----
    public sealed class ColorDetectionStep : JobStep
    {
        [JsonPropertyName("settings")]
        public ColorDetectionSettings Settings { get; set; } = new();
    }

    public sealed class ColorDetectionSettings
    {
        [JsonPropertyName("color_hex")]
        public string ColorHex { get; set; } = "#FF0000";

        [JsonPropertyName("confidence_threshold")]
        public double ConfidenceThreshold { get; set; } = 0.90;

        [JsonPropertyName("min_size")]
        public int MinSize { get; set; } = 25;

        [JsonPropertyName("max_size")]
        public int MaxSize { get; set; } = int.MaxValue;

        [JsonPropertyName("min_width")]
        public int MinWidth { get; set; } = 1;

        [JsonPropertyName("min_height")]
        public int MinHeight { get; set; } = 1;

        [JsonPropertyName("downscale_factor")]
        public int DownscaleFactor { get; set; } = 1;

        [JsonPropertyName("roi")]
        [JsonConverter(typeof(OpenCvRectJsonConverter))]
        public Rect ROI { get; set; } = new Rect(0, 0, 0, 0);

        [JsonPropertyName("enable_roi")]
        public bool EnableROI { get; set; } = false;

        [JsonPropertyName("source_capture_step_id")]
        public string SourceCaptureStepId { get; set; } = "";

        [JsonPropertyName("dynamic_roi_step_id")]
        public string DynamicRoiStepId { get; set; } = "";
    }

    // ---- PredictMovement ----
    public sealed class PredictMovementStep : JobStep
    {
        [JsonPropertyName("settings")]
        public PredictMovementSettings Settings { get; set; } = new();
    }

    public sealed class PredictMovementSettings
    {
        [JsonPropertyName("source_detection_step_id")]
        public string SourceDetectionStepId { get; set; } = "";

        [JsonPropertyName("min_samples")]
        public int MinSamples { get; set; } = 3;

        [JsonPropertyName("prediction_ms")]
        public int PredictionMs { get; set; } = 100;

        [JsonPropertyName("reset_distance_threshold")]
        public double ResetDistanceThreshold { get; set; } = 250;

        [JsonPropertyName("max_sample_age_ms")]
        public int MaxSampleAgeMs { get; set; } = 500;

        [JsonPropertyName("prediction_model")]
        public string PredictionModel { get; set; } = "Linear";

        [JsonPropertyName("time_basis")]
        public string TimeBasis { get; set; } = "Capture";

        [JsonPropertyName("max_prediction_distance")]
        public double MaxPredictionDistance { get; set; } = 0;

        [JsonPropertyName("max_fit_error")]
        public double MaxFitError { get; set; } = 0;

        [JsonPropertyName("minimum_confidence")]
        public double MinimumConfidence { get; set; } = 0;
    }

    public sealed class DynamicRoiStep : JobStep
    {
        [JsonPropertyName("settings")]
        public DynamicRoiSettings Settings { get; set; } = new();
    }

    public sealed class DynamicRoiSettings
    {
        [JsonPropertyName("source_detection_step_id")]
        public string SourceDetectionStepId { get; set; } = string.Empty;

        [JsonPropertyName("padding")]
        public int Padding { get; set; } = 25;

        [JsonPropertyName("minimum_confidence")]
        public double MinimumConfidence { get; set; }

        [JsonPropertyName("full_search_interval")]
        public int FullSearchInterval { get; set; } = 10;

        [JsonPropertyName("reset_after_misses")]
        public int ResetAfterMisses { get; set; } = 3;
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

        [JsonPropertyName("capture_cursor")]
        public bool CaptureCursor { get; set; } = false;
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

    // ---- ShowOnDesktop ----
    public sealed class ShowOnDesktopStep : JobStep
    {
        [JsonPropertyName("settings")]
        public ShowOnDesktopSettings Settings { get; set; } = new();
    }

    public sealed class ShowOnDesktopSettings
    {
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
        [JsonPropertyName("arguments")]
        public string Arguments { get; set; } = string.Empty;
        [JsonPropertyName("wait_for_exit")]
        public bool WaitForExit { get; set; } = false;
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
        [JsonPropertyName("offset_x")]
        public int OffsetX { get; set; } = 0;
        [JsonPropertyName("offset_y")]
        public int OffsetY { get; set; } = 0;

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
        [JsonPropertyName("double_click")]
        public bool DoubleClick { get; set; } = false;
        [JsonPropertyName("click_type")]
        public string ClickType { get; set; } = "left";
        [JsonPropertyName("timeout_ms")]
        public int TimeoutMs { get; set; } = 1000;
        [JsonPropertyName("origin_x")]
        public int OriginX { get; set; } = 0;
        [JsonPropertyName("origin_y")]
        public int OriginY { get; set; } = 0;
        [JsonPropertyName("offset_x")]
        public int OffsetX { get; set; } = 0;
        [JsonPropertyName("offset_y")]
        public int OffsetY { get; set; } = 0;
        [JsonPropertyName("source_detection_step_id")]
        public string SourceDetectionStepId { get; set; } = "";
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

        [JsonPropertyName("source_capture_step_id")]
        public string SourceCaptureStepId { get; set; } = "";

        [JsonPropertyName("dynamic_roi_step_id")]
        public string DynamicRoiStepId { get; set; } = "";
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
        Contains,
        StartsWith,
        IsEmpty,
        IsNotEmpty,
    }

    public enum ConditionMatchMode { All, Any }

    public enum ComparisonOperandKind { Literal, JobResult }

    public sealed class ComparisonOperand
    {
        [JsonPropertyName("kind")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ComparisonOperandKind Kind { get; set; } = ComparisonOperandKind.Literal;

        [JsonPropertyName("value")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Value { get; set; }

        [JsonPropertyName("source_step_id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? SourceStepId { get; set; }

        [JsonPropertyName("property_path")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? PropertyPath { get; set; }
    }

    public sealed class StepCondition
    {
        [JsonPropertyName("source_step_id")]
        public string SourceStepId { get; set; } = "";

        [JsonPropertyName("property_path")]
        public string PropertyPath { get; set; } = "";

        [JsonPropertyName("operator")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ConditionOperator Operator { get; set; } = ConditionOperator.IsTrue;

        /// <summary>Legacy literal value. Kept so existing job JSON remains readable.</summary>
        [JsonPropertyName("comparison_value")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ComparisonValue { get; set; }

        [JsonPropertyName("comparison")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ComparisonOperand? Comparison { get; set; }

        [JsonIgnore]
        public ComparisonOperand EffectiveComparison => Comparison ?? new ComparisonOperand
        {
            Kind = ComparisonOperandKind.Literal,
            Value = ComparisonValue
        };
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
        public override bool CanBeDisabled => false;
    }

    public sealed class ElseIfStep : JobStep
    {
        [JsonPropertyName("settings")]
        public IfConditionSettings Settings { get; set; } = new();
        public override bool CanBeDisabled => false;
    }

    /// <summary>Marks the start of the else block. No configuration needed.</summary>
    public sealed class ElseStep : JobStep
    {
        public override bool CanBeDisabled => false;
    }

    /// <summary>Marks the end of an if/elseif/else block. No configuration needed.</summary>
    public sealed class EndIfStep : JobStep
    {
        public override bool CanBeDisabled => false;
    }

    /// <summary>Immediately ends the current job when executed.</summary>
    public sealed class EndJobStep : JobStep { }

    // ---- ActiveProcess ----
    /// <summary>Prüft, ob ein Prozess mit dem angegebenen Namen aktuell läuft.</summary>
    public sealed class ActiveProcessStep : JobStep
    {
        [JsonPropertyName("settings")]
        public ActiveProcessSettings Settings { get; set; } = new();
    }

    public sealed class ActiveProcessSettings
    {
        [JsonPropertyName("process_name")]
        public string ProcessName { get; set; } = string.Empty;
    }

    // ---- StartProcess ----
    /// <summary>Startet einen Prozess / ein Programm.</summary>
    public sealed class StartProcessStep : JobStep
    {
        [JsonPropertyName("settings")]
        public StartProcessSettings Settings { get; set; } = new();
    }

    public enum StartProcessPlacementMode
    {
        Centered,
        Custom
    }

    public enum StartProcessWindowMode
    {
        ApplicationDefault,
        Normal,
        Maximized
    }

    public enum StartProcessAction
    {
        Start,
        Terminate
    }

    public sealed class StartProcessSettings
    {
        [JsonPropertyName("action")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public StartProcessAction Action { get; set; } = StartProcessAction.Start;

        [JsonPropertyName("executable_path")]
        public string ExecutablePath { get; set; } = string.Empty;

        [JsonPropertyName("process_name")]
        public string ProcessName { get; set; } = string.Empty;

        [JsonPropertyName("window_title_contains")]
        public string WindowTitleContains { get; set; } = string.Empty;

        [JsonPropertyName("arguments")]
        public string Arguments { get; set; } = string.Empty;

        [JsonPropertyName("wait_for_exit")]
        public bool WaitForExit { get; set; } = false;

        [JsonPropertyName("monitor_index")]
        public int MonitorIndex { get; set; }

        [JsonPropertyName("placement_mode")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public StartProcessPlacementMode PlacementMode { get; set; } = StartProcessPlacementMode.Centered;

        [JsonPropertyName("offset_x")]
        public int OffsetX { get; set; }

        [JsonPropertyName("offset_y")]
        public int OffsetY { get; set; }

        [JsonPropertyName("window_mode")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public StartProcessWindowMode WindowMode { get; set; } = StartProcessWindowMode.ApplicationDefault;
    }

    // ---- FocusProcess ----
    /// <summary>Bringt das Hauptfenster eines laufenden Prozesses in den Vordergrund.</summary>
    public sealed class FocusProcessStep : JobStep
    {
        [JsonPropertyName("settings")]
        public FocusProcessSettings Settings { get; set; } = new();
    }

    // ---- ShowText ----
    public sealed class ShowTextStep : JobStep
    {
        [JsonPropertyName("settings")]
        public ShowTextSettings Settings { get; set; } = new();
    }

    public sealed class ShowTextSettings
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("font_size")]
        public float FontSize { get; set; } = 24f;

        /// <summary>Hex-Farbe, z. B. "#FFFFFF" oder "#FF0000".</summary>
        [JsonPropertyName("font_color")]
        public string FontColor { get; set; } = "#FFFFFF";

        /// <summary>Deckkraft von 0.0 (unsichtbar) bis 1.0 (voll opak).</summary>
        [JsonPropertyName("opacity")]
        public float Opacity { get; set; } = 1.0f;

        /// <summary>Index des Monitors (0-basiert).</summary>
        [JsonPropertyName("desktop_index")]
        public int DesktopIndex { get; set; } = 0;

        /// <summary>X-Offset in Pixel relativ zur linken oberen Ecke des gewählten Monitors.</summary>
        [JsonPropertyName("offset_x")]
        public int OffsetX { get; set; } = 100;

        /// <summary>Y-Offset in Pixel relativ zur linken oberen Ecke des gewählten Monitors.</summary>
        [JsonPropertyName("offset_y")]
        public int OffsetY { get; set; } = 100;

        /// <summary>Zeit in Millisekunden nach der der Text automatisch entfernt wird. 0 = dauerhaft.</summary>
        [JsonPropertyName("duration_ms")]
        public int DurationMs { get; set; } = 5000;

        /// <summary>Text wird am Ende des Jobs automatisch entfernt.</summary>
        [JsonPropertyName("clear_on_job_end")]
        public bool ClearOnJobEnd { get; set; } = false;
    }

    public enum FocusProcessWindowMode
    {
        Normal,
        Maximized,
        // Nur zur Abwärtskompatibilität mit bereits gespeicherten Jobs.
        Fullscreen
    }

    public enum FocusProcessAction
    {
        BringToFront,
        Minimize
    }

    public sealed class FocusProcessSettings
    {
        [JsonPropertyName("action")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public FocusProcessAction Action { get; set; } = FocusProcessAction.BringToFront;

        [JsonPropertyName("executable_path")]
        public string ExecutablePath { get; set; } = string.Empty;

        [JsonPropertyName("window_title_contains")]
        public string WindowTitleContains { get; set; } = string.Empty;

        [JsonPropertyName("window_mode")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public FocusProcessWindowMode WindowMode { get; set; } = FocusProcessWindowMode.Normal;
    }

    // ---- ActiveWindow ----
    /// <summary>Prüft, ob ein Fenster des angegebenen Prozesses das aktive Vordergrundfenster ist.</summary>
    public sealed class ActiveWindowStep : JobStep
    {
        [JsonPropertyName("settings")]
        public ActiveWindowSettings Settings { get; set; } = new();
    }

    public sealed class ActiveWindowSettings
    {
        [JsonPropertyName("process_name")]
        public string ProcessName { get; set; } = string.Empty;

        [JsonPropertyName("cache_ms")]
        public int CacheMs { get; set; } = 0;
    }

    // ---- KeyPointMatching ----
    /// <summary>Vergleicht SIFT-Keypoints eines Templates mit der Bildquelle aus einem Erfassungs-Step.</summary>
    public sealed class KeyPointMatchingStep : JobStep
    {
        [JsonPropertyName("settings")]
        public KeyPointMatchingSettings Settings { get; set; } = new();
    }

    public sealed class KeyPointMatchingSettings
    {
        [JsonPropertyName("template_path")]
        public string TemplatePath { get; set; } = string.Empty;

        [JsonPropertyName("min_match_count")]
        public int MinMatchCount { get; set; } = 10;

        [JsonPropertyName("lowes_ratio_threshold")]
        public double LowesRatioThreshold { get; set; } = 0.75;

        [JsonPropertyName("enable_roi")]
        public bool EnableROI { get; set; } = false;

        [JsonPropertyName("roi")]
        [JsonConverter(typeof(OpenCvRectJsonConverter))]
        public Rect ROI { get; set; } = new Rect(0, 0, 0, 0);

        [JsonPropertyName("source_capture_step_id")]
        public string SourceCaptureStepId { get; set; } = string.Empty;

        [JsonPropertyName("dynamic_roi_step_id")]
        public string DynamicRoiStepId { get; set; } = string.Empty;
    }

    // ---- PointComparison ----

    public enum PointComparisonMode { Offset, Expression }
    public enum PointEntrySource { Manual, JobResult }
    public enum PointMatchRequirement { All, Any }
    public enum PointAxisOperator { LessThan, LessThanOrEqual, GreaterThan, GreaterThanOrEqual, Equal, NotEqual }
    public enum ExpressionCombineMode { And, Or }

    public sealed class PointEntry
    {
        [JsonPropertyName("source")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public PointEntrySource Source { get; set; } = PointEntrySource.Manual;

        [JsonPropertyName("manual_x")]
        public int ManualX { get; set; } = 0;

        [JsonPropertyName("manual_y")]
        public int ManualY { get; set; } = 0;

        [JsonPropertyName("source_detection_step_id")]
        public string SourceDetectionStepId { get; set; } = "";

        [JsonPropertyName("use_all_detections")]
        public bool UseAllDetections { get; set; } = false;
    }

    public sealed class OffsetComparisonSettings
    {
        [JsonPropertyName("reference_source")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public PointEntrySource ReferenceSource { get; set; } = PointEntrySource.Manual;

        [JsonPropertyName("reference_x")]
        public int ReferenceX { get; set; } = 0;

        [JsonPropertyName("reference_y")]
        public int ReferenceY { get; set; } = 0;

        [JsonPropertyName("reference_detection_step_id")]
        public string ReferenceDetectionStepId { get; set; } = "";

        [JsonPropertyName("offset_x")]
        public int OffsetX { get; set; } = 10;

        [JsonPropertyName("offset_y")]
        public int OffsetY { get; set; } = 10;
    }

    public sealed class AxisExpression
    {
        [JsonPropertyName("axis")]
        public string Axis { get; set; } = "X";

        [JsonPropertyName("operator")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public PointAxisOperator Operator { get; set; } = PointAxisOperator.LessThan;

        [JsonPropertyName("value")]
        public int Value { get; set; } = 0;
    }

    public sealed class ExpressionComparisonSettings
    {
        [JsonPropertyName("expressions")]
        public List<AxisExpression> Expressions { get; set; } = new();

        [JsonPropertyName("combine_mode")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ExpressionCombineMode CombineMode { get; set; } = ExpressionCombineMode.And;
    }

    public sealed class PointComparisonSettings
    {
        [JsonPropertyName("mode")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public PointComparisonMode Mode { get; set; } = PointComparisonMode.Offset;

        [JsonPropertyName("match_requirement")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public PointMatchRequirement MatchRequirement { get; set; } = PointMatchRequirement.All;

        [JsonPropertyName("points")]
        public List<PointEntry> Points { get; set; } = new();

        [JsonPropertyName("offset_settings")]
        public OffsetComparisonSettings OffsetSettings { get; set; } = new();

        [JsonPropertyName("expression_settings")]
        public ExpressionComparisonSettings ExpressionSettings { get; set; } = new();
    }

    public sealed class PointComparisonStep : JobStep
    {
        [JsonPropertyName("settings")]
        public PointComparisonSettings Settings { get; set; } = new();
    }
}
