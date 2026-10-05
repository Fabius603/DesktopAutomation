using System.Windows;
using System.Windows.Media;
using MahApps.Metro.IconPacks;
using TaskAutomation.Steps.Definitions;

namespace DesktopAutomationApp.Services.Jobs;

internal static class StepIconPresentation
{
    public static string CategoryForType(Type stepType) =>
        BuiltInStepDefinitions.Instance.TryGetByType(stepType, out var definition)
            ? definition.Descriptor.CategoryId : "Unknown";

    public static Brush ForCategory(string category, FrameworkElement? owner) =>
        (owner?.TryFindResource($"Step.Category.{category}.Brush")
            ?? Application.Current?.TryFindResource($"Step.Category.{category}.Brush")) as Brush
            ?? Brushes.SlateGray;

    public static PackIconMaterialKind ForType(Type stepType)
    {
        if (!BuiltInStepDefinitions.Instance.TryGetByType(stepType, out var definition))
            return PackIconMaterialKind.ShapeOutline;
        // Adapt the catalog's library-neutral icon keys to the installed icon library.
        var iconName = definition.Descriptor.IconKey switch
        {
            "condition" => "RhombusOutline",
            "condition-alternative" => "SourceBranch",
            "else-branch" => "ArrowDecisionOutline",
            "process-check" => "CheckCircleOutline",
            "process-search" => "CardSearchOutline",
            "process-start" => "PlayCircleOutline",
            "process-stop" => "StopCircleOutline",
            "window-check" => "WindowMaximize",
            "window-focus" => "ApplicationBracketsOutline",
            "script-run" => "ConsoleLine",
            "dynamic-roi" => "CropFree",
            "movement-prediction" => "VectorPolyline",
            "mouse-click" => "CursorDefaultClickOutline",
            "mouse-click-3d" => "AxisArrow",
            "macro-run" => "KeyboardOutline",
            "job-run" => "PlayBoxMultipleOutline",
            "color-detection" => "PaletteOutline",
            "keypoint-matching" => "VectorCombine",
            "template-matching" => "ImageSearchOutline",
            "text-recognition" => "TextRecognition",
            "yolo-detection" => "ScanHelper",
            "point-comparison" => "MapMarkerDistance",
            "desktop-overlay" => "MonitorEye",
            "text" => "MessageTextOutline",
            "user-choice" => "AccountQuestionOutline",
            "image-output" => "ContentSaveOutline",
            "video-output" => "VideoOutline",
            "windows-query" => "MonitorDashboard",
            "windows-setting" => "MonitorEdit",
            _ => string.Concat(definition.Descriptor.IconKey.Split('-')
                .Select(part => char.ToUpperInvariant(part[0]) + part[1..]))
        };
        return Enum.TryParse<PackIconMaterialKind>(iconName, out var icon)
            ? icon : PackIconMaterialKind.ShapeOutline;
    }
}
