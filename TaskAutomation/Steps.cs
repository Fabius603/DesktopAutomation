using System.Text.Json.Serialization;
using OpenCvSharp; // Für Rect und TemplateMatchModes

namespace TaskAutomation
{
    public class TemplateMatchingStep
    {
        [JsonPropertyName("template_path")]
        public string TemplatePath { get; set; }

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

    public class DesktopDuplicationStep
    {
        [JsonPropertyName("output_device")]
        public int OutputDevice { get; set; } = 0;

        [JsonPropertyName("graphics_card_adapter")]
        public int GraphicsCardAdapter { get; set; } = 0;
    }

    public class ProcessDuplicationStep
    {
        [JsonPropertyName("process_name")]
        public string ProcessName { get; set; } = string.Empty;
    }

    public class ShowImageStep
    {
        [JsonPropertyName("window_name")]
        public string WindowName { get; set; } = "MyWindow";

        [JsonPropertyName("show_raw_image")]
        public bool ShowRawImage { get; set; } = true;

        [JsonPropertyName("show_processed_image")]
        public bool ShowProcessedImage { get; set; } = true;
    }

    public class VideoCreationStep
    {
        [JsonPropertyName("save_path")]
        public string SavePath { get; set; } = string.Empty;

        [JsonPropertyName("file_name")]
        public string FileName { get; set; } = "output.mp4";

        [JsonPropertyName("use_raw_image")]
        public bool ShowRawImage { get; set; } = true;

        [JsonPropertyName("use_processed_image")]
        public bool ShowProcessedImage { get; set; } = true;
    }
}