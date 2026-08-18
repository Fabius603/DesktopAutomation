using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DesktopAutomationApp.Controls;

public partial class LibraryEditorHeader : UserControl
{
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title), typeof(string), typeof(LibraryEditorHeader), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty BackCommandProperty = RegisterCommand(nameof(BackCommand));
    public static readonly DependencyProperty RenameCommandProperty = RegisterCommand(nameof(RenameCommand));
    public static readonly DependencyProperty OpenFileCommandProperty = RegisterCommand(nameof(OpenFileCommand));
    public static readonly DependencyProperty ExecuteCommandProperty = RegisterCommand(nameof(ExecuteCommand));
    public static readonly DependencyProperty StopCommandProperty = RegisterCommand(nameof(StopCommand));
    public static readonly DependencyProperty SaveCommandProperty = RegisterCommand(nameof(SaveCommand));
    public static readonly DependencyProperty DiscardCommandProperty = RegisterCommand(nameof(DiscardCommand));
    public static readonly DependencyProperty HasExecutionCommandsProperty = DependencyProperty.Register(
        nameof(HasExecutionCommands), typeof(bool), typeof(LibraryEditorHeader),
        new PropertyMetadata(false, OnExecutionStateChanged));
    public static readonly DependencyProperty IsRunningProperty = DependencyProperty.Register(
        nameof(IsRunning), typeof(bool), typeof(LibraryEditorHeader),
        new PropertyMetadata(false, OnExecutionStateChanged));
    public static readonly DependencyProperty HasUnsavedChangesProperty = DependencyProperty.Register(
        nameof(HasUnsavedChanges), typeof(bool), typeof(LibraryEditorHeader), new PropertyMetadata(false));
    public static readonly DependencyProperty ExecuteToolTipProperty = DependencyProperty.Register(
        nameof(ExecuteToolTip), typeof(string), typeof(LibraryEditorHeader), new PropertyMetadata(null));
    public static readonly DependencyProperty StopToolTipProperty = DependencyProperty.Register(
        nameof(StopToolTip), typeof(string), typeof(LibraryEditorHeader), new PropertyMetadata(null));

    public LibraryEditorHeader()
    {
        InitializeComponent();
        UpdateExecutionVisibility();
    }

    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public ICommand? BackCommand { get => (ICommand?)GetValue(BackCommandProperty); set => SetValue(BackCommandProperty, value); }
    public ICommand? RenameCommand { get => (ICommand?)GetValue(RenameCommandProperty); set => SetValue(RenameCommandProperty, value); }
    public ICommand? OpenFileCommand { get => (ICommand?)GetValue(OpenFileCommandProperty); set => SetValue(OpenFileCommandProperty, value); }
    public ICommand? ExecuteCommand { get => (ICommand?)GetValue(ExecuteCommandProperty); set => SetValue(ExecuteCommandProperty, value); }
    public ICommand? StopCommand { get => (ICommand?)GetValue(StopCommandProperty); set => SetValue(StopCommandProperty, value); }
    public ICommand? SaveCommand { get => (ICommand?)GetValue(SaveCommandProperty); set => SetValue(SaveCommandProperty, value); }
    public ICommand? DiscardCommand { get => (ICommand?)GetValue(DiscardCommandProperty); set => SetValue(DiscardCommandProperty, value); }
    public bool HasExecutionCommands { get => (bool)GetValue(HasExecutionCommandsProperty); set => SetValue(HasExecutionCommandsProperty, value); }
    public bool IsRunning { get => (bool)GetValue(IsRunningProperty); set => SetValue(IsRunningProperty, value); }
    public bool HasUnsavedChanges { get => (bool)GetValue(HasUnsavedChangesProperty); set => SetValue(HasUnsavedChangesProperty, value); }
    public string? ExecuteToolTip { get => (string?)GetValue(ExecuteToolTipProperty); set => SetValue(ExecuteToolTipProperty, value); }
    public string? StopToolTip { get => (string?)GetValue(StopToolTipProperty); set => SetValue(StopToolTipProperty, value); }

    private static DependencyProperty RegisterCommand(string name) =>
        DependencyProperty.Register(name, typeof(ICommand), typeof(LibraryEditorHeader), new PropertyMetadata(null));

    private static void OnExecutionStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((LibraryEditorHeader)d).UpdateExecutionVisibility();

    private void UpdateExecutionVisibility()
    {
        if (ExecuteButton == null) return;

        ExecuteButton.Visibility = HasExecutionCommands && !IsRunning ? Visibility.Visible : Visibility.Collapsed;
        StopButton.Visibility = HasExecutionCommands && IsRunning ? Visibility.Visible : Visibility.Collapsed;
        ExecutionSeparator.Visibility = HasExecutionCommands ? Visibility.Visible : Visibility.Collapsed;
    }

    private void MoreButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { ContextMenu: { } menu } button)
        {
            menu.PlacementTarget = button;
            menu.IsOpen = true;
            e.Handled = true;
        }
    }
}
