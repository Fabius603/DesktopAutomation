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
    public abstract class JobStep { }

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
    }

    // ---- DesktopDuplication ----
    public sealed class DesktopDuplicationStep : JobStep
    {
        [JsonPropertyName("settings")]
        public DesktopDuplicationSettings Settings { get; set; } = new();
    }

    public sealed class DesktopDuplicationSettings
    {
        [JsonPropertyName("output_device")]
        public int OutputDevice { get; set; } = 0;

        [JsonPropertyName("graphics_card_adapter")]
        public int GraphicsCardAdapter { get; set; } = 0;
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
    }
}