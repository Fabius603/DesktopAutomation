using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace TaskAutomation.Contracts.Steps;

public enum StepValueKind
{
    Text,
    MultilineText,
    Integer,
    Number,
    Boolean,
    Enum,
    FilePath,
    DirectoryPath,
    Duration,
    Color,
    Point,
    Rectangle,
    ResultBinding,
    Object,
    Collection
}

public enum StepFieldWidth { Full, Half, Third }
public enum StepKnownDirectory { Documents, Pictures, Videos, Desktop }
public enum StepFilePickerKind { Any, Image, Script, Executable }
public enum StepSummaryValueFormat { Default, ShortText, DurationMilliseconds, FileName, BooleanBadge }
public enum StepValidationSeverity { Error, Warning }
public enum StepWindowsCapabilityPickerMode { StateQuery, SettingChange }

public static class StepEditorHints
{
    public const string MonitorPicker = "monitor-picker";
    public const string FilePicker = "file-picker";
    public const string DirectoryPicker = "directory-picker";
    public const string FileOrFolderPicker = "file-or-folder-picker";
    public const string CameraPicker = "camera-picker";
    public const string VisualOverlay = "visual-overlay";
    public const string ProcessNameSuggestions = "process-name-suggestions";
    public const string ExecutablePathSuggestions = "executable-path-suggestions";
    public const string StartProgramPicker = "start-program-picker";
    public const string MacroPicker = "macro-picker";
    public const string JobPicker = "job-picker";
    public const string ProcessTargetPicker = "process-target-picker";
    public const string ExecutableProcessTargetPicker = "executable-process-target-picker";
    public const string ResultBindingPicker = "result-binding-picker";
    public const string Percentage = "percentage";
    public const string RoiPicker = "roi-picker";
    public const string YoloPicker = "yolo-picker";
    public const string ConditionEditor = "condition-editor";
    public const string WindowsCapabilityPicker = "windows-capability-picker";
    public const string ScreenPointPicker = "screen-point-picker";
    public const string UserChoiceOptions = "user-choice-options";
    public const string PointEntryList = "point-entry-list";
    public const string AxisExpressionList = "axis-expression-list";
    public const string EmojiText = "emoji-text";

    private static readonly HashSet<string> Known =
    [
        MonitorPicker, FilePicker, DirectoryPicker, FileOrFolderPicker, CameraPicker, VisualOverlay,
        ProcessNameSuggestions, ExecutablePathSuggestions, StartProgramPicker, MacroPicker, JobPicker,
        ProcessTargetPicker, ExecutableProcessTargetPicker, ResultBindingPicker, Percentage, RoiPicker,
        YoloPicker, ConditionEditor, WindowsCapabilityPicker, ScreenPointPicker, UserChoiceOptions,
        PointEntryList, AxisExpressionList, EmojiText
    ];

    public static bool IsKnown(string editorHint) => Known.Contains(editorHint);
}

public sealed record StepReferenceValue(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name);

public sealed record StepProcessSelectorValue(
    [property: JsonPropertyName("process_source")] JsonNode? ProcessSource,
    [property: JsonPropertyName("process_name")] string ProcessName,
    [property: JsonPropertyName("executable_path")] string ExecutablePath);

public sealed record StepCameraSelectionValue(
    [property: JsonPropertyName("camera_id")] string CameraId,
    [property: JsonPropertyName("camera_name")] string CameraName,
    [property: JsonPropertyName("quality_mode")] string QualityMode,
    [property: JsonPropertyName("width")] int Width,
    [property: JsonPropertyName("height")] int Height,
    [property: JsonPropertyName("frames_per_second")] double FramesPerSecond,
    [property: JsonPropertyName("pixel_format")] string PixelFormat);

public sealed record StepFieldConstraints(
    decimal? Minimum = null,
    decimal? Maximum = null,
    int? MinimumLength = null,
    int? MaximumLength = null,
    IReadOnlyList<string>? AllowedValues = null);

public sealed record StepVisibilityRule(
    string FieldId,
    JsonNode? EqualsValue = null,
    IReadOnlyList<JsonNode?>? AnyOfValues = null);

public sealed record StepFieldOptionDescriptor(string Value, string LabelKey);

public sealed record StepVisualOverlayEditorOptions(
    string DetectionInputContractId,
    string TextInputContractId,
    bool SupportsDesktopPlacement = false);

public sealed record StepDirectoryPickerOptions(
    StepKnownDirectory SuggestedDirectory,
    string? SuggestedSubfolder = null);

public sealed record StepFilePickerOptions(
    StepFilePickerKind Kind = StepFilePickerKind.Any,
    bool ShowPreview = false);

public sealed record StepRoiSelectionValue(
    [property: JsonPropertyName("enabled")] bool Enabled,
    [property: JsonPropertyName("x")] int X,
    [property: JsonPropertyName("y")] int Y,
    [property: JsonPropertyName("width")] int Width,
    [property: JsonPropertyName("height")] int Height,
    [property: JsonPropertyName("dynamic_source")] JsonNode? DynamicSource);

public sealed record StepRoiPickerOptions(string DynamicInputContractId);

public sealed record StepYoloSelectionValue(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("class_name")] string ClassName);

public sealed record StepYoloPickerOptions(string RecommendedConfidenceTargetFieldId);

public sealed record StepWindowsCapabilitySelectionValue(
    [property: JsonPropertyName("capability_id")] string CapabilityId,
    [property: JsonPropertyName("parameters")] IReadOnlyDictionary<string, string?> Parameters);

public sealed record StepWindowsCapabilityPickerOptions(StepWindowsCapabilityPickerMode Mode);

public sealed record StepScreenPointSelectionValue(
    [property: JsonPropertyName("monitor_index")] int MonitorIndex,
    [property: JsonPropertyName("x")] int X,
    [property: JsonPropertyName("y")] int Y,
    [property: JsonPropertyName("coordinate_space")] string CoordinateSpace);

public sealed record StepScreenPointPickerOptions(bool DefaultToPrimaryMonitorCenter = false);

public sealed record StepUserChoiceOptionValue(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("label")] string Label,
    [property: JsonPropertyName("value")] string Value);

public sealed record StepPointEntryValue(
    [property: JsonPropertyName("source")] string Source,
    [property: JsonPropertyName("manual_x")] int ManualX,
    [property: JsonPropertyName("manual_y")] int ManualY,
    [property: JsonPropertyName("points_source")] JsonNode? PointsSource);

public sealed record StepAxisExpressionValue(
    [property: JsonPropertyName("axis")] string Axis,
    [property: JsonPropertyName("operator")] string Operator,
    [property: JsonPropertyName("value")] int Value);

public sealed record StepFieldDescriptor(
    string Id,
    string LabelKey,
    StepValueKind ValueKind,
    bool Required = false,
    JsonNode? DefaultValue = null,
    string? DescriptionKey = null,
    string? EditorHint = null,
    StepFieldConstraints? Constraints = null,
    StepFieldWidth Width = StepFieldWidth.Full,
    bool Advanced = false,
    int Order = 0,
    StepVisibilityRule? VisibleWhen = null,
    IReadOnlyList<StepFieldOptionDescriptor>? Options = null,
    string? InputContractId = null,
    IReadOnlyList<StepVisibilityRule>? VisibleWhenAll = null,
    StepVisualOverlayEditorOptions? VisualOverlayOptions = null,
    StepDirectoryPickerOptions? DirectoryPickerOptions = null,
    StepFilePickerOptions? FilePickerOptions = null,
    StepRoiPickerOptions? RoiPickerOptions = null,
    StepYoloPickerOptions? YoloPickerOptions = null,
    StepWindowsCapabilityPickerOptions? WindowsCapabilityPickerOptions = null,
    StepScreenPointPickerOptions? ScreenPointPickerOptions = null);

public sealed record StepEditorSectionDescriptor(
    string Id,
    string? TitleKey,
    IReadOnlyList<string> FieldIds,
    int Order = 0,
    bool Collapsible = false,
    bool InitiallyExpanded = true);

public sealed record StepSummaryItemDescriptor(
    string FieldId,
    StepSummaryValueFormat Format = StepSummaryValueFormat.Default,
    string? LabelKey = null,
    int Priority = 0,
    bool HideWhenEmpty = true);

public sealed record StepPresentationDescriptor(
    IReadOnlyList<StepEditorSectionDescriptor> EditorSections,
    IReadOnlyList<StepSummaryItemDescriptor> SummaryItems,
    IReadOnlyList<string> DetailFieldIds,
    string? EditorDescriptionKey = null);

public sealed record StepDescriptor(
    string TypeId,
    string CategoryId,
    string DisplayNameKey,
    string DescriptionKey,
    string? IconKey,
    IReadOnlyList<StepFieldDescriptor> Fields,
    StepPresentationDescriptor Presentation);

public sealed class StepDraft
{
    public StepDraft(string typeId) => TypeId = typeId;

    public string TypeId { get; }
    public Dictionary<string, JsonNode?> Values { get; } = new(StringComparer.Ordinal);

    public StepDraft Clone()
    {
        var clone = new StepDraft(TypeId);
        foreach (var (key, value) in Values)
            clone.Values[key] = value?.DeepClone();
        return clone;
    }
}

public sealed record StepValidationIssue(
    string Code,
    string? FieldId,
    StepValidationSeverity Severity = StepValidationSeverity.Error,
    IReadOnlyDictionary<string, object?>? Arguments = null);
