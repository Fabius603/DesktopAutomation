using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DesktopAutomationApp.Localization;
using DesktopAutomationApp.ViewModels;
using DesktopAutomationApp.Views;
using TaskAutomation.Makros;

namespace TaskAutomation.Tests.DesktopAutomationApp;

internal static class MacroEditorRenderHost
{
    public static int Run(string directory)
    {
        try { Render(directory); return 0; }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }

    private static void Render(string directory)
    {
        var app = StepListRenderHost.LoadResources(directory);
        app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/DesktopAutomationApp;component/Styles/Themes/Light.xaml") });
        LocalizationService.Instance.SetCulture("de-DE");
        var first = new MakroGruppe { Id = "select", Title = "Feld auswählen" };
        var second = new MakroGruppe { Id = "input", Title = "Wert eingeben" };
        using var vm = MakroStepsViewModelGroupingTests.CreateRenderViewModel(new Makro
        {
            Name = "Rechnung erfassen",
            Gruppen = new ObservableCollection<MakroGruppe>([first, second]),
            Befehle = new ObservableCollection<MakroBefehl>([
                new MouseMoveAbsoluteBefehl { X = 640, Y = 420, GroupId = first.Id },
                new MouseDownBefehl { Button = "Left", DelayBeforeMicroseconds = 200_000, GroupId = first.Id },
                new MouseUpBefehl { Button = "Left", DelayBeforeMicroseconds = 100_000, GroupId = first.Id },
                new TextInputBefehl { Text = "RE-2026-042", DelayBeforeMicroseconds = 200_000, GroupId = second.Id },
                new TimeoutBefehl { Duration = 500, GroupId = second.Id },
                new KeyDownBefehl { Key = "Tab", DelayBeforeMicroseconds = 100_000, GroupId = second.Id },
                new KeyUpBefehl { Key = "Tab", DelayBeforeMicroseconds = 100_000, GroupId = second.Id },
                new KeyCombinationBefehl { Keys = ["Ctrl", "S"], DelayBeforeMicroseconds = 2_000_000, GroupId = second.Id }
            ])
        });
        vm.ToggleGroupCommand.Execute(first.Id);
        vm.ToggleGroupCommand.Execute(second.Id);
        vm.SelectedStep = vm.Steps[3];
        var window = new global::DesktopAutomationApp.MainWindow
        {
            DataContext = new Shell(vm),
            Width = 1487,
            Height = 1058,
            Left = -20000,
            Top = -20000,
            WindowStartupLocation = WindowStartupLocation.Manual,
            ShowInTaskbar = false
        };
        window.Show();
        var transition = Stopwatch.StartNew();
        while (transition.Elapsed < TimeSpan.FromMilliseconds(450))
        {
            Pump();
            Thread.Sleep(10);
        }
        var view = Descendants(window).OfType<MakroStepsView>().Single();
        var stepList = (ListBox)view.FindName("StepsList");
        var contextRow = stepList.Items.OfType<MacroStepListItem>().Single(item => item.Step == vm.Steps[3]);
        var contextTarget = (ListBoxItem)stepList.ItemContainerGenerator.ContainerFromItem(contextRow);
        var contextMenu = view.CreateStepMenu(contextTarget, contextRow);
        global::DesktopAutomationApp.Controls.ActionMenus.Prepare(contextMenu);
        contextMenu.IsOpen = true; Pump(); contextMenu.UpdateLayout();
        var popupBitmap = new RenderTargetBitmap((int)Math.Ceiling(contextMenu.ActualWidth), (int)Math.Ceiling(contextMenu.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        popupBitmap.Render(contextMenu);
        var popupEncoder = new PngBitmapEncoder(); popupEncoder.Frames.Add(BitmapFrame.Create(popupBitmap));
        using (var popupFile = File.Create(Path.Combine(directory, "context-menu.png"))) popupEncoder.Save(popupFile);
        contextMenu.IsOpen = false; Pump();
        var back = Find<Button>(view, "Macro.Back")!;
        var title = Find<TextBlock>(view, "Macro.Title")!;
        var backPosition = back.TransformToAncestor(view).Transform(new Point());
        var titlePosition = title.TransformToAncestor(view).Transform(new Point());
        Ensure(Math.Abs(backPosition.Y + back.ActualHeight / 2 - titlePosition.Y - title.ActualHeight / 2) < 2
            && backPosition.X + back.ActualWidth <= titlePosition.X, "Back must share the title row, as in the job editor.");
        var field = Find<TextBox>(view, "Macro.Text");
        Ensure(field != null && field.IsVisible, "Selected text editor must be visible.");
        Ensure(field!.Text == "RE-2026-042", "Inspector must load selected Unicode text.");
        field.Text = "RE-2026-043";
        Pump();
        Ensure(vm.StepEditor!.Text == "RE-2026-043", "Text binding must update the draft.");
        Ensure(!vm.StartMakroCommand.CanExecute(null), "Unsaved draft must block real input execution.");
        Capture(window, directory, "light.png");
        Find<Button>(view, "Macro.ApplyStep")!.Command.Execute(null);
        Pump();
        Ensure(((TextInputBefehl)vm.Steps[3]).Text == "RE-2026-043", "Apply must change the selected command.");
        Ensure(vm.HasStepEditor && vm.SelectedStep == vm.Steps[3], "Apply must retain the selected inspector after rebuilding rows.");
        vm.UndoCommand.Execute(null); Pump();
        Ensure(((TextInputBefehl)vm.Steps[3]).Text == "RE-2026-042", "Undo must restore the text.");
        vm.SelectedStep = vm.Steps[7]; Pump();
        Ensure(vm.StepEditor!.SelectedType == "KeyCombination", "Combination must use a real editor.");
        Capture(window, directory, "combination.png");
        vm.SetSelectedSteps([vm.Steps[3], vm.Steps[4]]); Pump();
        Ensure(!vm.HasStepEditor, "Multiple selection must not display misleading single-step inputs.");
        Capture(window, directory, "multiple.png");
        vm.SetSelectedSteps([vm.Steps[3]]); Pump();
        vm.StepEditor!.Text = "";
        vm.ApplyStepEditsCommand.Execute(null); Pump();
        Ensure(vm.StepEditor.HasValidationError, "Empty text must show a validation message.");
        Capture(window, directory, "invalid.png");
        vm.StepEditor.Text = "RE-2026-042"; Pump();
        var delayField = Find<TextBox>(view, "Macro.DelayBefore")!;
        delayField.Text = "invalid"; Pump();
        vm.ApplyStepEditsCommand.Execute(null); Pump();
        Ensure(vm.StepEditor.HasValidationError && vm.StepEditor.DelayBeforeValueInput == "invalid", "Invalid numeric draft must remain visible and block apply.");
        Capture(window, directory, "invalid-number.png");
        delayField.Text = "200"; Pump();
        Ensure(!vm.StepEditor.HasValidationError, "Correcting numeric input must clear the stale error.");
        window.Width = 1300; window.Height = 700; Pump();
        Ensure(Find<Button>(view, "Macro.ApplyStep")!.IsVisible && Find<Button>(view, "Macro.PreviewStart")!.IsVisible,
            "Apply and preview must remain available at minimum supported viewport.");
        Capture(window, directory, "compact.png");
        window.Width = 1487; window.Height = 1058;
        app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/DesktopAutomationApp;component/Styles/Themes/Dark.xaml") });
        Pump(); Capture(window, directory, "dark.png");
        LocalizationService.Instance.SetCulture("en-US"); Pump();
        Ensure(vm.StepEditor!.SelectedStepDisplayName == "Enter text" && vm.RecordButtonText != "Aufnahme starten", "Locale changes must refresh inspector and recording controls.");
        Capture(window, directory, "english.png");
        var invalidCommand = new TextInputBefehl();
        vm.Steps.Add(invalidCommand);
        var invalidRow = vm.VisibleItems.OfType<MacroStepListItem>().Single(row => row.Step == invalidCommand);
        stepList.ScrollIntoView(invalidRow);
        stepList.UpdateLayout();
        WaitUntil(() => stepList.ItemContainerGenerator.ContainerFromItem(invalidRow) is ListBoxItem);
        Ensure(!invalidCommand.IsValid && Descendants(view).OfType<Grid>().Any(grid =>
            grid.DataContext is MacroStepListItem row && row.Step == invalidCommand
            && grid.Background is SolidColorBrush brush && brush.Color.R == 255 && brush.Color.A == 48),
            "Persisted invalid commands must retain their visible validation highlight.");
        vm.DiscardChanges();
        vm.PreviewPlaybackCommand.Execute(null);
        WaitUntil(() => vm.IsPreviewActive && vm.PreviewPositionSeconds > 0);
        Ensure(!vm.StartMakroCommand.CanExecute(null), "Visual preview must not overlap real input execution.");
        vm.PreviewSpeed = 2;
        vm.PreviewPositionSeconds = 2;
        Ensure(vm.PreviewPositionSeconds >= 2, "Seek must update the actual preview clock.");
        vm.PreviewStopCommand.Execute(null);
        Ensure(!vm.IsPreviewActive, "Stop must release the overlay.");
        WaitUntil(() => vm.PreviewPlaybackCommand.CanExecute(null));
        vm.PreviewPlaybackCommand.Execute(null);
        WaitUntil(() => vm.IsPreviewActive);
        vm.PreviewPositionSeconds = vm.PreviewDurationSeconds;
        WaitUntil(() => !vm.IsPreviewActive);
        Ensure(!vm.PreviewStopCommand.CanExecute(null), "Completed preview must stop automatically.");
        ((ObservableRangeCollection<MakroBefehl>)vm.Steps).ReplaceRange(
            Enumerable.Range(1, 9999).Select(_ => (MakroBefehl)new KeyDownBefehl { Key = "A" }));
        vm.SelectedStep = vm.Steps[^1]; Pump();
        var list = Find<ListBox>(view, "Macro.Steps")!;
        list.ScrollIntoView(vm.VisibleItems.Last()); Pump();
        var number = Descendants(view).OfType<TextBlock>().Single(text =>
            AutomationProperties.GetAutomationId(text) == "Macro.StepNumber" && text.Text == "9999");
        var measured = new TextBlock { Text = number.Text, FontFamily = number.FontFamily, FontSize = number.FontSize };
        measured.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        Ensure(number.ActualWidth >= measured.DesiredSize.Width, "Four-digit command numbers must fit without clipping.");
        Capture(window, directory, "four-digit.png");
        LocalizationService.Instance.SetCulture("de-DE");
        app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/DesktopAutomationApp;component/Styles/Themes/Light.xaml") });
        var settings = new MakroRecordingSettings();
        var dialog = new RecordingSettingsDialog(settings, MakroStepsViewModelGroupingTests.CreateRenderHotkeys())
        { Left = -20000, Top = -20000, WindowStartupLocation = WindowStartupLocation.Manual };
        dialog.Show();
        var dialogTransition = Stopwatch.StartNew();
        while (dialogTransition.Elapsed < TimeSpan.FromMilliseconds(450)) { Pump(); Thread.Sleep(10); }
        Pump();
        var clicks = Find<RadioButton>(dialog, "Macro.Recording.IsClicksOnly")!;
        clicks.IsChecked = true; Pump();
        var recordingModel = (RecordingSettingsDialogModel)dialog.DataContext;
        Ensure(recordingModel.IsClicksOnly
            && Find<RadioButton>(dialog, "Macro.Recording.IsScreenAccurate")!.IsChecked == false,
            "Recording mode selection must refresh every radio and selected-card binding.");
        Find<RadioButton>(dialog, "Macro.Recording.Nav.Inputs")!.IsChecked = true; Pump();
        Ensure(!clicks.IsVisible && Find<CheckBox>(dialog, "Macro.Recording.CombineKeyboardInputs")!.IsVisible,
            "Navigation must show only the selected settings section.");
        Find<CheckBox>(dialog, "Macro.Recording.CombineKeyboardInputs")!.IsChecked = true; Pump();
        Ensure(recordingModel.CombineKeyboardInputs && !settings.CombineKeyboardInputs, "Recording drafts must remain isolated until applied.");
        Capture(dialog, directory, "recording-inputs.png");
        Find<RadioButton>(dialog, "Macro.Recording.Nav.Precision")!.IsChecked = true; Pump();
        Find<MahApps.Metro.Controls.NumericUpDown>(dialog, "Macro.Recording.MinimumIntervalMilliseconds")!.Value = 2.5; Pump();
        Ensure(recordingModel.PrecisionSummary.Contains("2,5") && recordingModel.MinimumIntervalMicroseconds == 2500,
            "Precision edits must update the navigation summary without losing microseconds.");
        Capture(dialog, directory, "recording-precision.png");
        Find<MahApps.Metro.Controls.NumericUpDown>(dialog, "Macro.Recording.MinimumIntervalMilliseconds")!.Value = 1; Pump();
        Find<RadioButton>(dialog, "Macro.Recording.Nav.Recording")!.IsChecked = true; Pump();
        Ensure(Find<Button>(dialog, "Macro.Recording.Apply")!.IsVisible, "Recording actions must remain visible.");
        Capture(dialog, directory, "recording-light.png");
        app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/DesktopAutomationApp;component/Styles/Themes/Dark.xaml") });
        dialog.Width = 820; dialog.Height = 650; Pump();
        Capture(dialog, directory, "recording-dark-compact.png");
        LocalizationService.Instance.SetCulture("en-US"); Pump();
        Ensure(recordingModel.InputsSummary == "Keyboard and mouse", "Language changes must refresh navigation summaries.");
        Capture(dialog, directory, "recording-english.png");
        dialog.Close();
        Ensure(settings.Mode == MakroRecordingMode.ScreenAccurateAbsolute && !settings.CombineKeyboardInputs,
            "Closing without applying must preserve original settings.");
        foreach (var apply in new[] { false, true })
        {
            var modal = new RecordingSettingsDialog(settings, MakroStepsViewModelGroupingTests.CreateRenderHotkeys())
            { Left = -20000, Top = -20000, WindowStartupLocation = WindowStartupLocation.Manual };
            Dispatcher.CurrentDispatcher.BeginInvoke(() =>
            {
                Find<RadioButton>(modal, "Macro.Recording.IsClicksOnly")!.IsChecked = true;
                Find<RadioButton>(modal, "Macro.Recording.Nav.Inputs")!.IsChecked = true; Pump();
                Find<CheckBox>(modal, "Macro.Recording.CombineKeyboardInputs")!.IsChecked = true;
                Find<Button>(modal, apply ? "Macro.Recording.Apply" : "Macro.Recording.Cancel")!
                    .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }, DispatcherPriority.ApplicationIdle);
            Ensure(modal.ShowDialog() == apply, "Modal Apply/Cancel must return the expected result.");
            Ensure(modal.Settings.CombineKeyboardInputs == apply, "Only Apply must return the edited snapshot.");
            Ensure(!settings.CombineKeyboardInputs, "Apply must return a snapshot without mutating its caller's settings.");
        }
        window.Close(); app.Shutdown();
    }

    private static void Ensure(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        yield return root;
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
            foreach (var child in Descendants(VisualTreeHelper.GetChild(root, index))) yield return child;
    }
    private static T? Find<T>(DependencyObject root, string id) where T : FrameworkElement
        => Descendants(root).OfType<T>().FirstOrDefault(item => AutomationProperties.GetAutomationId(item) == id);
    private static void WaitUntil(Func<bool> condition)
    {
        var timeout = Stopwatch.StartNew();
        while (!condition())
        {
            if (timeout.Elapsed > TimeSpan.FromSeconds(5)) throw new TimeoutException("Macro UI did not reach expected state.");
            Pump(); Thread.Sleep(10);
        }
    }
    private static void Pump() => Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
    private static void Capture(Window window, string directory, string name)
    {
        Pump(); window.UpdateLayout();
        var bitmap = new RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(window);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(Path.Combine(directory, name)); encoder.Save(stream);
    }
    private sealed class Shell(MakroStepsViewModel vm)
    {
        public object CurrentContent => vm;
        public string WindowTitle => "DesktopAutomation";
        public string VersionLabel => "";
        public bool HasUpdate => false;
        public bool IsNavigating => false;
    }
}
