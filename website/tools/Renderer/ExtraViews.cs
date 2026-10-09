using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DesktopAutomation.Application.Interfaces;
using DesktopAutomation.Application.Logging;
using DesktopAutomationApp.Models;
using DesktopAutomationApp.ViewModels;
using DesktopAutomationApp.Views;
using Microsoft.Extensions.Logging.Abstractions;
using TaskAutomation.Automations;
using TaskAutomation.Hotkeys;
using TaskAutomation.Jobs;
using TaskAutomation.Logging;
using TaskAutomation.Makros;
using TaskAutomation.WindowsIntegration;

internal static class ExtraViews
{
    public static void Render(Application app, Assembly tests, List<Job> jobs, string root, string output, HashSet<string> selected, IReadOnlyDictionary<string, ScreenshotSpec> specs)
    {
        foreach (var (id, index) in new[] { ("makro-editor", 7), ("makro-text", 3), ("makro-wartezeit", 4) })
            if (selected.Contains(id)) RenderMacro(tests, specs, output, id, index);
        if (!selected.Overlaps(["verlauf", "automationen", "automation-hotkey", "automation-ordner"])) return;
        var temporary = Path.Combine(Path.GetTempPath(), "DesktopAutomation-website-logs-" + Guid.NewGuid());
        Directory.CreateDirectory(temporary);
        try
        {
            var options = new JsonSerializerOptions();
            JobJsonSerialization.Configure(options);
            var definitions = Directory.GetFiles(Path.Combine(root, "artifacts/website-export/examples"), "*.json", SearchOption.AllDirectories)
                .Where(p => p.Contains(Path.DirectorySeparatorChar + "automations" + Path.DirectorySeparatorChar))
                .Select(p => JsonSerializer.Deserialize<AutomationDefinition>(File.ReadAllText(p), options)!).ToList();
            var catalog = new JobCatalog(jobs);
            var automations = new Automations(definitions);
            using var repository = new LogRepository(temporary);
            using var attention = new LogAttentionService(repository, Path.Combine(temporary, "attention"));
            var queries = new LogQueryService(repository, attention);
            var stamp = new DateTimeOffset(2026, 10, 8, 8, 30, 0, TimeSpan.FromHours(2));
            foreach (var (name, status, source, duration) in new[]
            {
                (jobs[0].Name, LogOutcome.Successful, LogSource.Job, 2400L),
                (jobs[1].Name, LogOutcome.Failed, LogSource.Job, 5100L),
                (jobs[2].Name, LogOutcome.Successful, LogSource.Job, 1800L),
                ("Notiz eingeben", LogOutcome.Successful, LogSource.Makro, 200L),
                ("Dateisicherung", LogOutcome.WithWarnings, LogSource.Job, 3600L)
            })
            {
                var run = repository.SaveRun(new LogRun { Id = Guid.NewGuid(), InstanceId = Guid.NewGuid(), SourceId = jobs[0].Id,
                    Source = source, Name = name, StartedAt = stamp, EndedAt = stamp.AddMilliseconds(duration), DurationMs = duration,
                    Outcome = status, ExecutionNumber = 1, Origin = "Manual", Steps = LogRunSnapshots.Steps(jobs[0]) });
                repository.Append(new LogEvent { Timestamp = stamp, Source = source, SourceName = name, Context = new(run.Id, run.InstanceId),
                    Code = status == LogOutcome.Failed ? LogCodes.StepFailed : LogCodes.RunCompleted,
                    Level = status == LogOutcome.Failed ? ExecutionLogLevel.Error : status == LogOutcome.WithWarnings ? ExecutionLogLevel.Warning : ExecutionLogLevel.Information,
                    DiagnosticCode = status == LogOutcome.Failed ? LogCodes.FileUnavailable : null,
                    Details = status == LogOutcome.Failed ? "Der Beispiel-Zielordner ist nicht verfügbar." : null });
                stamp = stamp.AddMinutes(8);
            }
            using var vm = new LogsHomeViewModel(queries, new(repository, queries), repository, catalog, automations, attention);
            // Fixed fixture dates must stay visible independently of the machine's current day.
            vm.Range = 3;
            vm.Source = null;
            if (selected.Contains("verlauf")) ScreenshotCapture.Render(new LogsHomeView { DataContext = vm }, "verlauf", specs, output, () => !vm.IsLoading && vm.Runs.Count == 5);
            foreach (var (id, definition) in new[] {
                ("automationen", definitions.First(d => d.Trigger is ScheduleAutomationTrigger)),
                ("automation-hotkey", definitions.First(d => d.Trigger is HotkeyAutomationTrigger)),
                ("automation-ordner", definitions.First(d => d.Trigger is FileSystemAutomationTrigger))
            }.Where(c => selected.Contains(c.Item1)))
            {
            definition.RunPolicy.EnabledFrom = new TimeOnly(8,0);
            definition.RunPolicy.EnabledUntil = new TimeOnly(18,0);
            var automationVm = new AutomationDetailViewModel(EditableAutomation.FromDomain(definition),
                automations, DispatchProxy.Create<IDialogService, UnusedService>(), catalog, new Macros(),
                DispatchProxy.Create<IGlobalHotkeyService, UnusedService>(), new WindowsCapabilityCatalog(), NullLogger<AutomationDetailViewModel>.Instance);
            ScreenshotCapture.Render(new AutomationDetailView { DataContext = automationVm }, id, specs, output);
            }
        }
        finally
        {
            // This path is a newly created, isolated fixture directory, never an application profile.
            if (Directory.Exists(temporary)) Directory.Delete(temporary, true);
        }
    }

    private static void RenderMacro(Assembly tests, IReadOnlyDictionary<string, ScreenshotSpec> specs, string output, string id, int index)
    {
        var first = new MakroGruppe { Id = "select", Title = "Feld auswählen" };
        var second = new MakroGruppe { Id = "input", Title = "Wert eingeben" };
        var macro = new Makro
        {
            Name = "Rechnung erfassen",
            Gruppen = new System.Collections.ObjectModel.ObservableCollection<MakroGruppe>([first, second]),
            Befehle = new System.Collections.ObjectModel.ObservableCollection<MakroBefehl>([
                new MouseMoveAbsoluteBefehl { X = 640, Y = 420, GroupId = first.Id },
                new MouseDownBefehl { Button = "Left", DelayBeforeMicroseconds = 200_000, GroupId = first.Id },
                new MouseUpBefehl { Button = "Left", DelayBeforeMicroseconds = 100_000, GroupId = first.Id },
                new TextInputBefehl { Text = "RE-2026-042", DelayBeforeMicroseconds = 200_000, GroupId = second.Id },
                new TimeoutBefehl { Duration = 500, DelayBeforeMicroseconds = 200_000, GroupId = second.Id },
                new KeyDownBefehl { Key = "Tab", DelayBeforeMicroseconds = 100_000, GroupId = second.Id },
                new KeyUpBefehl { Key = "Tab", DelayBeforeMicroseconds = 100_000, GroupId = second.Id },
                new KeyCombinationBefehl { Keys = ["Ctrl", "S"], DelayBeforeMicroseconds = 2_000_000, GroupId = second.Id }
            ])
        };
        using var vm = (MakroStepsViewModel)tests.GetType("TaskAutomation.Tests.DesktopAutomationApp.MakroStepsViewModelGroupingTests")!
            .GetMethod("CreateRenderViewModel", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)!.Invoke(null, [macro])!;
        vm.ToggleGroupCommand.Execute(first.Id);
        vm.ToggleGroupCommand.Execute(second.Id);
        vm.SelectedStep = vm.Steps[index];
        ScreenshotCapture.Render(new MakroStepsView { DataContext = vm }, id, specs, output);
    }

    internal sealed class JobCatalog(List<Job> jobs) : IJobApplicationService
    {
        public IReadOnlyDictionary<string, Job> Jobs { get; } = jobs.ToDictionary(j => j.Id.ToString());
        public Task ReloadAsync() => Task.CompletedTask;
        public Task<Job> CreateJobAsync(string name) => throw new NotSupportedException();
        public Task SaveJobAsync(Job job) => throw new NotSupportedException();
        public Task DeleteJobAsync(Guid id) => throw new NotSupportedException();
        public string GetStoragePath() => "";
    }
    internal sealed class Automations(List<AutomationDefinition> definitions) : IAutomationApplicationService
    {
        public Task<IReadOnlyList<AutomationDefinition>> LoadAllAsync() => Task.FromResult<IReadOnlyList<AutomationDefinition>>(definitions);
        public Task SaveAsync(AutomationDefinition value) => throw new NotSupportedException();
        public Task DeleteAsync(Guid id) => throw new NotSupportedException();
        public Task TriggerAsync(Guid id) => throw new NotSupportedException();
        public string GetStoragePath() => "";
    }
    internal sealed class Macros : IMakroApplicationService
    {
        public IReadOnlyDictionary<string, Makro> Makros => new Dictionary<string, Makro>();
        public Task<Makro> CreateMakroAsync(string name) => throw new NotSupportedException();
        public Task SaveMakroAsync(Makro makro) => throw new NotSupportedException();
        public Task DeleteMakroAsync(Guid id) => throw new NotSupportedException();
        public Task ReloadAsync() => Task.CompletedTask;
        public string GetStoragePath() => "";
    }
}

public class UnusedService : DispatchProxy
{
    protected override object? Invoke(MethodInfo? method, object?[]? args) =>
        throw new NotSupportedException("Screenshot fixture cannot invoke services: " + method?.Name);
}
