using TaskAutomation.Contracts.Steps;

namespace DesktopAutomationApp.Localization;

/// <summary>Projects field descriptions without changing input contracts or validation.</summary>
public static class StepInputHelp
{
    public static string Description(StepFieldDescriptor field)
    {
        if (!string.IsNullOrWhiteSpace(field.DescriptionKey)) return Loc.Get(field.DescriptionKey);
        var labelKey = field.LabelKey;
        if (string.IsNullOrWhiteSpace(labelKey))
        {
            labelKey = field.Id.Split('.').Last() switch
            {
                "x" => "Ui.Step.Settings.X",
                "y" => "Ui.Step.Settings.Y",
                "width" => "Ui.Step.Settings.Width",
                "height" => "Ui.Step.Settings.Height",
                "enabled" => "Ui.Step.Settings.EnableROI",
                "monitor_index" or "desktop_index" => "Ui.Step.Settings.Monitor",
                "process_name" => "Ui.Step.Settings.ProcessName",
                "executable_path" => "Ui.Step.Settings.PathProgram",
                "window_title_contains" => "Ui.Step.Settings.WindowTitleContains",
                "model" => "Ui.Step.Settings.Model",
                "class_name" => "Ui.Step.Settings.Class",
                "result" or "text_result" => "Ui.Step.Settings.DisplayText",
                "detection_result" => "Ui.Step.DynamicRoi.Source",
                "font_size" => "Ui.Step.Settings.FontSizePt",
                "font_color" => "Ui.Step.Settings.FontColor",
                "opacity" => "Ui.Step.Settings.OpacityPercent",
                "offset_x" => "Ui.Step.Settings.XOffsetPixels",
                "offset_y" => "Ui.Step.Settings.YOffsetPixels",
                "duration_ms" => "Ui.Step.Settings.DisplayDurationMs",
                "clear_on_job_end" => "Ui.Step.Settings.RemoveWhenJobEnds",
                "label" => "Ui.UserChoice.AnswerTextRequired",
                "value" => "Ui.UserChoice.AnswerValueOptional",
                _ => string.Empty
            };
        }
        if (string.IsNullOrWhiteSpace(labelKey)) return string.Empty;
        var key = labelKey + ".Help";
        var text = Loc.Get(key);
        return text == $"[{key}]" ? string.Empty : text;
    }
}
