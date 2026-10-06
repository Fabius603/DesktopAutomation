using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DesktopAutomation.Application.Interfaces;
using DesktopAutomation.Application.Organization;
using DesktopAutomation.Application.Services;
using DesktopAutomation.Application.Settings;
using DesktopAutomationApp.Localization;
using DesktopAutomationApp.ViewModels.Library;
using DesktopAutomationApp.Views.Library;
using TaskAutomation.Tests.TestDoubles;

namespace TaskAutomation.Tests.DesktopAutomationApp;

internal static class LibraryBrowserRenderHost
{
    internal static int Run(string directory)
    {
        try
        {
            var app = StepListRenderHost.LoadResources(directory);
            app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/DesktopAutomationApp;component/Styles/Themes/Dark.xaml") });
            using var temporary = new TemporaryDirectory();
            using var organization = new LibraryOrganizationService(Path.Combine(temporary.Path, "library.json"));
            foreach (var kind in Enum.GetValues<LibraryItemKind>())
            {
                var parent = organization.CreateFolderAsync(kind, null, "B\u00fcro").GetAwaiter().GetResult();
                var folder = organization.CreateFolderAsync(kind, parent.Id, "Rechnungen").GetAwaiter().GetResult();
                organization.CreateFolderAsync(kind, parent.Id, "Berichte").GetAwaiter().GetResult();
                organization.CreateFolderAsync(kind, null, "Daten").GetAwaiter().GetResult();
                var preferences = new Preferences();
                preferences.Current.ExpandedLibraryFolders[kind.ToString()] = [parent.Id];
                var vm = new LibraryTreeViewModel(organization, new Dialogs(), preferences, kind, "Neu");
                var opened = 0;
                var names = new[] { "Rechnungsablage", "Rechnungen umbenennen", "Monatsabschluss" };
                var descriptions = new[] { "PDFs pr\u00fcfen und ablegen", "Einheitliche Dateinamen", "Belege zusammenstellen" };
                var items = names.Select((name, index) => new LibraryItemDescriptor { Id = Guid.NewGuid(), Name = name, Subtitle = descriptions[index], Model = new object(), Open = () => opened++ }).ToArray();
                foreach (var item in items) organization.PlaceItemAsync(kind, item.Id, folder.Id).GetAwaiter().GetResult();
                // Avoid a dispatcher deadlock while loading async storage in this isolated host.
                var load = vm.SetItemsAsync(items);
                if (!load.IsCompleted)
                {
                    var frame = new System.Windows.Threading.DispatcherFrame();
                    _ = load.ContinueWith(_ => app.Dispatcher.BeginInvoke(new Action(() => frame.Continue = false)));
                    System.Windows.Threading.Dispatcher.PushFrame(frame);
                }
                load.GetAwaiter().GetResult();
                vm.SelectedFolderId = folder.Id;
                UserControl view = kind switch
                {
                    LibraryItemKind.Job => new global::DesktopAutomationApp.Views.ListJobsView(),
                    LibraryItemKind.Makro => new global::DesktopAutomationApp.Views.ListMakrosView(),
                    _ => new global::DesktopAutomationApp.Views.ListAutomationsView()
                };
                view.SetResourceReference(Control.BackgroundProperty, "App.Brush.WindowBackground");
                view.DataContext = new { Library = vm, Title = vm.RootLabel, CreateNewJobCommand = vm.NewItemCommand, NewMakroCommand = vm.NewItemCommand, NewCommand = vm.NewItemCommand, OpenFolderCommand = vm.ShowAllCommand };
                var window = new Window { Content = view, Width = 1180, Height = 760, Left = -20000, Top = -20000, ShowInTaskbar = false };
                window.Show(); window.UpdateLayout();
                var library = Descendants<LibraryTreeView>(view).Single();
                var list = (ListBox)library.FindName("LibraryNodes");
                list.SelectedIndex = 1; window.UpdateLayout();
                Assert.Equal(3, list.Items.Count);
                Assert.Equal("Library.Items", AutomationProperties.GetAutomationId(list));
                Assert.Equal(3, vm.Breadcrumbs.Count);
                Assert.Contains(Descendants<TextBlock>(view), text => text.Text == descriptions[0] && text.IsVisible);
                Save(view, Path.Combine(directory, kind + ".png"));
                vm.SearchText = "PDF"; window.UpdateLayout();
                Assert.Single(vm.ContentNodes);
                vm.OpenNodeCommand.Execute(vm.ContentNodes[0]); Assert.Equal(1, opened);
                vm.SearchText = "missing"; window.UpdateLayout(); Assert.False(vm.HasContent);
                Assert.Contains(Descendants<TextBlock>(view), text => text.Text == vm.EmptyTitle && text.IsVisible);
                vm.ClearSearchCommand.Execute(null);
                vm.SortCommand.Execute(null);
                Assert.Equal("Rechnungsablage", vm.ContentNodes[0].Name);
                vm.NavigateCommand.Execute(vm.Breadcrumbs[1]); Assert.Empty(vm.ContentNodes);
                vm.ShowAllCommand.Execute(null); Assert.Equal(3, vm.ContentNodes.Count);
                vm.SelectedFolderId = folder.Id;
                if (kind == LibraryItemKind.Job)
                {
                    window.Width = 800; window.UpdateLayout(); Save(view, Path.Combine(directory, "compact.png"));
                    LocalizationService.Instance.SetCulture("en-US");
                    app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/DesktopAutomationApp;component/Styles/Themes/Light.xaml") });
                    vm.ShowAllCommand.Execute(null); window.UpdateLayout();
                    var expectedText = ((SolidColorBrush)app.FindResource("App.Brush.TextPrimary")).Color;
                    foreach (var text in Descendants<TextBlock>(view).Where(text => names.Contains(text.Text)))
                        Assert.Equal(expectedText, ((SolidColorBrush)text.Foreground).Color);
                    Save(view, Path.Combine(directory, "light-en.png"));
                    LocalizationService.Instance.SetCulture("de-DE");
                    app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/DesktopAutomationApp;component/Styles/Themes/Black.xaml") });
                }
                window.Close();
            }
            app.Shutdown(); return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }

    private static IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is T match) yield return match;
            foreach (var nested in Descendants<T>(child)) yield return nested;
        }
    }

    private static void Save(FrameworkElement view, string path)
    {
        var bitmap = new RenderTargetBitmap((int)view.ActualWidth, (int)view.ActualHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(view);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path); encoder.Save(stream);
    }

    private sealed class Preferences : IUserPreferencesService
    {
        public UserPreferences Current { get; } = new();
        public Task LoadAsync() => Task.CompletedTask;
        public Task SaveAsync() => Task.CompletedTask;
    }
    private sealed class Dialogs : IDialogService
    {
        public Task<bool> ConfirmAsync(string message, string title) => Task.FromResult(false);
        public Task<bool?> ConfirmWithCancelAsync(string message, string title) => Task.FromResult<bool?>(false);
        public Task<string?> AskForNameAsync(string title, string prompt, string? defaultValue = null) => Task.FromResult<string?>(null);
        public void ShowError(string message, string title) => throw new InvalidOperationException(message);
    }
}
