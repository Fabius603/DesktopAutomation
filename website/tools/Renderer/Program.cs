using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DesktopAutomationApp.Localization;
using DesktopAutomationApp.ViewModels;
using DesktopAutomationApp.Views;
using TaskAutomation.Jobs;

public sealed class Shell(object view)
{
    public object CurrentContent => view;
    public string WindowTitle => "DesktopAutomation";
    public bool HasUpdate => false;
    public bool IsNavigating => false;
    public string VersionLabel => "";
}

public sealed class DashboardPreview
{
    public int ActiveAutomationCount => 2;
    public int TotalAutomationCount => 2;
    public int TotalJobCount => 3;
    public int TotalMakroCount => 1;
    public int RunningTotalCount => 0;
    public object[] RunningItems => [];
    public object[] ActiveAutomations => [
        new { Name = "Arbeitsplatz vorbereiten", Trigger = "Zeitplan", Action = "Job ausführen", LastRun = "Heute, 08:30", NextRun = "Morgen, 08:30" },
        new { Name = "Dateien sichern", Trigger = "Zeitplan", Action = "Job ausführen", LastRun = "Gestern, 18:00", NextRun = "Heute, 18:00" }];
}

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        var root = Path.GetFullPath(args[0]);
        Directory.SetCurrentDirectory(root); // Relative fixture files resolve inside this isolated renderer process.
        var tests = Assembly.Load("DesktopAutomation.UiTests");
        var output = Path.GetFullPath(args[1]);
        Directory.CreateDirectory(output);
        using var catalog = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "website/docs/screenshots.json")));
        var specs = catalog.RootElement.GetProperty("entries").EnumerateArray().ToDictionary(
            e => e.GetProperty("id").GetString()!,
            e => ScreenshotSpec.FromJson(e));
        var selected = args.Length > 2 ? args[2].Split(',').ToHashSet() : specs.Keys.ToHashSet();
        if (selected.Any(id => !specs.ContainsKey(id))) throw new ArgumentException("Unknown screenshot selection.");
        var app = (Application)tests.GetType("TaskAutomation.Tests.DesktopAutomationApp.StepListRenderHost")!
            .GetMethod("LoadResources", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [output])!;
        ControlzEx.Theming.ThemeManager.Current.ChangeTheme(app, catalog.RootElement.GetProperty("theme").GetString()!);
        app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/DesktopAutomationApp;component/Styles/Themes/Light.xaml") });
        app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/DesktopAutomationApp;component/Styles/Accents/Blue.xaml") });
        LocalizationService.Instance.SetCulture(catalog.RootElement.GetProperty("culture").GetString()!);
        var options = new JsonSerializerOptions { WriteIndented = true };
        JobJsonSerialization.Configure(options);
        var jobs = new List<Job>();
        foreach (var file in new[] { "unterlagen", "sicherung", "arbeitsplatz" })
        {
            var reread = JsonSerializer.Deserialize<Job>(File.ReadAllText(Path.Combine(root, "artifacts/website-export/examples", file, "jobs", file + ".json")), options)!;
            jobs.Add(reread);
            var captures = new Dictionary<string, int> { [file] = 1 };
            if (file == "unterlagen")
            {
                captures.Add("step-auswahl", 0);
                captures.Add("step-bedingung", 1);
                captures.Add("step-dateien", 2);
                captures.Add("step-programmstart", 3);
                captures.Add("step-makro", 8);
                captures.Add("step-werte", 9);
            }
            if (file == "arbeitsplatz") captures.Add("step-prozessstatus", 0);
            foreach (var (id, index) in captures.Where(c => selected.Contains(c.Key)))
            {
            using var vm = (JobStepsViewModel)tests.GetType("TaskAutomation.Tests.DesktopAutomationApp.JobStepsViewModelExecutionTests")!
                .GetMethod("CreateRenderViewModel", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, [reread])!;
            if (id == "step-makro")
            {
                // Populate the isolated test executor's catalog before selecting its macro editor.
                var executor = (IJobExecutor)typeof(JobStepsViewModel)
                    .GetField("_jobExecutionContext", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(vm)!;
                var macro = JsonSerializer.Deserialize<TaskAutomation.Makros.Makro>(File.ReadAllText(
                    Path.Combine(root, "artifacts/website-export/examples/unterlagen/makros/notiz.json")), options)!;
                ((IDictionary<string, TaskAutomation.Makros.Makro>)executor.AllMakros).Add(macro.Id.ToString(), macro);
            }
            vm.SelectedStep = vm.Steps[index];
            if (id == "step-programmstart" && vm.SelectedGeneratedEditor is { } editor)
            {
                editor.Fields.First(f => f.Descriptor.Id == "placement_mode").SelectedEnumValue = "Custom";
                editor.Fields.First(f => f.Descriptor.Id == "offset_x").IntegerValue = 120;
                editor.Fields.First(f => f.Descriptor.Id == "offset_y").IntegerValue = 80;
            }
            var view = new JobStepsView { DataContext = vm };
            ScreenshotCapture.Render(view, id, specs, output);
            }
        }
        if (selected.Contains("startseite"))
            ScreenshotCapture.Render(new StartView { DataContext = new DashboardPreview() }, "startseite", specs, output);
        ExtraViews.Render(app, tests, jobs, root, output, selected, specs);
        ReferenceViews.Render(tests, root, output, selected, specs, options);
        app.Shutdown();
        if (!selected.SetEquals(ScreenshotCapture.RenderedIds))
            throw new InvalidOperationException("A selected catalog entry has no successful renderer fixture.");
    }
}
