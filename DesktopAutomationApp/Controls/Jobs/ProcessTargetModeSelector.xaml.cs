using System.Windows;
using System.Windows.Controls;
using DesktopAutomationApp.ViewModels;

namespace DesktopAutomationApp.Controls.Jobs;

public partial class ProcessTargetModeSelector : UserControl
{
    private const bool DefaultUseProcessReference = false;

    public static readonly DependencyProperty PickerProperty = DependencyProperty.Register(
        nameof(Picker), typeof(ResultBindingPickerViewModel), typeof(ProcessTargetModeSelector));

    public static readonly DependencyProperty UseProcessReferenceProperty = DependencyProperty.Register(
        nameof(UseProcessReference), typeof(bool), typeof(ProcessTargetModeSelector),
        new FrameworkPropertyMetadata(DefaultUseProcessReference, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public ProcessTargetModeSelector() => InitializeComponent();

    public ResultBindingPickerViewModel? Picker
    {
        get => (ResultBindingPickerViewModel?)GetValue(PickerProperty);
        set => SetValue(PickerProperty, value);
    }

    public bool UseProcessReference
    {
        get => (bool)GetValue(UseProcessReferenceProperty);
        set => SetValue(UseProcessReferenceProperty, value);
    }
}
