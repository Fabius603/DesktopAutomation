
using System.ComponentModel;
using System.Windows;
using MahApps.Metro.Controls;
using System.Windows.Input;
using DesktopAutomationApp.Behaviors;
using DesktopAutomationApp.Input;
using DesktopAutomationApp.ViewModels;

namespace DesktopAutomationApp
{
    public partial class MainWindow : MetroWindow
    {
        private bool _allowClose;

        public MainWindow()
        {
            InitializeComponent();
            PreviewKeyDown += OnPreviewKeyDown;
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Handled || DataContext is not MainViewModel viewModel
                || ViewShortcutGuard.IsReserved(e, viewModel.CurrentContent))
                return;

            if (ViewShortcutRouter.TryExecute(e, AppShortcutGestures.NavigateJobs, viewModel.ShowListJobs)
                || ViewShortcutRouter.TryExecute(e, AppShortcutGestures.NavigateMacros, viewModel.ShowListMakros)
                || ViewShortcutRouter.TryExecute(e, AppShortcutGestures.NavigateAutomations, viewModel.ShowListAutomations)
                || ViewShortcutRouter.TryExecute(e, AppShortcutGestures.ShowHelp, viewModel.ShowShortcutHelpCommand))
                return;

            // Page-wide commands are routed by the window. Immediately after a
            // navigation the keyboard focus can still belong to the sidebar (or
            // another element outside the new page), so the page itself would
            // never see PreviewKeyDown. Selection-specific step commands remain
            // local to their lists.
            if (TryExecuteCurrentPageShortcut(e, viewModel.CurrentContent))
                return;

            if (ViewShortcutRouter.TryExecute(
                    e,
                    AppShortcutGestures.ShowHelpQuestion,
                    viewModel.ShowShortcutHelpCommand))
                return;

            ViewShortcutRouter.TryExecute(
                e,
                AppShortcutGestures.ShowHelpQuestionAlternate,
                viewModel.ShowShortcutHelpCommand);
        }

        private static bool TryExecuteCurrentPageShortcut(KeyEventArgs e, object? currentContent)
        {
            return currentContent switch
            {
                JobStepsViewModel job =>
                    ViewShortcutRouter.TryExecute(e, AppShortcutGestures.Save, job.SaveCommand)
                    || ViewShortcutRouter.TryExecute(e, AppShortcutGestures.NewItem, job.AddStepCommand)
                    || ViewShortcutRouter.TryExecute(e, AppShortcutGestures.Back, job.BackCommand)
                    || ViewShortcutRouter.TryExecute(e, AppShortcutGestures.Rename, job.RenameCommand)
                    || ViewShortcutRouter.TryExecute(e, AppShortcutGestures.OpenFile, job.OpenFileCommand)
                    || ViewShortcutRouter.TryExecute(e, AppShortcutGestures.Execute, job.StartJobCommand)
                    || ViewShortcutRouter.TryExecute(e, AppShortcutGestures.Stop, job.StopJobCommand)
                    || ViewShortcutRouter.TryExecute(e, AppShortcutGestures.DebugJob, job.DebugJobCommand)
                    || ViewShortcutRouter.TryExecute(e, AppShortcutGestures.DebugStep, job.DebugStepCommand)
                    || ViewShortcutRouter.TryExecute(e, AppShortcutGestures.DebugContinue, job.DebugContinueCommand)
                    || ViewShortcutRouter.TryExecute(
                        e,
                        AppShortcutGestures.ToggleBreakpoint,
                        job.ToggleBreakpointCommand,
                        job.SelectedStep),

                MakroStepsViewModel macro =>
                    ViewShortcutRouter.TryExecute(e, AppShortcutGestures.Save, macro.SaveCommand)
                    || ViewShortcutRouter.TryExecute(e, AppShortcutGestures.NewItem, macro.AddStepCommand)
                    || ViewShortcutRouter.TryExecute(e, AppShortcutGestures.Back, macro.BackCommand)
                    || ViewShortcutRouter.TryExecute(e, AppShortcutGestures.Rename, macro.RenameCommand)
                    || ViewShortcutRouter.TryExecute(e, AppShortcutGestures.OpenFile, macro.OpenFileCommand)
                    || ViewShortcutRouter.TryExecute(e, AppShortcutGestures.Execute, macro.StartMakroCommand)
                    || ViewShortcutRouter.TryExecute(e, AppShortcutGestures.Stop, macro.StopMakroCommand)
                    || ViewShortcutRouter.TryExecute(e, AppShortcutGestures.PreviewMacro, macro.PreviewPlaybackCommand),

                AutomationDetailViewModel automation =>
                    ViewShortcutRouter.TryExecute(e, AppShortcutGestures.Save, automation.SaveCommand)
                    || ViewShortcutRouter.TryExecute(e, AppShortcutGestures.Back, automation.BackCommand)
                    || ViewShortcutRouter.TryExecute(e, AppShortcutGestures.Rename, automation.RenameCommand)
                    || ViewShortcutRouter.TryExecute(e, AppShortcutGestures.OpenFile, automation.OpenFileCommand)
                    || ViewShortcutRouter.TryExecute(e, AppShortcutGestures.Execute, automation.TriggerCommand)
                    || ViewShortcutRouter.TryExecute(e, AppShortcutGestures.Undo, automation.UndoCommand)
                    || ViewShortcutRouter.TryExecute(e, AppShortcutGestures.Redo, automation.RedoCommand)
                    || ViewShortcutRouter.TryExecute(e, AppShortcutGestures.RedoAlternate, automation.RedoCommand)
                    || ViewShortcutRouter.TryExecute(
                        e,
                        AppShortcutGestures.CaptureAutomationHotkey,
                        automation.CaptureHotkeyCommand),

                _ => false
            };
        }

        private void RestartToUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not ViewModels.MainViewModel viewModel
                || !viewModel.PrepareUpdateAndRestart())
                return;

            _allowClose = true;
            Application.Current.Shutdown();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (_allowClose) return;
            e.Cancel = true;
            Hide();
        }
    }
}
