using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using System.Windows.Threading;
using DesktopAutomation.Application.Interfaces;
using DesktopAutomation.Application.Logging;
using DesktopAutomationApp.Localization;
using TaskAutomation.Jobs;
using TaskAutomation.Logging;
using DesktopAutomationApp.Services.Jobs;
using TaskAutomation.Steps.Definitions;
using MahApps.Metro.IconPacks;
using System.Windows.Media;

namespace DesktopAutomationApp.ViewModels;

public enum LogPageKind { Overview, Jobs, Automations, Application }
public interface ILogLocalizedOption { void UpdateCulture(); }
public sealed class LogFilter<T>(T value, string label, bool resource = false) : ViewModelBase, ILogLocalizedOption
{
    public T Value { get; } = value;
    public string Label => resource ? Loc.Get(label) : label;
    public void UpdateCulture() => OnPropertyChanged(nameof(Label));
}

/// <summary>Presentation state over shared v2 queries; selection and paging use stable identities.</summary>
public sealed class LogsHomeViewModel : ViewModelBase
{
    private readonly LogQueryService _queries;
    private readonly LogExportService _exports;
    private readonly LogAttentionService _attentionService;
    private LogAttentionDetail? _attentionDetail;
    private bool _onlyNewProblems;
    private Guid? _lastViewedEventId;
    private bool _rebuildingSelection;
    private Guid? _preferredProblemEventId;
    private readonly IJobApplicationService _jobs;
    private readonly IAutomationApplicationService _automations;
    private readonly ILogRepository _repository;
    private readonly DispatcherTimer _timer;
    private CancellationTokenSource? _load, _export;
    private bool _active, _dirty = true, _loading, _exporting, _onlyProblems, _stepProblems, _live = true;
    private int _page, _range, _total, _available, _attention, _newCount;
    private int? _nextOffset;
    private long _checkpoint = long.MaxValue;
    private long? _before;
    private string _search = "", _status = "";
    private DateTime? _from = DateTime.Today, _until = DateTime.Today;
    private LogOutcome? _outcome;
    private LogSource? _source = LogSource.Job;
    private LogArea? _area;
    private ExecutionLogLevel _level = ExecutionLogLevel.Warning;
    private Guid? _sourceId;
    private LogSource? _eventSource;
    private int _navigationVersion;
    private bool _catalogFrozen, _disposed;
    private AutomationLogChoice? _choice;
    private RunDetails? _details;
    private LogStepRow? _step;
    private LogAutomationRow? _trigger;
    private LogEventRow? _event;
    private LogEventDetails? _eventDetails;
    private LogNavigation? _navigation;
    private Guid? _detailEventId;
    private LogPageKind _returnPage;

    public LogsHomeViewModel(LogQueryService queries, LogExportService exports, ILogRepository repository,
        IJobApplicationService jobs, IAutomationApplicationService automations, LogAttentionService attention)
    {
        _queries = queries; _exports = exports; _attentionService = attention; _repository = repository; _jobs = jobs; _automations = automations;
        BuildOptions();
        ChangePageCommand = new RelayCommand<LogPageKind>(value => { if (value == LogPageKind.Jobs) { OnlyProblems = false; OnlyNewProblems = false; } Page = value; });
        RefreshCommand = new AsyncRelayCommand(() => RefreshAsync());
        MoreCommand = new AsyncRelayCommand(() => RefreshAsync(true), () => HasMore && !IsLoading);
        OpenRunCommand = new RelayCommand<LogRunRow>(row => { if (row is not null) _ = OpenRunAsync(row.Run.Id); });
        OpenLinkCommand = new RelayCommand<LogRunLinkRow>(link => { if (link?.RunId is { } id) _ = OpenRunAsync(id); else Status = Loc.Get("Logs.Ui.RunUnavailable"); });
        BackCommand = new RelayCommand(() => { _details = null; _eventDetails = null; _step = null; _page = (int)_returnPage; NotifyPage(); _ = RefreshAsync(preserveSnapshot: true); });
        AttentionCommand = new RelayCommand(() => { Page = LogPageKind.Jobs; OnlyProblems = false; OnlyNewProblems = true; });
        MarkAllSeenCommand = new AsyncRelayCommand(async () => { var scope = await _attentionService.ScopeAsync(RunFilter); if (!await _attentionService.ChangeAsync(scope.Where(problem => problem.State == LogAttentionState.New), LogAttentionState.Seen)) Status = Loc.Get("Logs.Ui.AttentionSaveFailed"); else await RefreshAsync(); });
        OpenNewProblemCommand = new AsyncRelayCommand(() => OpenProblemAsync(_attentionDetail?.Next));
        ResolveProblemCommand = new AsyncRelayCommand(() => ChangeAttentionAsync(_attentionDetail?.Selected, LogAttentionState.Resolved));
        ReopenProblemCommand = new AsyncRelayCommand(() => ChangeAttentionAsync(_attentionDetail?.Selected, LogAttentionState.New));
        SeeSummaryCommand = new AsyncRelayCommand(() => ChangeAttentionAsync(_attentionDetail?.Summary, LogAttentionState.Seen));
        ResolveSummaryCommand = new AsyncRelayCommand(() => ChangeAttentionAsync(_attentionDetail?.Summary, LogAttentionState.Resolved));
        ReopenSummaryCommand = new AsyncRelayCommand(() => ChangeAttentionAsync(_attentionDetail?.Summary, LogAttentionState.New));
        OpenStepCommand = new AsyncRelayCommand(OpenStepAsync, () => CanOpenStep);
        OpenCauseCommand = new RelayCommand(() => { if (SelectedStep?.Display.Execution.Cause is { } cause) SelectedStep = AllSteps.FirstOrDefault(row => row.Display.Execution.Step.Id == cause.StepId && (!cause.ExecutionId.HasValue || row.Display.Execution.ExecutionId == cause.ExecutionId)); });
        OpenRelatedRunCommand = new RelayCommand(() => { if (_eventDetails?.Run?.RunId is { } id) _ = OpenRunAsync(id, _eventDetails.Event.Context.StepExecutionId, _eventDetails.Event.Context.StepId, problemEventId: _eventDetails.Event.Id); });
        OpenJobCommand = new RelayCommand(() => { if (_details is not null && _jobs.Jobs.Values.FirstOrDefault(job => job.Id == _details.Run.SourceId) is { } job) RequestOpenJob?.Invoke(job, null); });
        OpenAutomationCommand = new RelayCommand(() => { if (CanOpenAutomation && AutomationDefinitionId is { } id) RequestOpenAutomation?.Invoke(id); });
        OpenOriginCommand = new AsyncRelayCommand(OpenOriginAsync);
        CopyCommand = new RelayCommand(() => RequestCopy?.Invoke(TechnicalText));
        ExportCommand = new AsyncRelayCommand(ChooseExportAsync, () => !IsExporting && !IsLoading);
        CancelExportCommand = new RelayCommand(() => _export?.Cancel());
        ShowNewCommand = new RelayCommand(() => { Live = true; _ = RefreshAsync(); });
        CheckPathCommand = new RelayCommand<LogAction>(action => { if (action?.Path is { } path) RequestCheckPath?.Invoke(path); });
        ClearAutomationCommand = new RelayCommand(() => SelectedAutomation = null);
        OpenGroupCommand = new AsyncRelayCommand<LogEventGroup>(async group => { if (group is null) return; var detail = await _queries.EventDetailsAsync(group.EventIds[0]); if (detail is not null) { Live = false; var row = Events.FirstOrDefault(item => item.Event.Id == detail.Event.Id); if (row is null) { row = new(detail.Event); Events.Add(row); } SelectedEvent = row; } });
        _timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += OnTick;
        attention.Changed += OnAttentionChanged;
        repository.EntryWritten += OnEntry;
        repository.RunChanged += OnRun;
        repository.StorageChanged += OnStorageChanged;
        LocalizationService.Instance.CultureChanged += OnCultureChanged;
    }

    private void BuildOptions()
    {
        Tabs = Enum.GetValues<LogPageKind>().Select(value => new LogFilter<LogPageKind>(value, $"Logs.Ui.Tab.{value}", true)).ToArray();
        Outcomes = new[] { new LogFilter<LogOutcome?>(null, "Logs.Ui.AllResults", true) }.Concat(Enum.GetValues<LogOutcome>().Select(value => new LogFilter<LogOutcome?>(value, $"Logs.Ui.Outcome.{value}", true))).ToArray();
        Sources = [new(LogSource.Job, "Ui.Logs.JobLogs", true), new(LogSource.Makro, "Logs.Ui.Macros", true), new(null, "Logs.Ui.AllRuns", true)];
        Ranges = new[] { "Today", "Week", "Month", "All", "Custom" }.Select((key, index) => new LogFilter<int>(index, $"Logs.Ui.Range.{key}", true)).ToArray();
        Areas = new[] { new LogFilter<LogArea?>(null, "Logs.Ui.AllAreas", true) }.Concat(Enum.GetValues<LogArea>().Select(value => new LogFilter<LogArea?>(value, $"Log.Area.{value}", true))).ToArray();
        Levels = Enum.GetValues<ExecutionLogLevel>().Select(value => new LogFilter<ExecutionLogLevel>(value, $"Logs.Ui.Level.{value}", true)).ToArray();
        EventSources = new[] { new LogFilter<LogSource?>(null, "Logs.Ui.AllSources", true) }.Concat(Enum.GetValues<LogSource>().Select(value => new LogFilter<LogSource?>(value, $"Logs.Ui.Source.{value}", true))).ToArray();
    }

    public event Action<Job, string?>? RequestOpenJob;
    public event Action<Guid>? RequestOpenAutomation;
    public event Action<string>? RequestCopy;
    public event Action<string>? RequestCheckPath;
    public Func<Task<string?>>? ChooseExportPath { get; set; }
    public IReadOnlyList<LogFilter<LogPageKind>> Tabs { get; private set; } = [];
    public IReadOnlyList<LogFilter<LogOutcome?>> Outcomes { get; private set; } = [];
    public IReadOnlyList<LogFilter<LogSource?>> Sources { get; private set; } = [];
    public IReadOnlyList<LogFilter<int>> Ranges { get; private set; } = [];
    public IReadOnlyList<LogFilter<LogArea?>> Areas { get; private set; } = [];
    public IReadOnlyList<LogFilter<ExecutionLogLevel>> Levels { get; private set; } = [];
    public ObservableCollection<LogFilter<Guid?>> RunSources { get; } = [];
    public ObservableCollection<AutomationLogChoice> AutomationChoices { get; } = [];
    public ObservableCollection<LogRunRow> Runs { get; } = [];
    public ObservableCollection<LogAutomationRow> AutomationRows { get; } = [];
    public ObservableCollection<LogEventRow> Events { get; } = [];
    public ObservableCollection<LogEventGroup> Groups { get; } = [];
    public ObservableCollection<LogStepRow> AllSteps { get; } = [];
    public IEnumerable<LogStepRow> Steps => StepProblems ? AllSteps.Where(row => row.Display.Execution.Outcome is StepLogOutcome.Failed or StepLogOutcome.Warning or StepLogOutcome.Cancelled or StepLogOutcome.Unknown or StepLogOutcome.NotExecuted) : AllSteps;
    public IEnumerable<LogRunLinkRow> RelatedRuns => SelectedTrigger is null ? [] : SelectedTrigger.Item.StartedRuns.Concat(SelectedTrigger.Item.RelatedRuns).Select(link => new LogRunLinkRow(link));
    public IEnumerable<LogAction> PathActions => _eventDetails is null ? [] : LogDiagnostics.PathActions(_eventDetails.Event);
    public IReadOnlyList<LogFilter<LogSource?>> EventSources { get; private set; } = [];
    public ICommand MarkAllSeenCommand { get; }
    public ICommand OpenNewProblemCommand { get; }
    public bool HasNextProblem => _attentionDetail?.Next is not null;
    public ICommand ResolveProblemCommand { get; }
    public ICommand ReopenProblemCommand { get; }
    public ICommand SeeSummaryCommand { get; }
    public ICommand ResolveSummaryCommand { get; }
    public ICommand ReopenSummaryCommand { get; }
    public bool HasAttentionProblem => _attentionDetail?.Selected is not null;
    public bool HasAttentionSummary => IsRunDetail && _attentionDetail?.Summary is not null;
    public string AttentionProblemState => _attentionDetail?.Selected is { } problem ? Loc.Get($"Logs.Ui.AttentionState.{problem.State}") : "";
    public string AttentionSummaryState => _attentionDetail?.Summary is { } problem ? Loc.Get($"Logs.Ui.AttentionState.{problem.State}") : "";
    public ICommand ChangePageCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand MoreCommand { get; }
    public ICommand OpenRunCommand { get; }
    public ICommand OpenLinkCommand { get; }
    public ICommand BackCommand { get; }
    public ICommand AttentionCommand { get; }
    public ICommand OpenStepCommand { get; }
    public ICommand OpenCauseCommand { get; }
    public ICommand OpenRelatedRunCommand { get; }
    public ICommand OpenJobCommand { get; }
    public ICommand OpenAutomationCommand { get; }
    public ICommand OpenOriginCommand { get; }
    public ICommand CopyCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand CancelExportCommand { get; }
    public ICommand ShowNewCommand { get; }
    public ICommand CheckPathCommand { get; }
    public ICommand ClearAutomationCommand { get; }
    public ICommand OpenGroupCommand { get; }
    public LogPageKind Page { get => (LogPageKind)_page; set { if (Page == value && !IsRunDetail) return; _navigationVersion++; _lastViewedEventId = null; _detailEventId = null; _details = null; _eventDetails = null; _navigation = null; _step = null; _page = (int)value; NotifyPage(); Reset(); } }
    public bool IsOverview => !IsRunDetail && Page == LogPageKind.Overview;
    public bool IsJobs => !IsRunDetail && Page == LogPageKind.Jobs;
    public bool IsAutomation => !IsRunDetail && Page == LogPageKind.Automations;
    public bool IsApplication => !IsRunDetail && Page == LogPageKind.Application;
    public bool IsRunList => IsOverview || IsJobs;
    public bool ShowMainFilters => !IsRunDetail && !IsOverview;
    public bool IsRunDetail => _details is not null;
    public bool HasMore => _nextOffset.HasValue || _before.HasValue;
    public bool IsLoading { get => _loading; private set { SetProperty(ref _loading, value); RaiseCommands(); } }
    public bool IsExporting { get => _exporting; private set { SetProperty(ref _exporting, value); RaiseCommands(); } }
    public string Status { get => _status; private set { SetProperty(ref _status, value); OnPropertyChanged(nameof(HasStatus)); } }
    public bool HasStatus => !string.IsNullOrEmpty(Status);
    public string Title => IsRunDetail ? _details!.Run.Name : Loc.Get($"Logs.Ui.Title.{Page}");
    public string Subtitle => IsRunDetail ? new LogRunRow(_details!.Run).Header : Loc.Get($"Logs.Ui.Subtitle.{Page}");
    public string RunNumber => _details is null ? "" : "#" + _details.Run.ExecutionNumber;
    public string RunOutcome => _details is null ? "" : LogUiText.Outcome(_details.Run.Outcome);
    public string RunTone => _details is null ? "Muted" : LogUiText.Tone(_details.Run.Outcome.ToString());
    public string CountText => Loc.Format(IsApplication ? "Logs.Ui.EventCounts" : "Logs.Ui.Counts", IsApplication ? Events.Count : IsAutomation ? AutomationRows.Count : Runs.Count, _total, _available);
    public string AttentionText => Loc.Format("Logs.Ui.Attention", _attention);
    public bool HasAttention => _attention > 0;
    public string AttentionSummary { get; private set; } = "";
    public string StepCounts => _details?.StepCounts is { } counts ? string.Join("  ·  ", counts.Select(pair => $"{pair.Value} {Loc.Get($"Logs.Ui.StepOutcome.{pair.Key}")}"))
        + (_details.Run.CompletedIterations > 0 ? "  ·  " + Loc.Format("Logs.Ui.CompletedIterations", _details.Run.CompletedIterations) : "") : "";
    public string Search { get => _search; set { if (_search == value) return; SetProperty(ref _search, value); Reset(); } }
    public int Range { get => _range; set { if (_range == value) return; SetProperty(ref _range, value); OnPropertyChanged(nameof(CustomRange)); Reset(); } }
    public bool CustomRange => Range == 4;
    public DateTime? FromDate { get => _from; set { SetProperty(ref _from, value); Reset(); } }
    public DateTime? UntilDate { get => _until; set { SetProperty(ref _until, value); Reset(); } }
    public LogOutcome? Outcome { get => _outcome; set { SetProperty(ref _outcome, value); Reset(); } }
    public LogSource? Source { get => _source; set { SetProperty(ref _source, value); Reset(); } }
    public Guid? SourceId { get => _sourceId; set { SetProperty(ref _sourceId, value); Reset(); } }
    public LogArea? Area { get => _area; set { SetProperty(ref _area, value); Reset(); } }
    public LogSource? EventSource { get => _eventSource; set { SetProperty(ref _eventSource, value); Reset(); } }
    public ExecutionLogLevel Level { get => _level; set { SetProperty(ref _level, value); Reset(); } }
    public bool OnlyProblems { get => _onlyProblems; set { SetProperty(ref _onlyProblems, value); Reset(); } }
    public bool OnlyNewProblems { get => _onlyNewProblems; set { SetProperty(ref _onlyNewProblems, value); Reset(); } }
    public bool StepProblems { get => _stepProblems; set { SetProperty(ref _stepProblems, value); OnPropertyChanged(nameof(Steps)); } }
    public bool Live { get => _live; set { SetProperty(ref _live, value); OnPropertyChanged(nameof(LiveText)); if (value) _dirty = true; } }
    public string LiveText => Loc.Get(Live ? "Logs.Ui.Live" : "Logs.Ui.Paused");
    public string NewText => Loc.Format("Logs.Ui.New", _newCount);
    public bool HasNew => _newCount > 0;
    public AutomationLogChoice? SelectedAutomation { get => _choice; set { SetProperty(ref _choice, value); OnPropertyChanged(nameof(AutomationContext)); Reset(); } }
    public string AutomationContext => _choice is null ? Loc.Get("Logs.Ui.AllAutomations") : Loc.Format("Logs.Ui.AutomationContext", _choice.WatchedDirectory ?? "—", _choice.TargetName ?? "—") + (_choice.DefinitionExists ? "" : " · " + Loc.Get("Logs.Ui.Deleted"));
    public LogStepRow? SelectedStep { get => _step; set { if (!_rebuildingSelection && (_step?.Display.Execution.ExecutionId != value?.Display.Execution.ExecutionId || _step?.Display.Execution.Step.Id != value?.Display.Execution.Step.Id)) { _lastViewedEventId = null; _preferredProblemEventId = null; } SetProperty(ref _step, value); if (IsRunDetail) _ = LoadStepDetailsAsync(); } }
    public LogAutomationRow? SelectedTrigger { get => _trigger; set { SetProperty(ref _trigger, value); _eventDetails = null; _detailEventId = null; NotifyDetails(); } }
    public LogEventRow? SelectedEvent { get => _event; set { if (!_rebuildingSelection && _event?.Event.Id != value?.Event.Id) _lastViewedEventId = null; SetProperty(ref _event, value); if (IsApplication) _ = LoadEventDetailsAsync(value?.Event.Id); } }
    public string DetailTitle => IsAutomation ? SelectedTrigger?.Title ?? Loc.Get("Logs.Ui.SelectEvent") : IsRunDetail ? SelectedStep?.Title ?? Loc.Get("Logs.Ui.SelectStep") : SelectedEvent?.Title ?? Loc.Get("Logs.Ui.SelectEvent");
    public string DetailSummary => IsAutomation ? SelectedTrigger is null ? "" : LogUiText.Text(LogQueryService.AutomationSummary(SelectedTrigger.Item))
        : IsRunDetail && SelectedStep?.Display.Execution.Summary is not null ? SelectedStep.Summary + " " + Loc.Get("Logs.Ui.AggregatedLast")
            + (_eventDetails is not null ? " " + LogUiText.EventSummary(_eventDetails.Event) : "")
        : _eventDetails is not null ? LogUiText.EventSummary(_eventDetails.Event) : SelectedStep?.Summary ?? "";
    public string DetailTone => IsAutomation ? SelectedTrigger?.Tone ?? "Muted" : IsRunDetail ? SelectedStep?.Tone ?? "Muted" : SelectedEvent?.Tone ?? "Muted";
    public string Impact => SelectedStep?.Impact ?? (IsAutomation ? SelectedTrigger?.Impact ?? "" : "");
    public string Help => _eventDetails?.Display.Diagnostic is { } diagnostic ? Loc.Get(diagnostic.HelpKey) : "";
    public bool CanCopy => !string.IsNullOrWhiteSpace(TechnicalText);
    public bool ShowHelp => HasHelp && !IsApplication;
    public bool HasHelp => !string.IsNullOrEmpty(Help);
    public bool HasImpact => !string.IsNullOrEmpty(Impact);
    public bool HasCause => SelectedStep?.Display.Execution.Cause is not null;
    public bool CanOpenStep => _navigation?.CanOpenStep == true;
    public string NavigationReason => _navigation?.UnavailableReason is { } reason ? Loc.Get($"Logs.Ui.{reason}") : _navigation?.DefinitionChanged == true ? Loc.Get("Logs.Ui.DefinitionChanged") : "";
    public bool HasNavigationReason => !string.IsNullOrEmpty(NavigationReason);
    public bool HasRelatedRun => _eventDetails?.Run?.RunId is not null;
    public bool ShowRelatedRun => HasRelatedRun && !IsRunDetail;
    public bool CanOpenJob => _details is not null && _jobs.Jobs.Values.Any(job => job.Id == _details.Run.SourceId);
    private Guid? AutomationDefinitionId => SelectedTrigger?.Item.AutomationId ?? SelectedAutomation?.Id;
    public bool CanOpenAutomation => AutomationChoices.Any(choice => choice.Id == AutomationDefinitionId && choice.DefinitionExists);
    public bool CanOpenOrigin => _details?.Run.Context.ParentRunId is not null || _details?.Run.Context.AutomationId is not null;
    public string RelatedRunText => _eventDetails?.Run is { } run ? $"{run.Name}  ·  #{run.ExecutionNumber}" + (_eventDetails.StepPosition is { } position ? "  ·  " + Loc.Format("Logs.Ui.StepPosition", position) : "") : "";
    public string TechnicalText => IsAutomation && SelectedTrigger is not null ? string.Join("\n\n", SelectedTrigger.Item.Events.Select(LogUiText.Technical)) : _eventDetails is not null ? LogUiText.Technical(_eventDetails.Event) : SelectedStep is not null ? string.Join("\n\n", SelectedStep.Display.Execution.Events.Select(LogUiText.Technical)) : "";
    public string TriggerInfo => _details?.Run.Trigger is { } trigger ? Loc.Format("Logs.Ui.AutomationContext", trigger.WatchedDirectory ?? "—", trigger.TargetName ?? "—") + " · " + trigger.Kind + " / " + trigger.EventKind : "";
    public string TimelineDate => _details?.Run.StartedAt.LocalDateTime.ToString("D", CultureInfo.CurrentCulture) ?? "";
    public string AutomationDate => Range == 0 ? Loc.Format("Logs.Ui.TodayDate", DateTime.Today.ToString("d. MMMM", CultureInfo.CurrentCulture)) : Loc.Get("Logs.Ui.Events");
    public string DetailSeverity => SelectedEvent?.Level ?? "";
    public string Eligibility => SelectedTrigger?.Item.EligibleAt is { } date ? Loc.Format("Logs.Ui.Eligible", date.LocalDateTime.ToString("g", CultureInfo.CurrentCulture)) : "";
    public bool HasEligibility => IsAutomation && SelectedTrigger?.Item.EligibleAt is not null;
    public string DiagnosticCode => _eventDetails?.Display.Diagnostic?.Code ?? "";
    public string DiagnosticTitle => _eventDetails is null ? IsAutomation ? SelectedTrigger?.Title ?? "" : SelectedStep?.Result ?? "" : LogUiText.Text(_eventDetails.Display.Title);


    public void CopySelection(IReadOnlyList<object> rows)
    {
        var events = ContextEvents(rows).DistinctBy(entry => entry.Id).Select(_repository.Privacy.Sanitize);
        var text = string.Join("\n\n", events.Select(LogUiText.Technical));
        if (text.Length == 0)
            text = string.Join("\n", rows.OfType<LogRunRow>().Select(row => row.Name + " " + row.Number + " · " + row.Result));
        RequestCopy?.Invoke(text);
    }

    public async Task OpenContextStepAsync(LogStepRow row)
    {
        var runId = _details?.Run.Id;
        if (runId is null) return;
        var stepId = row.Display.Execution.Step.Id;
        await _jobs.ReloadAsync();
        if (_disposed) return;
        var navigation = _queries.Navigation(runId.Value, stepId, _jobs.Jobs.Values);
        if (navigation.CanOpenStep && _jobs.Jobs.Values.FirstOrDefault(job => job.Id == navigation.JobId) is { } job)
            RequestOpenJob?.Invoke(job, navigation.StepId);
    }

    public void OpenContextCause(LogStepRow row)
    {
        if (row.Display.Execution.Cause is not { } cause) return;
        SelectedStep = AllSteps.FirstOrDefault(item => item.Display.Execution.Step.Id == cause.StepId
            && (!cause.ExecutionId.HasValue || item.Display.Execution.ExecutionId == cause.ExecutionId));
    }

    public async Task OpenContextRunAsync(object row)
    {
        if (row is LogEventRow eventRow)
        {
            var details = await _queries.EventDetailsAsync(eventRow.Event.Id);
            if (details?.Run?.RunId is { } id)
                await OpenRunAsync(id, eventRow.Event.Context.StepExecutionId, eventRow.Event.Context.StepId, problemEventId: eventRow.Event.Id);
            else Status = Loc.Get("Logs.Ui.RunUnavailable");
        }
        else if (row is LogAutomationRow trigger && trigger.Item.StartedRuns.FirstOrDefault(link => link.RunId.HasValue)?.RunId is { } id)
            await OpenRunAsync(id);
    }

    private static IEnumerable<LogEvent> ContextEvents(IEnumerable<object> rows) => rows.SelectMany(row => row switch
    {
        LogEventRow entry => new[] { entry.Event },
        LogStepRow step => step.Display.Execution.Events,
        LogAutomationRow trigger => trigger.Item.Events,
        _ => []
    });

    public async Task ExportSelectionAsync(IReadOnlyList<object> rows)
    {
        if (IsExporting || ChooseExportPath is null || rows.Count == 0) return;
        var runs = rows.OfType<LogRunRow>().Select(row => row.Run.Id).Distinct().ToArray();
        var events = ContextEvents(rows).Select(entry => entry.Id).Distinct().ToArray();
        var selection = new LogExportSelection(runs, events, Math.Min(_checkpoint, _repository.SnapshotSequence));
        var destination = await ChooseExportPath();
        if (destination is null) return;
        _export?.Dispose(); _export = new(); IsExporting = true;
        try
        {
            var result = await _exports.ExportSelectionAsync(selection, destination,
                typeof(LogsHomeViewModel).Assembly.GetName().Version?.ToString() ?? "", _export.Token);
            Status = Loc.Get(result.IsComplete ? "Logs.Ui.Exported" : "Logs.Ui.ExportPartial");
        }
        catch (OperationCanceledException) { Status = Loc.Get("Logs.Ui.ExportCancelled"); }
        catch (Exception error) when (error is System.IO.IOException or UnauthorizedAccessException) { Status = Loc.Get("Logs.Ui.ExportFailed"); }
        finally { IsExporting = false; }
    }

    public async Task ChangeSelectionAttentionAsync(IReadOnlyList<object> rows, LogAttentionState state)
    {
        var runIds = rows.OfType<LogRunRow>().Select(row => row.Run.Id).ToHashSet();
        var eventIds = ContextEvents(rows).Select(entry => entry.Id).ToHashSet();
        var problems = await _attentionService.ScopeAsync(new RunQuery(SnapshotSequence: Math.Min(_checkpoint, _repository.SnapshotSequence)));
        var selected = problems.Where(problem => runIds.Contains(problem.RunId) || problem.EventId is { } id && eventIds.Contains(id)).ToArray();
        if (!await _attentionService.ChangeAsync(selected, state)) Status = Loc.Get("Logs.Ui.AttentionSaveFailed");
        else await RefreshAsync(preserveSnapshot: true);
    }

    public void Activate(bool active) { _active = active; if (active) { _lastViewedEventId = null; _timer.Start(); _ = RefreshAsync(); } else { _navigationVersion++; _detailEventId = null; _timer.Stop(); _load?.Cancel(); } }
    private void Reset() { _catalogFrozen = false; _checkpoint = long.MaxValue; _nextOffset = null; _before = null; _dirty = true; if (_active && !_disposed) _ = RefreshAsync(); }
    private (DateTimeOffset? From, DateTimeOffset? Until) Dates() => Range switch
    {
        0 => (new DateTimeOffset(DateTime.Today), new DateTimeOffset(DateTime.Today.AddDays(1))),
        1 => (new DateTimeOffset(DateTime.Today.AddDays(-6)), new DateTimeOffset(DateTime.Today.AddDays(1))),
        2 => (new DateTimeOffset(DateTime.Today.AddDays(-29)), new DateTimeOffset(DateTime.Today.AddDays(1))),
        4 => (FromDate.HasValue ? new DateTimeOffset(FromDate.Value.Date) : null, UntilDate.HasValue ? new DateTimeOffset(UntilDate.Value.Date.AddDays(1)) : null),
        _ => (null, null)
    };
    public RunQuery RunFilter => new(SourceId: SourceId, Outcome: Outcome, From: Dates().From, Until: Dates().Until, Search: Search, SnapshotSequence: _checkpoint, Source: Source, OnlyProblems: OnlyProblems, OnlyNewProblems: OnlyNewProblems);
    public LogQuery EventFilter => new(Source: EventSource, From: Dates().From, Until: Dates().Until, MinimumLevel: Level, Search: Search, Category: Area, SnapshotSequence: _checkpoint);

    public async Task RefreshAsync(bool more = false, bool preserveSnapshot = false)
    {
        if (_disposed) return;
        Interlocked.Exchange(ref _pendingRefresh, 0);
        if (more) { _catalogFrozen = true; if (IsApplication) Live = false; }
        else if (!preserveSnapshot) _catalogFrozen = false;
        _load?.Cancel(); _load?.Dispose(); _load = new(); var ct = _load.Token;
        IsLoading = true; Status = "";
        try
        {
            if (CustomRange && FromDate > UntilDate) { Status = Loc.Get("Logs.Ui.InvalidRange"); return; }
            if (IsRunDetail) { _dirty = false; await OpenRunAsync(_details!.Run.Id, SelectedStep?.Display.Execution.ExecutionId, SelectedStep?.Display.Execution.Step.Id, true); return; }
            if (!more && !preserveSnapshot) _checkpoint = long.MaxValue;
            if (IsRunList)
            {
                var page = await _queries.RunsAsync(RunFilter with { Offset = more ? _nextOffset ?? 0 : 0, PageSize = IsOverview ? 5 : 50 }, ct);
                ct.ThrowIfCancellationRequested(); _checkpoint = page.SnapshotSequence; _nextOffset = page.NextOffset; _before = null; _total = page.Total;
                if (!more) Runs.Clear(); foreach (var run in page.Runs) if (!Runs.Any(row => row.Run.Id == run.Id)) Runs.Add(new(run));
                if (IsOverview)
                {
                    var overview = await _queries.OverviewAsync(RunFilter, ct); _attention = overview.NewProblemRunCount;
                    AttentionSummary = overview.NewProblemRuns?.FirstOrDefault() is { } problem ? problem.Name + " · " + LogUiText.Outcome(problem.Outcome) : "";
                    var history = await _queries.AutomationHistoryAsync(new(From: Dates().From, Until: Dates().Until, PageSize: 2, SnapshotSequence: _checkpoint), ct);
                    ct.ThrowIfCancellationRequested(); AutomationRows.Clear(); foreach (var item in history.Items) AutomationRows.Add(new(item));
                    OnPropertyChanged(nameof(AttentionSummary)); OnPropertyChanged(nameof(AttentionText)); OnPropertyChanged(nameof(HasAttention));
                }
                if (RunSources.Count == 0) { RunSources.Add(new(null, "Logs.Ui.AllJobs", true)); foreach (var group in _repository.ReadRuns().GroupBy(run => run.SourceId).OrderBy(group => group.First().Name)) RunSources.Add(new(group.Key, group.First().Name)); }
            }
            else if (IsAutomation)
            {
                if (AutomationChoices.Count == 0)
                {
                    var definitions = await _automations.LoadAllAsync(); ct.ThrowIfCancellationRequested();
                    var choices = await _queries.AutomationChoicesAsync(definitions, ct); ct.ThrowIfCancellationRequested();
                    foreach (var choice in choices) AutomationChoices.Add(choice);
                }
                var page = await _queries.AutomationHistoryAsync(new(SelectedAutomation?.Id, Dates().From, Dates().Until, Search, more ? _nextOffset ?? 0 : 0, SnapshotSequence: _checkpoint), ct);
                ct.ThrowIfCancellationRequested(); _checkpoint = page.SnapshotSequence; _total = page.Total; _nextOffset = page.NextOffset; _before = null;
                var id = SelectedTrigger?.Item.TriggerId; if (!more) AutomationRows.Clear();
                foreach (var item in page.Items) if (!AutomationRows.Any(row => row.Item.TriggerId == item.TriggerId)) AutomationRows.Add(new(item));
                SelectedTrigger = AutomationRows.FirstOrDefault(row => row.Item.TriggerId == id) ?? AutomationRows.FirstOrDefault();
                ReadStatus(page.State, page.Issues);
            }
            else
            {
                var id = SelectedEvent?.Event.Id;
                var page = await _queries.SearchAsync(EventFilter with { BeforeSequence = more ? _before ?? long.MaxValue : long.MaxValue }, ct);
                ct.ThrowIfCancellationRequested(); _checkpoint = page.SnapshotSequence; _total = page.MatchCount; _available = page.AvailableCount; _before = page.NextBeforeSequence; _nextOffset = null;
                _rebuildingSelection = true;
                try
                {
                    if (!more) Events.Clear(); foreach (var entry in page.Entries) if (!Events.Any(row => row.Event.Id == entry.Id)) Events.Add(new(entry));
                    SelectedEvent = Events.FirstOrDefault(row => row.Event.Id == id) ?? Events.FirstOrDefault();
                }
                finally { _rebuildingSelection = false; }
                var groups = await _queries.GroupsAsync(EventFilter, ct); ct.ThrowIfCancellationRequested();
                Groups.Clear(); foreach (var group in groups.Where(group => group.Count > 1)) Groups.Add(group);
                ReadStatus(page.State, page.Issues);
            }
            if (_attentionService.ReadFailed && string.IsNullOrEmpty(Status)) Status = Loc.Get("Logs.Ui.AttentionLoadFailed");
            if (_attentionService.CleanupFailed) Status = Loc.Get("Logs.Ui.AttentionCleanupFailed");
            _dirty = false; _newCount = 0; OnPropertyChanged(nameof(CountText)); OnPropertyChanged(nameof(HasMore)); OnPropertyChanged(nameof(NewText)); OnPropertyChanged(nameof(HasNew));
        }
        catch (OperationCanceledException) { }
        catch (Exception error) when (error is System.IO.IOException or UnauthorizedAccessException or InvalidOperationException) { Status = Loc.Get("Logs.Ui.LoadFailed"); }
        finally { if (!ct.IsCancellationRequested) IsLoading = false; }
    }

    public async Task OpenRunAsync(Guid id, Guid? executionId = null, string? stepId = null, bool refreshing = false, Guid? problemEventId = null)
    {
        var version = ++_navigationVersion;
        var details = await _queries.RunAsync(id);
        if (version != _navigationVersion || _disposed) return;
        if (details is null) { Status = Loc.Get("Logs.Ui.RunUnavailable"); return; }
        if (!IsRunDetail) _returnPage = Page;
        var next = !refreshing && !executionId.HasValue && stepId is null && !problemEventId.HasValue ? await _attentionService.NextNewAsync(id) : null;
        if (version != _navigationVersion || _disposed) return;
        _details = details;
        if (!refreshing) _lastViewedEventId = null;
        _rebuildingSelection = true;
        try
        {
            AllSteps.Clear();
            foreach (var step in details.DisplaySteps ?? []) AllSteps.Add(new(step));
            if (!refreshing) _preferredProblemEventId = problemEventId ?? next?.EventId;
            SelectedStep = AllSteps.FirstOrDefault(row => _preferredProblemEventId is { } eventId && row.Display.Execution.Events.Any(entry => entry.Id == eventId))
                ?? AllSteps.FirstOrDefault(row => executionId.HasValue && row.Display.Execution.ExecutionId == executionId)
                ?? AllSteps.FirstOrDefault(row => stepId is not null && row.Display.Execution.Step.Id == stepId)
                ?? AllSteps.FirstOrDefault(row => row.Display.Execution.Events.Any(entry => entry.Id == details.PrimaryProblem?.Id))
                ?? AllSteps.FirstOrDefault(row => row.Display.Execution.Outcome == StepLogOutcome.Failed) ?? AllSteps.FirstOrDefault();
        }
        finally { _rebuildingSelection = false; }
        NotifyPage(); OnPropertyChanged(nameof(Steps)); OnPropertyChanged(nameof(StepCounts)); ReadStatus(details.State, details.Issues);
        if (!refreshing) IsLoading = false;
    }
    private async Task LoadStepDetailsAsync()
    {
        _navigation = null; _eventDetails = null;
        var observation = SelectedStep?.Display.Execution;
        var entry = observation?.Events.FirstOrDefault(item => item.Id == _preferredProblemEventId) ?? observation?.Events.Where(item => item.Level >= ExecutionLogLevel.Warning).OrderByDescending(item => item.Level).ThenByDescending(item => item.DiagnosticCode is not null).FirstOrDefault() ?? observation?.Events.LastOrDefault();
        await LoadEventDetailsAsync(entry?.Id);
        if (entry is null && observation is not null && _details is not null) { _navigation = _queries.Navigation(_details.Run.Id, observation.Step.Id, _jobs.Jobs.Values); NotifyDetails(); }
    }
    private async Task LoadEventDetailsAsync(Guid? id)
    {
        _detailEventId = id; _eventDetails = null; _navigation = null; _attentionDetail = null; NotifyDetails();
        if (id is null) { await LoadAttentionAsync(null); return; }
        var details = await _queries.EventDetailsAsync(id.Value);
        if (_detailEventId != id || _disposed) return;
        _eventDetails = details;
        if (details is not null && _active && (IsRunDetail || IsApplication))
        {
            if (_lastViewedEventId != details.Event.Id)
            {
                if (!await _attentionService.MarkSeenAsync(details.Event)) Status = Loc.Get("Logs.Ui.AttentionSaveFailed");
                else _lastViewedEventId = details.Event.Id;
            }
            if (_detailEventId != id || _disposed) return;
            await LoadAttentionAsync(details.Event);
            if (_detailEventId != id || _disposed) return;
        }
        if (details is not null && (details.Event.Context.RunId.HasValue || details.Event.Context.InstanceId.HasValue)) _navigation = _queries.Navigation(details.Event, _jobs.Jobs.Values);
        NotifyDetails();
    }
    private async Task LoadAttentionAsync(LogEvent? entry)
    {
        var version = _navigationVersion; var eventId = _detailEventId;
        var detail = await _attentionService.DetailAsync(entry, IsRunDetail ? _details?.Run.Id : _eventDetails?.Run?.RunId);
        if (version != _navigationVersion || eventId != _detailEventId || _disposed) return;
        _attentionDetail = detail; NotifyDetails();
        if (_attentionService.ReadFailed) Status = Loc.Get("Logs.Ui.AttentionLoadFailed");
    }
    private async Task ChangeAttentionAsync(LogAttentionProblem? problem, LogAttentionState state)
    {
        if (problem is null) return;
        if (!await _attentionService.ChangeAsync([problem], state)) { Status = Loc.Get("Logs.Ui.AttentionSaveFailed"); return; }
        await LoadAttentionAsync(_eventDetails?.Event);
    }
    private async Task OpenProblemAsync(LogAttentionProblem? problem)
    {
        if (problem?.EventId is not { } id) return;
        var step = IsRunDetail ? AllSteps.FirstOrDefault(row => row.Display.Execution.Events.Any(entry => entry.Id == id)) : null;
        if (step is not null)
        {
            _preferredProblemEventId = id; _lastViewedEventId = null; _rebuildingSelection = true;
            try { SelectedStep = step; } finally { _rebuildingSelection = false; }
            return;
        }
        var version = _navigationVersion;
        var detail = await _queries.EventDetailsAsync(id);
        if (_disposed || version != _navigationVersion) return;
        if (detail is null) { Status = Loc.Get("Logs.Ui.RunUnavailable"); return; }
        Page = LogPageKind.Application; var destination = _navigationVersion; await RefreshAsync();
        if (_disposed || destination != _navigationVersion) return;
        var item = Events.FirstOrDefault(row => row.Event.Id == id);
        if (item is null) { item = new(detail.Event); Events.Add(item); }
        _lastViewedEventId = null; SelectedEvent = item;
    }
    private void OnAttentionChanged(object? sender, EventArgs args) => System.Windows.Application.Current?.Dispatcher.InvokeAsync(() =>
    {
        if (IsOverview || IsJobs && OnlyNewProblems) Reset();
    });
    private async Task OpenStepAsync()
    {
        if (_navigation?.RunId is not { } runId) return;
        var stepId = _navigation.StepId;
        await _jobs.ReloadAsync();
        if (_navigation?.RunId != runId || _navigation.StepId != stepId || _disposed) return;
        _navigation = _queries.Navigation(runId, stepId, _jobs.Jobs.Values); NotifyDetails();
        if (_navigation.CanOpenStep && _jobs.Jobs.Values.FirstOrDefault(job => job.Id == _navigation.JobId) is { } job) RequestOpenJob?.Invoke(job, _navigation.StepId);
    }
    private async Task OpenOriginAsync()
    {
        if (_details?.Run.Context.ParentRunId is { } parent) await OpenRunAsync(parent);
        else if (_details?.Run.Context.AutomationId is { } automation)
        {
            Page = LogPageKind.Automations; await RefreshAsync();
            var choice = AutomationChoices.FirstOrDefault(item => item.Id == automation);
            if (choice is null) Status = Loc.Get("Logs.Ui.AutomationUnavailable");
            else SelectedAutomation = choice;
        }
    }
    private async Task ChooseExportAsync() { if (ChooseExportPath is null) return; var path = await ChooseExportPath(); if (path is not null) await ExportToAsync(path); }
    public async Task ExportToAsync(string path)
    {
        _export?.Dispose(); _export = new(); IsExporting = true;
        try
        {
            var version = typeof(LogsHomeViewModel).Assembly.GetName().Version?.ToString() ?? "";
            var result = IsRunDetail ? await _exports.ExportAsync(new(RunId: _details!.Run.Id), path, version, _export.Token)
                : IsRunList ? await _exports.ExportRunsAsync(RunFilter, path, version, _export.Token)
                : await _exports.ExportAsync(IsAutomation ? new(Source: LogSource.Automation, SourceId: SelectedAutomation?.Id, From: Dates().From, Until: Dates().Until, Search: Search, SnapshotSequence: _checkpoint) : EventFilter, path, version, _export.Token);
            Status = Loc.Get(result.IsComplete ? "Logs.Ui.Exported" : "Logs.Ui.ExportPartial");
        }
        catch (OperationCanceledException) { Status = Loc.Get("Logs.Ui.ExportCancelled"); }
        catch (Exception error) when (error is System.IO.IOException or UnauthorizedAccessException) { Status = Loc.Get("Logs.Ui.ExportFailed"); }
        finally { IsExporting = false; }
    }
    private void ReadStatus(LogReadState state, IReadOnlyList<string> issues) => Status = state is LogReadState.Partial or LogReadState.Unavailable ? Loc.Get($"Logs.Ui.Read.{state}") + (issues.Count > 0 ? " · " + string.Join(", ", issues) : "") : "";
    private int _pendingRefresh;
    private int _pendingStorageRefresh;
    private bool _storageDirty;
    private void OnEntry(object? sender, LogEvent entry) => Interlocked.Exchange(ref _pendingRefresh, 1);
    private void OnRun(object? sender, LogRun run) => Interlocked.Exchange(ref _pendingRefresh, 1);
    private void OnStorageChanged(object? sender, EventArgs args) => Interlocked.Exchange(ref _pendingStorageRefresh, 1);
    private async void OnTick(object? sender, EventArgs args)
    {
        if (Interlocked.Exchange(ref _pendingRefresh, 0) != 0) _dirty = true;
        if (Interlocked.Exchange(ref _pendingStorageRefresh, 0) != 0) { _storageDirty = true; _dirty = true; }
        foreach (var row in Runs) row.Tick(); OnPropertyChanged(nameof(Subtitle));
        if (!_dirty || IsLoading || !_active) return;
        if (_storageDirty)
        {
            _storageDirty = false;
            await RefreshAsync(preserveSnapshot: true);
            return;
        }
        if (IsApplication && !Live)
        {
            var page = await _queries.SearchAsync(EventFilter with { SnapshotSequence = long.MaxValue, AfterSequence = _checkpoint, PageSize = 1 });
            _newCount = page.MatchCount; OnPropertyChanged(nameof(NewText)); OnPropertyChanged(nameof(HasNew)); return;
        }
        if (!_catalogFrozen) await RefreshAsync();
    }
    private void OnCultureChanged(object? sender, EventArgs args)
    {
        foreach (var option in Tabs.Cast<ILogLocalizedOption>().Concat(Outcomes).Concat(Sources).Concat(Ranges).Concat(Areas).Concat(Levels).Concat(EventSources).Concat(RunSources)) option.UpdateCulture();
        foreach (var name in new[] { nameof(LiveText), nameof(NewText), nameof(AttentionText), nameof(AutomationContext) }) OnPropertyChanged(name);
        NotifyPage(); Reset();
    }
    private void RaiseCommands() { (MoreCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged(); (ExportCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged(); }
    private void NotifyPage() { foreach (var key in new[] { nameof(Page), nameof(ShowMainFilters), nameof(IsOverview), nameof(IsJobs), nameof(IsAutomation), nameof(IsApplication), nameof(IsRunList), nameof(IsRunDetail), nameof(Title), nameof(Subtitle), nameof(RunNumber), nameof(RunOutcome), nameof(RunTone), nameof(CountText) }) OnPropertyChanged(key); NotifyDetails(); }
    private void NotifyDetails() { foreach (var key in new[] { nameof(HasNextProblem), nameof(HasAttentionProblem), nameof(HasAttentionSummary), nameof(AttentionProblemState), nameof(AttentionSummaryState), nameof(CanOpenJob), nameof(CanOpenAutomation), nameof(CanOpenOrigin), nameof(DetailTitle), nameof(DetailSummary), nameof(DetailTone), nameof(Impact), nameof(Help), nameof(CanCopy), nameof(HasHelp), nameof(ShowHelp), nameof(HasImpact), nameof(HasCause), nameof(CanOpenStep), nameof(NavigationReason), nameof(HasNavigationReason), nameof(TechnicalText), nameof(TriggerInfo), nameof(TimelineDate), nameof(AutomationDate), nameof(DetailSeverity), nameof(Eligibility), nameof(HasEligibility), nameof(DiagnosticCode), nameof(DiagnosticTitle), nameof(PathActions), nameof(RelatedRuns), nameof(HasRelatedRun), nameof(ShowRelatedRun), nameof(RelatedRunText) }) OnPropertyChanged(key); (OpenStepCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged(); }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { _disposed = true; _navigationVersion++; _timer.Stop(); _timer.Tick -= OnTick; _load?.Cancel(); _load?.Dispose(); _export?.Cancel(); _export?.Dispose(); _attentionService.Changed -= OnAttentionChanged; _repository.EntryWritten -= OnEntry; _repository.RunChanged -= OnRun; _repository.StorageChanged -= OnStorageChanged; LocalizationService.Instance.CultureChanged -= OnCultureChanged; }
        base.Dispose(disposing);
    }
}

public sealed class LogRunRow(LogRun run) : ViewModelBase
{
    public LogRun Run { get; } = run;
    public string Name => Run.Name;
    public string Number => "#" + Run.ExecutionNumber;
    public string Result => LogUiText.Outcome(Run.Outcome);
    public string Tone => LogUiText.Tone(Run.Outcome.ToString());
    public string Origin => Loc.Get($"JobLog.Origin.{Run.Origin}") + (string.IsNullOrEmpty(Run.OriginName) ? "" : " · " + Run.OriginName);
    public string Time => LogUiText.Timestamp(Run.StartedAt);
    public string Duration => LogUiText.Duration(Run.DurationMs ?? (Run.Outcome is LogOutcome.Running or LogOutcome.Paused ? (long)(DateTimeOffset.Now - Run.StartedAt).TotalMilliseconds : null));
    public string Header => Time + "  ·  " + Duration + "  ·  " + Origin;
    public void Tick() => OnPropertyChanged(nameof(Duration));
}
public sealed record LogStepRow(LogStepDisplay Display)
{
    public string Title => Display.Execution.Step.Position + " · " + Loc.Get(Display.TitleKey);
    private Type? StepType => BuiltInStepDefinitions.Instance.TryGetByTypeId(Display.Execution.Step.TypeId, out var definition) ? definition.StepType : null;
    public PackIconMaterialKind Icon => StepType is { } type ? StepIconPresentation.ForType(type) : PackIconMaterialKind.ShapeOutline;
    public Brush IconBrush => Tone == "Muted" ? Brushes.SlateGray : StepType is { } type ? StepIconPresentation.ForCategory(StepIconPresentation.CategoryForType(type), null) : Brushes.SlateGray;
    public string Result => Loc.Get($"Logs.Ui.StepOutcome.{Display.Execution.Outcome}");
    public string Tone => LogUiText.Tone(Display.Execution.Outcome.ToString());
    public string Duration => Display.Execution.Summary?.AverageDurationMs is { } average
        ? Loc.Format("Logs.Ui.AggregatedDuration", LogUiText.Duration(Display.Execution.DurationMs), average)
        : LogUiText.Duration(Display.Execution.DurationMs);
    public string Context => Loc.Get($"Logs.Ui.Phase.{Display.Execution.Phase}")
        + (Display.Execution.Summary is { } summary ? " · " + Loc.Format("Logs.Ui.AggregatedCount", summary.Count)
            : Display.Execution.Iteration is { } iteration ? " · " + Loc.Format("Logs.Ui.Iteration", iteration) : "");
    public string Summary => Display.Execution.Cause is not null && Display.Impact is not null ? LogUiText.Text(Display.Impact) : LogUiText.Text(Display.Summary);
    public string Impact => Display.Impact is null ? "" : LogUiText.Text(Display.Impact);
}
public sealed record LogEventRow(LogEvent Event)
{
    public string Time => Event.Timestamp.LocalDateTime.ToString("HH:mm:ss", CultureInfo.CurrentCulture);
    public string Title => Event.Code == LogCodes.Message && Event.DiagnosticCode is null && !string.IsNullOrWhiteSpace(Event.Message) ? Event.Message : LogUiText.Text(LogPresentation.Event(Event).Title);
    public string Area => Loc.Get(LogPresentation.Event(Event).AreaKey);
    public string Level => Loc.Get($"Logs.Ui.Severity.{Event.Level}");
    public string Tone => LogUiText.Tone(Event.Level.ToString());
}
public sealed record LogAutomationRow(AutomationHistoryItem Item)
{
    public string Time => LogUiText.Timestamp(Item.ObservedAt);
    public string Title => Loc.Get(Item.TitleKey);
    public string Tone => Item.Reason is "Failed" or "StartRejected" ? "Error" : Item.TitleKey == "Log.Automation.Started" ? "Success" : "Muted";
    public string Summary => (Item.Trigger?.TargetName ?? Item.Name)
        + string.Concat(Item.StartedRuns.Where(link => link.RunId.HasValue).Select(link => " · #" + link.ExecutionNumber))
        + (Item.EligibleAt is { } eligible && Item.Reason == "Cooldown" ? "\n" + Loc.Format("Logs.Ui.Eligible", eligible.LocalDateTime.ToString("g")) : "")
        + (Item.Events.FirstOrDefault(entry => entry.Code == LogCodes.Trigger)?.Parameters.GetValueOrDefault("FileName") is { } name ? "\n" + Loc.Format("Logs.Ui.FileDetected", name) : "")
        + (Item.IsComplete ? "" : " · " + Loc.Get("Logs.Ui.Read.Partial"));
    public string Impact => LogUiText.Text(LogQueryService.AutomationImpact(Item));
    public string Context => Item.Trigger is { } trigger ? Loc.Format("Logs.Ui.AutomationContext", trigger.WatchedDirectory ?? "—", trigger.TargetName ?? "—") + " · " + trigger.EventKind : "";
}
public sealed record LogRunLinkRow(LogRunLink Link)
{
    public Guid? RunId => Link.RunId;
    public bool CanOpen => RunId.HasValue;
    public string Name => Link.Name ?? Loc.Get("Logs.Ui.RunUnavailable");
    public long? ExecutionNumber => Link.ExecutionNumber;
    public string UnavailableReason => Link.UnavailableReason is { } reason ? Loc.Get($"Logs.Ui.{reason}") : "";
    public string Result => Link.Outcome is { } outcome ? LogUiText.Outcome(outcome) : "";
}
public static class LogUiText
{
    public static string Outcome(LogOutcome outcome) => Loc.Get($"Logs.Ui.Outcome.{outcome}");
    public static string Tone(string value) => value switch { "Failed" or "Error" or "WithErrors" => "Error", "WithWarnings" or "Warning" => "Warning", "Successful" => "Success", "Running" or "Paused" => "Active", _ => "Muted" };
    public static string Text(LogText text) { var value = Loc.Get(text.Key); foreach (var argument in text.Arguments) value = value.Replace("{" + argument.Key + "}", argument.Value ?? "—", StringComparison.Ordinal); return value; }
    public static string Timestamp(DateTimeOffset stamp) => stamp.LocalDateTime.ToString(stamp.LocalDateTime.Date == DateTime.Today ? "HH:mm" : "g", CultureInfo.CurrentCulture);
    public static string Duration(long? ms) => ms is null ? "—" : ms < 60000 ? Loc.Format("Logs.Ui.Seconds", Math.Max(0, ms.Value) / 1000d) : Loc.Format("Logs.Ui.Minutes", (long)TimeSpan.FromMilliseconds(ms.Value).TotalMinutes, TimeSpan.FromMilliseconds(ms.Value).Seconds);
    public static string EventSummary(LogEvent entry) => entry.Code == LogCodes.Message && entry.DiagnosticCode is null && !string.IsNullOrWhiteSpace(entry.Message) ? entry.Message : Text(LogPresentation.Event(entry).Summary);
    public static string Technical(LogEvent entry) => Loc.Format("Logs.Ui.TechnicalHeader", entry.DiagnosticCode ?? entry.Code,
        entry.Timestamp.LocalDateTime.ToString("G", CultureInfo.CurrentCulture), Loc.Get(LogPresentation.Event(entry).AreaKey), entry.Details,
        string.Join("\n", entry.Paths.Select(path => $"{path.Kind}: {path.Value}")),
        string.Join("\n", entry.Parameters.Select(pair => $"{pair.Key}: {pair.Value}")), entry.Id, entry.Context.RunId, entry.Context.StepId, entry.Context.StepExecutionId, entry.Message);
}
