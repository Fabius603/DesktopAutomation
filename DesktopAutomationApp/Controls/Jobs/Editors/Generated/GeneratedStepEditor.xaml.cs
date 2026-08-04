using System.Windows.Controls;
using System.Windows;
using DesktopAutomationApp.ViewModels;

namespace DesktopAutomationApp.Controls.Jobs.Editors.Generated;

public partial class GeneratedStepEditor : UserControl
{
    public GeneratedStepEditor() => InitializeComponent();
}

public sealed class GeneratedStepFieldTemplateSelector : DataTemplateSelector
{
    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
    {
        if (item is not GeneratedStepFieldViewModel field || container is not FrameworkElement element)
            return base.SelectTemplate(item, container);

        var key = field switch
        {
            { UsesEmojiText: true } => "EmojiTextFieldTemplate",
            { UsesMultilineTextInput: true } => "MultilineTextFieldTemplate",
            { IsBoolean: true } => "BooleanFieldTemplate",
            { UsesSuggestions: true } => "SuggestionsFieldTemplate",
            { UsesSuggestionFilePicker: true } => "SuggestionFileFieldTemplate",
            { UsesChoicePicker: true } => "ChoiceFieldTemplate",
            { UsesEnumPicker: true } => "EnumFieldTemplate",
            { UsesProcessTargetPicker: true } => "ProcessTargetFieldTemplate",
            { UsesFilePicker: true } => "FileFieldTemplate",
            { UsesDirectoryPicker: true } => "DirectoryFieldTemplate",
            { UsesFileOrFolderPicker: true } => "FileOrFolderFieldTemplate",
            { UsesMonitorPicker: true } => "MonitorFieldTemplate",
            { UsesResultBindingPicker: true } => "ResultBindingFieldTemplate",
            { UsesPercentagePicker: true } => "PercentageFieldTemplate",
            { UsesColorPicker: true } => "ColorFieldTemplate",
            { UsesCameraPicker: true } => "CameraFieldTemplate",
            { UsesVisualOverlay: true } => "VisualOverlayFieldTemplate",
            { UsesRoiPicker: true } => "RoiFieldTemplate",
            { UsesYoloPicker: true } => "YoloFieldTemplate",
            { UsesConditionEditor: true } => "ConditionFieldTemplate",
            { UsesWindowsCapabilityPicker: true } => "WindowsCapabilityFieldTemplate",
            { UsesScreenPointPicker: true } => "ScreenPointFieldTemplate",
            { UsesUserChoiceOptions: true } => "UserChoiceOptionsFieldTemplate",
            { UsesPointEntryList: true } => "PointEntryListFieldTemplate",
            { UsesAxisExpressionList: true } => "AxisExpressionListFieldTemplate",
            _ => "TextFieldTemplate"
        };

        return element.TryFindResource(key) as DataTemplate
            ?? base.SelectTemplate(item, container);
    }
}
