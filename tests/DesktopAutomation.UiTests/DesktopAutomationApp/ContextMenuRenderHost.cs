using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DesktopAutomationApp.Controls;
using DesktopAutomationApp.Localization;
using DesktopAutomationApp.ViewModels;

namespace TaskAutomation.Tests.DesktopAutomationApp;

internal static class ContextMenuRenderHost
{
    public static int Run(string directory)
    {
        try
        {
            Directory.CreateDirectory(directory);
            var app = StepListRenderHost.LoadResources(directory);
            VerifyActionIcons();
            var list = new ListBox { SelectionMode = SelectionMode.Extended, ItemsSource = new[] { "one", "two", "three" } };
            var window = new Window { Content = list, Width = 500, Height = 400, Left = -20000, ShowInTaskbar = false };
            window.Show(); window.UpdateLayout();
            list.SelectedItems.Add("one"); list.SelectedItems.Add("two");
            ActionMenus.SelectContextItem(list, (ListBoxItem)list.ItemContainerGenerator.ContainerFromIndex(1));
            if (list.SelectedItems.Count != 2) throw new InvalidOperationException("Context selection was discarded.");
            ActionMenus.SelectContextItem(list, (ListBoxItem)list.ItemContainerGenerator.ContainerFromIndex(2));
            if (list.SelectedItems.Count != 1 || list.SelectedItem?.ToString() != "three") throw new InvalidOperationException("Context target was not selected.");
            var answers = new GeneratedUserChoiceOptionsEditorViewModel(null);
            var answerList = new ListBox { SelectionMode = SelectionMode.Extended, ItemsSource = answers.Options };
            var original = answers.Options[0]; original.Label = "original";
            var answerMenu = EditorCollectionMenus.Create(answerList, answerList, "Answers", [original]);
            var remove = answerMenu.Items.OfType<MenuItem>().Last();
            if (remove.Command.CanExecute(null)) throw new InvalidOperationException("Minimum answer count was not protected.");
            var duplicate = answerMenu.Items.OfType<MenuItem>().First();
            duplicate.Command.Execute(null);
            if (answers.Options.Count != 3 || answerList.SelectedItems.Count != 1 || ReferenceEquals(answerList.SelectedItem, original))
                throw new InvalidOperationException("Answer duplication did not select an independent copy.");
            var copy = (UserChoiceOptionEditorViewModel)answerList.SelectedItem;
            if (copy.Id == original.Id || copy.Label != original.Label) throw new InvalidOperationException("Answer copy identity or draft was lost.");
            var allMenu = EditorCollectionMenus.Create(answerList, answerList, "Answers", answers.Options.Cast<object>().ToArray());
            if (allMenu.Items.OfType<MenuItem>().Last().Command.CanExecute(null)) throw new InvalidOperationException("Batch removal exceeded answer limits.");
            foreach (var theme in new[] { "Dark", "Light" })
            {
                ControlzEx.Theming.ThemeManager.Current.ChangeTheme(app, theme + ".Blue");
                app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri($"pack://application:,,,/DesktopAutomationApp;component/Styles/Themes/{theme}.xaml") });
                var menu = ActionMenus.Create(list);
                ActionMenus.Add(menu, "Ui.Common.EditStep", new RelayCommand(() => { }));
                menu.Items.Add(new Separator());
                ActionMenus.Add(menu, "Ui.Common.Copy", new RelayCommand(() => { }), gesture: "Shortcut.CtrlC");
                var duplicateItem = ActionMenus.Add(menu, "Ui.Macro.Steps.DuplicateStep", new RelayCommand(() => { }), gesture: "Shortcut.CtrlD");
                menu.Items.Add(new Separator());
                ActionMenus.Add(menu, "Ui.Common.MoveStepUp", new RelayCommand(() => { }));
                ActionMenus.Add(menu, "Ui.Common.MoveStepDown", new RelayCommand(() => { }));
                var move = ActionMenus.Add(menu, "Ui.Context.Group", new RelayCommand(() => { }));
                ActionMenus.Add(move, "Ui.Context.LibraryRoot", new RelayCommand(() => { }));
                menu.Items.Add(new Separator());
                ActionMenus.Add(menu, "Ui.Macro.Steps.DeleteStep", new RelayCommand(() => { }), gesture: "Shortcut.Delete");
                ActionMenus.Prepare(menu);
                if (!ReferenceEquals(menu.Style, app.FindResource("App.ContextMenuStyle"))) throw new InvalidOperationException("Theme replaced the menu design.");
                var legacy = new ContextMenu();
                var legacyItem = new MenuItem { Header = "legacy", Command = new RelayCommand(() => { }) };
                var separator = new Separator(); legacy.Items.Add(legacyItem); legacy.Items.Add(separator);
                var nested = new MenuItem { Header = "nested" }; legacyItem.Items.Add(nested);
                ActionMenus.Prepare(legacy);
                if (!ReferenceEquals(legacyItem.Style, app.FindResource("App.MenuItemStyle"))
                    || !ReferenceEquals(nested.Style, legacyItem.Style)
                    || !ReferenceEquals(separator.Style, app.FindResource("App.MenuSeparatorStyle")))
                    throw new InvalidOperationException("Legacy and nested menus diverged from the shared design.");
                menu.IsOpen = true;
                menu.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
                menu.ApplyTemplate(); menu.UpdateLayout();
                move.IsSubmenuOpen = true;
                menu.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
                if (move.Template.FindName("PART_Popup", move) is not System.Windows.Controls.Primitives.Popup { IsOpen: true, Child: FrameworkElement { ActualWidth: > 0 } })
                    throw new InvalidOperationException("Move submenu did not render.");
                move.IsSubmenuOpen = false;
                duplicateItem.Focus();
                FrameworkElement visual = menu;
                var bitmap = new RenderTargetBitmap((int)Math.Ceiling(visual.ActualWidth), (int)Math.Ceiling(visual.ActualHeight), 96, 96, PixelFormats.Pbgra32);
                bitmap.Render(visual);
                var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using var file = File.Create(Path.Combine(directory, theme + ".png")); encoder.Save(file);
                menu.IsOpen = false;
                var actions = ActionMenus.Create(list);
                foreach (var key in new[] { "Ui.Context.Enable", "Ui.Context.Disable", "Ui.Context.Debug", "Ui.Context.SetBreakpoints", "Ui.Context.RemoveBreakpoints", "Ui.Common.Save", "Ui.Common.Discard", "Ui.Context.Group", "Ui.Macro.Group.RemoveSelected", "Ui.Context.MoveTo", "Ui.Context.LibraryRoot", "Ui.Yolo.Download", "Logs.Ui.Export", "Logs.Ui.MarkSeen", "Logs.Ui.ResolveProblem", "Logs.Ui.ReopenProblem", "Ui.Job.StepInput.Source.JobVariable", "Ui.Job.StepInput.Source.StepResult", "Ui.Job.StepInput.Source.Secret", "Tray.Exit" })
                    ActionMenus.Add(actions, key, new RelayCommand(() => { }));
                actions.IsOpen = true;
                actions.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
                actions.ApplyTemplate(); actions.UpdateLayout();
                var actionsBitmap = new RenderTargetBitmap((int)Math.Ceiling(actions.ActualWidth), (int)Math.Ceiling(actions.ActualHeight), 96, 96, PixelFormats.Pbgra32);
                actionsBitmap.Render(actions);
                var actionsEncoder = new PngBitmapEncoder(); actionsEncoder.Frames.Add(BitmapFrame.Create(actionsBitmap));
                using var actionsFile = File.Create(Path.Combine(directory, $"Actions-{theme}.png")); actionsEncoder.Save(actionsFile);
                actions.IsOpen = false;
            }
            window.Close(); app.Shutdown();
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }

    private static void VerifyActionIcons()
    {
        var command = new RelayCommand(() => { });
        foreach (var culture in new[] { "de-DE", "en-US" })
        {
            LocalizationService.Instance.SetCulture(culture);
            var menu = ActionMenus.Create(new ListBox());
            string Icon(string key) => ((System.Windows.Shapes.Path)ActionMenus.Add(menu, key, command).Icon).Data.ToString();
            foreach (var pair in new[] { ("Ui.Context.Enable", "Ui.Context.Disable"), ("Ui.Context.SetBreakpoints", "Ui.Context.RemoveBreakpoints"), ("Ui.Context.RemoveBreakpoints", "Ui.Job.Steps.DeleteStep"), ("Ui.Context.Group", "Ui.Macro.Group.RemoveSelected"), ("Ui.Common.Copy", "Ui.Context.Duplicate"), ("Ui.Yolo.Download", "Logs.Ui.Export"), ("Ui.Common.Save", "Ui.Common.Discard"), ("Ui.Job.StepInput.Source.JobVariable", "Ui.Job.StepInput.Source.StepResult"), ("Ui.Job.StepInput.Source.Secret", "Ui.Job.StepInput.Source.Direct") })
                if (Icon(pair.Item1) == Icon(pair.Item2)) throw new InvalidOperationException($"Opposing or distinct actions share an icon: {pair}.");
            var removeBreakpoint = ActionMenus.Add(menu, "Ui.Context.RemoveBreakpoints", command);
            var ungroup = ActionMenus.Add(menu, "Ui.Macro.Group.RemoveSelected", command);
            var delete = ActionMenus.Add(menu, "Ui.Job.Steps.DeleteStep", command);
            if (Equals(removeBreakpoint.Foreground, delete.Foreground) || Equals(ungroup.Foreground, delete.Foreground))
                throw new InvalidOperationException("Non-destructive actions use the deletion color.");
            var bound = ActionMenus.Bind(menu, new { ApplyCommand = command }, "Ui.Context.Disable", "ApplyCommand");
            if (((System.Windows.Shapes.Path)bound.Icon).Data.ToString() != Icon("Ui.Context.Disable"))
                throw new InvalidOperationException("Bound command names override action semantics.");
            var legacy = new ContextMenu();
            var save = new MenuItem();
            save.SetBinding(MenuItem.HeaderProperty, new System.Windows.Data.Binding("[Ui.Common.Save]") { Source = LocalizationService.Instance });
            legacy.Items.Add(save); ActionMenus.Prepare(legacy);
            if (((System.Windows.Shapes.Path)save.Icon).Data.ToString() != Icon("Ui.Common.Save"))
                throw new InvalidOperationException("Translated XAML menus lose their action identity.");
        }
        LocalizationService.Instance.SetCulture("de-DE");
    }
}
