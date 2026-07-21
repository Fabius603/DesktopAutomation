using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DesktopAutomationApp.Controls.Jobs.Shared;

public partial class PathPickerField : UserControl
{
    public static readonly DependencyProperty PathValueProperty = DependencyProperty.Register(
        nameof(PathValue), typeof(string), typeof(PathPickerField),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
    public static readonly DependencyProperty BrowseCommandProperty = DependencyProperty.Register(
        nameof(BrowseCommand), typeof(ICommand), typeof(PathPickerField));
    public static readonly DependencyProperty PreviewSourceProperty = DependencyProperty.Register(
        nameof(PreviewSource), typeof(ImageSource), typeof(PathPickerField));
    public static readonly DependencyProperty HasPreviewProperty = DependencyProperty.Register(
        nameof(HasPreview), typeof(bool), typeof(PathPickerField));
    public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(
        nameof(IsReadOnly), typeof(bool), typeof(PathPickerField));

    public PathPickerField() => InitializeComponent();

    public string PathValue { get => (string)GetValue(PathValueProperty); set => SetValue(PathValueProperty, value); }
    public ICommand? BrowseCommand { get => (ICommand?)GetValue(BrowseCommandProperty); set => SetValue(BrowseCommandProperty, value); }
    public ImageSource? PreviewSource { get => (ImageSource?)GetValue(PreviewSourceProperty); set => SetValue(PreviewSourceProperty, value); }
    public bool HasPreview { get => (bool)GetValue(HasPreviewProperty); set => SetValue(HasPreviewProperty, value); }
    public bool IsReadOnly { get => (bool)GetValue(IsReadOnlyProperty); set => SetValue(IsReadOnlyProperty, value); }
}
