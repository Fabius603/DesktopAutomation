using System.Windows.Controls;
using System.Windows;
using DesktopAutomationApp.ViewModels;

namespace DesktopAutomationApp.Controls.Jobs.Editors.Generated;

public partial class GeneratedStepEditor : UserControl
{
    public GeneratedStepEditor() => InitializeComponent();

    private void OpenValueSourceMenu_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { ContextMenu: { } menu } button) return;
        menu.PlacementTarget = button;
        menu.IsOpen = true;
    }
}

public sealed class GeneratedStepFieldTemplateSelector : DataTemplateSelector
{
    public bool IgnoreInputReference { get; set; }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
    {
        if (item is not GeneratedStepFieldViewModel field || container is not FrameworkElement element)
            return base.SelectTemplate(item, container);

        var key = field switch
        {
            { UsesInputReference: true } when !IgnoreInputReference => "InputReferenceFieldTemplate",
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
            { UsesValueReferencePicker: true } => "ValueReferenceFieldTemplate",
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
