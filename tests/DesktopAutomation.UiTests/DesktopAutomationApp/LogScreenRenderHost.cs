using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DesktopAutomation.Application.Interfaces;
using DesktopAutomation.Application.Logging;
using DesktopAutomationApp.Localization;
using DesktopAutomationApp.ViewModels;
using DesktopAutomationApp.Views;
using TaskAutomation.Automations;
using TaskAutomation.Jobs;
using TaskAutomation.Logging;
using TaskAutomation.Tests.TestDoubles;

namespace TaskAutomation.Tests.DesktopAutomationApp;

internal static class LogScreenRenderHost
{
    public static int Run(string directory)
    {
        try { Render(directory); Console.WriteLine("Log screen interactions and rendering passed."); return 0; }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
    private static void Render(string directory)
    {
        var app = StepListRenderHost.LoadResources(directory);
        app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/DesktopAutomationApp;component/Styles/Themes/Dark.xaml") });
        LocalizationService.Instance.SetCulture("de-DE");
        using var temporary = new TemporaryDirectory();
        var retentionClock = new RetentionClock();
        using var repository = new LogRepository(temporary.Path, retentionClock);
        using var attention = new LogAttentionService(repository, Path.Combine(temporary.Path, "attention"));
        var queries = new LogQueryService(repository, attention);
        var jobs = new JobCatalog();
        var automation = new AutomationDefinition { Name = "Ordner überwachen", Trigger = new FileSystemAutomationTrigger { DirectoryPath = @"C:\Eingang" }, Action = new() { JobId = jobs.Job.Id, Name = jobs.Job.Name } };
        var definitions = new Automations(automation);
        var stamp = DateTimeOffset.Now.AddMinutes(-5);
        var run = repository.SaveRun(new LogRun
        {
            Id = Guid.NewGuid(),
            InstanceId = Guid.NewGuid(),
            SourceId = jobs.Job.Id,
            Source = LogSource.Job,
            Name = jobs.Job.Name,
            StartedAt = stamp,
            EndedAt = stamp.AddSeconds(8.4),
            DurationMs = 8400,
            Outcome = LogOutcome.Failed,
            ExecutionNumber = 128,
            Origin = "Automation",
            OriginName = "Büroabend",
            Context = new(AutomationId: automation.Id),
            Steps = LogRunSnapshots.Steps(jobs.Job)
        });
        var steps = run.Steps.ToArray();
        for (var index = 0; index < 4; index++)
        {
            var execution = Guid.NewGuid(); var problem = index == 3;
            repository.Append(new LogEvent { Timestamp = stamp.AddSeconds(index), Source = LogSource.Job, Context = new(run.Id, run.InstanceId, StepId: steps[index].Id, StepExecutionId: execution), Code = LogCodes.StepStarted, Phase = "Main", Iteration = 1 });
            repository.Append(new LogEvent
            {
                Timestamp = stamp.AddSeconds(index + 1),
                Source = LogSource.Job,
                SourceName = jobs.Job.Name,
                Context = new(run.Id, run.InstanceId, StepId: steps[index].Id, StepExecutionId: execution),
                Code = problem ? LogCodes.StepFailed : LogCodes.StepCompleted,
                Phase = "Main",
                Iteration = 1,
                Level = problem ? ExecutionLogLevel.Error : ExecutionLogLevel.Information,
                DiagnosticCode = problem ? LogCodes.FileUnavailable : null,
                DurationMs = problem ? 6800 : index == 1 ? 1100 : 200,
                FlowEffect = problem ? new("StopPhase") : null,
                Details = problem ? "System.IO.DirectoryNotFoundException" : null,
                Paths = problem ? [new("TargetDirectory", @"Z:\Ablage\Rechnungen")] : [],
                Parameters = new() { ["ResultKind"] = index == 1 ? "DesktopDuplicationResult" : "", ["HasImage"] = index == 1 ? "True" : "False", ["Width"] = "1920", ["Height"] = "1080" }
            });
        }
        foreach (var (name, outcome, origin, duration) in new[] {
            ("Bildprüfung", LogOutcome.Running, "Manual", 72000L),
            ("Dateien sortieren", LogOutcome.WithWarnings, "Automation", 12600L),
            ("Datensicherung", LogOutcome.Successful, "Automation", 24100L),
            ("Bildprüfung", LogOutcome.Stopped, "Manual", 5200L) })
        {
            repository.SaveRun(new LogRun
            {
                Id = Guid.NewGuid(),
                InstanceId = Guid.NewGuid(),
                SourceId = Guid.NewGuid(),
                Source = LogSource.Job,
                Name = name,
                StartedAt = stamp.AddMinutes(-2 - repository.ReadRuns().Count),
                DurationMs = outcome == LogOutcome.Running ? null : duration,
                EndedAt = outcome == LogOutcome.Running ? null : stamp,
                Outcome = outcome,
                ExecutionNumber = 87
            });
        }
        var instance = repository.ReadRuns().First(item => item.Outcome == LogOutcome.Running);
        for (var index = 0; index < 4; index++)
        {
            var trigger = Guid.NewGuid(); var time = stamp.AddMinutes(-index * 5);
            repository.Append(new LogEvent
            {
                Timestamp = time,
                Source = LogSource.Automation,
                SourceId = automation.Id,
                SourceName = automation.Name,
                Context = new(AutomationId: automation.Id, TriggerId: trigger),
                Code = LogCodes.Trigger,
                Trigger = new("FileSystemEvent", "Created", @"C:\Eingang", jobs.Job.Id, "Job", "Dateien sortieren"),
                Parameters = new() { ["FileName"] = "Rechnung_042.pdf" }
            });
            repository.Append(new LogEvent
            {
                Timestamp = time,
                Source = LogSource.Automation,
                SourceId = automation.Id,
                SourceName = automation.Name,
                Context = new(InstanceId: index % 2 == 0 ? null : run.InstanceId, AutomationId: automation.Id, TriggerId: trigger),
                Code = LogCodes.AutomationDecision,
                RelatedInstanceIds = index == 0 ? [instance.InstanceId] : [],
                Parameters = new() { ["Reason"] = index == 0 ? "AlreadyRunning" : index == 2 ? "Cooldown" : "StartRequested", ["EligibleAt"] = stamp.AddMinutes(1).ToString("O", CultureInfo.InvariantCulture) }
            });
        }
        repository.Append(new LogEvent { Timestamp = stamp.AddMinutes(-10), Source = LogSource.Application, Category = LogArea.Video, Code = LogCodes.Message, Level = ExecutionLogLevel.Warning });
        repository.Append(new LogEvent { Timestamp = stamp.AddMinutes(-12), Source = LogSource.Automation, Category = LogArea.Automation, Code = LogCodes.Message, Level = ExecutionLogLevel.Warning, DiagnosticCode = LogCodes.FileUnavailable });
        using var vm = new LogsHomeViewModel(queries, new(repository, queries), repository, jobs, definitions, attention);
        var view = new LogsHomeView { DataContext = vm, Margin = new Thickness(26, 24, 26, 24) };
        var window = new Window
        {
            Width = 1200,
            Height = 1010,
            Left = -20000,
            Top = -20000,
            WindowStyle = WindowStyle.None,
            ShowInTaskbar = false,
            Background = (Brush)app.FindResource("App.Brush.WindowBackground"),
            Content = view
        };
        var bindingErrors = new BindingErrors();
        window.SetResourceReference(Window.BackgroundProperty, "App.Brush.WindowBackground");
        PresentationTraceSources.DataBindingSource.Listeners.Add(bindingErrors);
        window.Show();
        Wait(() => !vm.IsLoading && vm.Runs.Count == 5);
        vm.AttentionCommand.Execute(null);
        Wait(() => !vm.IsLoading && vm.IsJobs && vm.OnlyNewProblems);
        vm.ChangePageCommand.Execute(LogPageKind.Jobs);
        Wait(() => !vm.IsLoading && vm.Runs.Count == 5);
        Ensure(!vm.OnlyProblems, "Normal execution navigation must default to all results, including after attention navigation.");
        vm.ChangePageCommand.Execute(LogPageKind.Overview);
        Wait(() => !vm.IsLoading && vm.IsOverview && vm.Runs.Count == 5);
        Capture(window, directory, "overview.png");
        Ensure(vm.HasAttention, "Overview must expose full attention counts.");
        Ensure(Find<Button>(view, "Logs.Export") is not null, "Export must be accessible.");
        vm.OpenRunCommand.Execute(vm.Runs.First(item => item.Run.Id == run.Id));
        Wait(() => vm.IsRunDetail && vm.SelectedStep?.Display.Execution.Step.Id == steps[3].Id && vm.CanOpenStep);
        Capture(window, directory, "run.png");
        Wait(() => vm.HasAttentionProblem && vm.AttentionProblemState.StartsWith("Gesehen", StringComparison.Ordinal));
        vm.ResolveProblemCommand.Execute(null);
        Wait(() => vm.AttentionProblemState.Contains("erledigt", StringComparison.Ordinal));
        vm.ReopenProblemCommand.Execute(null);
        Wait(() => vm.AttentionProblemState.StartsWith("Neu", StringComparison.Ordinal));
        vm.RefreshCommand.Execute(null); Wait(() => !vm.IsLoading && vm.HasAttentionProblem);
        Ensure(vm.AttentionProblemState.StartsWith("Neu", StringComparison.Ordinal), "Background refreshing must not immediately acknowledge an explicitly reopened problem.");
        Ensure(vm.AllSteps.Count == 5 && vm.AllSteps.Last().Display.Execution.Cause is not null, "Unexecuted step must show its causal link.");
        vm.SelectedStep = vm.AllSteps.Last();
        Pump();
        Ensure(vm.HasCause, "Cause action must be available.");
        Ensure(vm.CanOpenStep, "A retained unexecuted step must still navigate to its current definition.");
        vm.OpenCauseCommand.Execute(null);
        Ensure(vm.SelectedStep?.Display.Execution.Step.Id == steps[3].Id, "Cause action must select the exact causing observation.");
        bool opened = false;
        vm.RequestOpenJob += (job, id) => opened = job.Id == jobs.Job.Id && id == steps[3].Id;
        Wait(() => vm.CanOpenStep); vm.OpenStepCommand.Execute(null); Wait(() => opened);
        vm.Activate(false); vm.Activate(true);
        Wait(() => !vm.IsLoading && vm.CanOpenStep);
        var reloads = 0;
        System.ComponentModel.PropertyChangedEventHandler loadingChanged = (_, args) => { if (args.PropertyName == nameof(vm.IsLoading) && vm.IsLoading) reloads++; };
        vm.PropertyChanged += loadingChanged;
        var idle = Stopwatch.StartNew();
        Wait(() => idle.Elapsed > TimeSpan.FromSeconds(2.2));
        vm.PropertyChanged -= loadingChanged;
        Ensure(reloads == 0, "Returning from Open Step must not replay the loading indicator on each timer tick.");
        Ensure(vm.SelectedStep?.Display.Execution.Step.Id == steps[3].Id, "Returning from the editor must preserve step selection.");
        vm.StepProblems = true; Ensure(vm.Steps.Count() == 2, "Problems filter must retain failed and prevented steps.");
        vm.StepProblems = false;
        vm.BackCommand.Execute(null); Wait(() => !vm.IsLoading && vm.IsOverview);
        Ensure((ReadAttentionScope(attention, vm.RunFilter)).Count(problem => problem.State == LogAttentionState.New) == 1, "Viewed run problem must leave only the unobserved summary in attention.");
        var summaryRun = vm.Runs.Single(row => row.Run.Outcome == LogOutcome.WithWarnings);
        vm.OpenRunCommand.Execute(summaryRun); Wait(() => vm.IsRunDetail && vm.HasAttentionSummary);
        Find<Expander>(view, "Logs.AttentionSummary")!.IsExpanded = true; Pump();
        Capture(window, directory, "attention-summary.png");
        var seeSummary = Find<Button>(view, "Logs.SeeSummary")!; seeSummary.Command.Execute(null);
        Wait(() => vm.AttentionSummaryState.StartsWith("Gesehen", StringComparison.Ordinal));
        vm.ReopenSummaryCommand.Execute(null); Wait(() => vm.AttentionSummaryState.StartsWith("Neu", StringComparison.Ordinal));
        vm.BackCommand.Execute(null); Wait(() => !vm.IsLoading && vm.IsOverview && vm.HasAttention);
        vm.MarkAllSeenCommand.Execute(null); Wait(() => !vm.IsLoading && !vm.HasAttention);
        Ensure(vm.Runs.Count == 5, "Acknowledgement must preserve normal history rows.");
        var tab = Find<RadioButton>(view, "Logs.Tab.Automations")!;
        tab.Command.Execute(tab.CommandParameter);
        Wait(() => !vm.IsLoading && vm.AutomationRows.Count == 4);
        vm.SelectedAutomation = vm.AutomationChoices.Single();
        Wait(() => !vm.IsLoading && vm.AutomationRows.Count == 4);
        Capture(window, directory, "automation.png");
        Ensure(vm.RelatedRuns.Single().RunId == instance.Id, "Already-running link must resolve to the correct run.");
        vm.ChangePageCommand.Execute(LogPageKind.Application);
        Wait(() => !vm.IsLoading && vm.Events.Count == 3);
        vm.SelectedEvent = vm.Events.Single(row => row.Event.Context.RunId == run.Id);
        Wait(() => vm.HasRelatedRun);
        Find<Expander>(view, "Logs.Technical")!.IsExpanded = true; Pump();
        var heading = Descendants<TextBlock>(view).Single(item => item.IsVisible && item.Text == vm.Title);
        var applicationTab = Find<RadioButton>(view, "Logs.Tab.Application")!;
        Ensure(heading.TransformToAncestor(view).Transform(new Point()).Y < applicationTab.TransformToAncestor(view).Transform(new Point()).Y,
            "Application heading must precede the shared navigation tabs like the other log pages.");
        Capture(window, directory, "application.png");
        vm.Area = LogArea.Video; Wait(() => !vm.IsLoading && vm.Events.Count == 1);
        Ensure(vm.Events.Single().Event.Category == LogArea.Video, "Area filter must query across all retained sources.");
        vm.Area = null; Wait(() => !vm.IsLoading && vm.Events.Count == 3);
        vm.Live = false;
        var selected = vm.SelectedEvent!.Event.Id;
        repository.Append(new LogEvent { Timestamp = DateTimeOffset.Now, Source = LogSource.Application, Level = ExecutionLogLevel.Warning });
        Wait(() => vm.HasNew);
        Ensure(vm.Events.Count == 3 && vm.SelectedEvent?.Event.Id == selected, "Paused updates must preserve the visible checkpoint and selection.");
        vm.ShowNewCommand.Execute(null); Wait(() => !vm.IsLoading && vm.Events.Count == 4);
        vm.Search = "does-not-exist"; Wait(() => !vm.IsLoading && vm.Events.Count == 0);
        Capture(window, directory, "empty.png");
        vm.EventSource = LogSource.Application;
        LocalizationService.Instance.SetCulture("en-US"); Wait(() => !vm.IsLoading);
        Ensure(vm.EventSource == LogSource.Application && vm.Title == "Application diagnostics", "Changing language must preserve filters and update labels.");
        Ensure(vm.EventSources.First().Label == "All sources", "Filter labels must update with culture.");
        LocalizationService.Instance.SetCulture("de-DE"); vm.EventSource = null;
        vm.Search = ""; Wait(() => !vm.IsLoading && vm.Events.Count == 4);
        window.Width = 1000; window.Height = 720; Pump();
        Capture(window, directory, "compact.png");
        Ensure(view.ActualWidth <= 1000, "Compact view must fit the available viewport.");
        window.Width = 1200; window.Height = 1010;
        ControlzEx.Theming.ThemeManager.Current.ChangeTheme(app, "Light.Blue");
        app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/DesktopAutomationApp;component/Styles/Themes/Light.xaml") });
        Pump(); Capture(window, directory, "light.png");
        var exported = false; vm.ChooseExportPath = () => Task.FromResult<string?>(Path.Combine(temporary.Path, "report.zip"));
        vm.ExportCommand.Execute(null);
        Wait(() => !vm.IsExporting && File.Exists(Path.Combine(temporary.Path, "report.zip")));
        using (var zip = ZipFile.OpenRead(Path.Combine(temporary.Path, "report.zip"))) exported = zip.GetEntry("events.jsonl") is not null;
        Ensure(exported, "Export must produce the real backend report.");
        vm.Page = LogPageKind.Jobs;
        for (var index = 0; index < 55; index++) repository.SaveRun(new LogRun
        {
            Id = Guid.NewGuid(),
            InstanceId = Guid.NewGuid(),
            SourceId = jobs.Job.Id,
            Source = LogSource.Job,
            Name = "Paged execution",
            StartedAt = DateTimeOffset.Now,
            EndedAt = DateTimeOffset.Now,
            Outcome = LogOutcome.Successful
        });
        vm.Search = "Paged execution"; Wait(() => !vm.IsLoading && vm.Runs.Count == 50 && vm.HasMore);
        repository.SaveRun(new LogRun { Id = Guid.NewGuid(), InstanceId = Guid.NewGuid(), SourceId = jobs.Job.Id, Source = LogSource.Job, Name = "Paged execution", StartedAt = DateTimeOffset.Now });
        vm.MoreCommand.Execute(null); Wait(() => !vm.IsLoading && vm.Runs.Count == 55);
        var runExport = Path.Combine(temporary.Path, "pages.zip");
        vm.ChooseExportPath = () => Task.FromResult<string?>(runExport); vm.ExportCommand.Execute(null);
        Wait(() => !vm.IsExporting && File.Exists(runExport));
        using (var zip = ZipFile.OpenRead(runExport))
        using (var reader = new StreamReader(zip.GetEntry("runs.json")!.Open()))
            Ensure(System.Text.Json.JsonSerializer.Deserialize<LogRun[]>(reader.ReadToEnd(), LogRepository.JsonOptions)!.Length == 55, "History export must use the same checkpoint and every matching page.");
        var opening = vm.OpenRunAsync(run.Id); Wait(() => opening.IsCompleted && vm.CanOpenStep);
        jobs.Deleted = true; vm.OpenStepCommand.Execute(null); Wait(() => !vm.CanOpenStep);
        Ensure(vm.NavigationReason == Loc.Get("Logs.Ui.JobDeleted"), "A deleted definition must disable editor navigation with a useful reason.");
        var observedExecution = vm.AllSteps.First().Display.Execution.ExecutionId;
        for (var problemIndex = 0; problemIndex < 2; problemIndex++)
        {
            repository.Append(new LogEvent
            {
                Timestamp = DateTimeOffset.Now,
                Source = LogSource.Job,
                Level = ExecutionLogLevel.Warning,
                Context = new(run.Id, run.InstanceId, StepId: steps[0].Id, StepExecutionId: observedExecution),
                Phase = "Main",
                Iteration = 1,
                ProblemId = Guid.NewGuid(),
                Message = "Additional warning"
            });
            vm.RefreshCommand.Execute(null); Wait(() => !vm.IsLoading && vm.HasNextProblem);
            var nextProblem = Find<Button>(view, "Logs.NextProblem")!; nextProblem.Command.Execute(null);
            Wait(() => vm.SelectedStep?.Display.Execution.Step.Id == steps[0].Id && vm.HasAttentionProblem
                && vm.AttentionProblemState.StartsWith("Gesehen", StringComparison.Ordinal) && !vm.HasNextProblem);
            Ensure(!vm.CanOpenStep, "Deleted definitions must not prevent viewing and acknowledging retained problems.");
        }
        var aggregateRun = repository.SaveRun(new LogRun
        {
            Id = Guid.NewGuid(),
            InstanceId = Guid.NewGuid(),
            SourceId = jobs.Job.Id,
            Source = LogSource.Job,
            Name = "Schnelle Wiederholungen",
            StartedAt = DateTimeOffset.Now,
            Steps = [steps[0]]
        });
        for (var iteration = 1; iteration <= 200; iteration++)
        {
            var context = new LogContext(aggregateRun.Id, aggregateRun.InstanceId, StepId: steps[0].Id, StepExecutionId: Guid.NewGuid());
            repository.Append(new LogEvent { Context = context, Phase = "Main", Iteration = iteration, Code = LogCodes.StepStarted });
            repository.Append(new LogEvent { Context = context, Phase = "Main", Iteration = iteration, Code = LogCodes.StepCompleted, DurationMs = 2 });
        }
        repository.SaveRun(repository.ReadRuns().First(item => item.Id == aggregateRun.Id) with { EndedAt = DateTimeOffset.Now, Outcome = LogOutcome.Successful });
        var aggregateOpening = vm.OpenRunAsync(aggregateRun.Id);
        Wait(() => aggregateOpening.IsCompleted && vm.AllSteps.Count == 1 && vm.DetailSummary.Contains("200", StringComparison.Ordinal));
        Ensure(vm.SelectedStep!.Display.Execution.Summary!.Count == 200, "Repeated successes must render as one summary with the full execution count.");
        Ensure(vm.SelectedStep.Display.Execution.DurationMs == 400, "Summary duration must cover all successful repetitions.");
        Ensure(vm.SelectedStep.Duration.Contains("Ø 2 ms", StringComparison.Ordinal), "Step rows must display the average duration per repetition in milliseconds.");
        Ensure(Descendants<TextBlock>(Find<ListBox>(view, "Logs.Steps")!).Any(text => text.Text.Contains("Ø 2 ms", StringComparison.Ordinal)),
            "The measured average must be visible on the rendered step row.");
        Ensure(vm.DetailSummary.Contains(Loc.Get("Logs.Ui.AggregatedLast"), StringComparison.Ordinal), "Summary details must identify the last observation.");
        Ensure(Find<ListBox>(view, "Logs.Steps")!.Items.Count == 1, "The actual timeline must contain one aggregate row.");
        Capture(window, directory, "aggregate.png");
        foreach (var activeRun in repository.ReadRuns().Where(item => item.EndedAt is null))
            repository.SaveRun(activeRun with { EndedAt = retentionClock.GetUtcNow(), Outcome = LogOutcome.Successful });
        var persisted = repository.FlushAsync();
        Wait(() => persisted.IsCompleted); persisted.GetAwaiter().GetResult();
        vm.Page = LogPageKind.Application;
        vm.EventSource = null; vm.Area = null; vm.Search = ""; vm.Range = 0;
        Wait(() => !vm.IsLoading && vm.Events.Count > 0);
        vm.Live = false;
        var checkpoint = vm.EventFilter.SnapshotSequence;
        foreach (var segment in Directory.GetFiles(temporary.Path, "*.events.jsonl"))
            File.SetLastWriteTimeUtc(segment, retentionClock.GetUtcNow().AddDays(-31).UtcDateTime);
        retentionClock.Advance(TimeSpan.FromHours(1));
        var cleanup = repository.FlushAsync();
        Wait(() => cleanup.IsCompleted && !vm.IsLoading && vm.Events.Count == 0);
        cleanup.GetAwaiter().GetResult();
        Ensure(vm.EventFilter.SnapshotSequence == checkpoint && !vm.Live,
            "Retention must remove expired rows from a paused view without changing its checkpoint or live setting.");
        Ensure(vm.SelectedEvent is null, "Expired selection must not keep deleted event details available.");
        Ensure(bindingErrors.Errors.Count == 0, string.Join("\n", bindingErrors.Errors));
        PresentationTraceSources.DataBindingSource.Listeners.Remove(bindingErrors);
        window.Close(); app.Shutdown();
    }
    private static IReadOnlyList<LogAttentionProblem> ReadAttentionScope(LogAttentionService attention, RunQuery query)
    {
        var task = attention.ScopeAsync(query); Wait(() => task.IsCompleted); return task.GetAwaiter().GetResult();
    }
    private static void Ensure(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Wait(Func<bool> predicate)
    {
        var watch = Stopwatch.StartNew();
        while (!predicate()) { if (watch.Elapsed > TimeSpan.FromSeconds(15)) throw new TimeoutException("Log UI state did not settle."); Pump(); Thread.Sleep(10); }
        Pump();
    }
    private static void Pump() { var frame = new DispatcherFrame(); Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => frame.Continue = false)); Dispatcher.PushFrame(frame); }
    private static void Capture(FrameworkElement view, string directory, string name)
    {
        view.UpdateLayout(); Pump();
        var image = new RenderTargetBitmap((int)view.ActualWidth, (int)view.ActualHeight, 96, 96, PixelFormats.Pbgra32); image.Render(view);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(image));
        using var stream = File.Create(Path.Combine(directory, name)); encoder.Save(stream);
    }
    private static T? Find<T>(DependencyObject root, string id) where T : DependencyObject
    {
        if (root is T result && (root is not UIElement element || element.IsVisible) && AutomationProperties.GetAutomationId(root) == id) return result;
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++) if (Find<T>(VisualTreeHelper.GetChild(root, index), id) is { } found) return found;
        return null;
    }
    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is T result) yield return result;
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
            foreach (var item in Descendants<T>(VisualTreeHelper.GetChild(root, index))) yield return item;
    }
    private sealed class BindingErrors : TraceListener
    {
        public List<string> Errors { get; } = [];
        public override void Write(string? message) { }
        public override void WriteLine(string? message) { if (message?.Contains("Error", StringComparison.Ordinal) == true) Errors.Add(message); }
    }
    private sealed class JobCatalog : IJobApplicationService
    {
        public bool Deleted { get; set; }
        public Job Job { get; } = new() { Name = "Rechnungsablage", Steps = [new FileSystemOperationStep(), new DesktopDuplicationStep(), new DynamicRoiStep(), new SaveImageStep(), new FileSystemOperationStep()] };
        public IReadOnlyDictionary<string, Job> Jobs => Deleted ? new Dictionary<string, Job>() : new Dictionary<string, Job> { [Job.Id.ToString()] = Job };
        public Task ReloadAsync() => Task.CompletedTask;
        public Task<Job> CreateJobAsync(string name) => throw new NotSupportedException();
        public Task SaveJobAsync(Job job) => Task.CompletedTask;
        public Task DeleteJobAsync(Guid id) => Task.CompletedTask;
        public string GetStoragePath() => "";
    }
    private sealed class Automations(AutomationDefinition definition) : IAutomationApplicationService
    {
        public Task<IReadOnlyList<AutomationDefinition>> LoadAllAsync() => Task.FromResult<IReadOnlyList<AutomationDefinition>>([definition]);
        public Task SaveAsync(AutomationDefinition value) => Task.CompletedTask;
        public Task DeleteAsync(Guid id) => Task.CompletedTask;
        public Task TriggerAsync(Guid id) => Task.CompletedTask;
        public string GetStoragePath() => "";
    }
}
