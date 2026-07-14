using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using DesktopAutomation.Application.Interfaces;
using TaskAutomation.Automations;
using TaskAutomation.Jobs;
using TaskAutomation.Orchestration;
using DesktopAutomationApp.Localization;

namespace DesktopAutomationApp.ViewModels
{
    public sealed class StartViewModel : ViewModelBase
    {
        private readonly IJobExecutor _executor;
        private readonly IJobDispatcher _dispatcher;
        private readonly IAutomationApplicationService _automationService;
        private readonly DispatcherTimer _relativeTimeTimer;

        // --- Stat cards ---
        private int _totalJobCount;
        public int TotalJobCount { get => _totalJobCount; private set => SetProperty(ref _totalJobCount, value); }

        private int _totalMakroCount;
        public int TotalMakroCount { get => _totalMakroCount; private set => SetProperty(ref _totalMakroCount, value); }

        private int _totalAutomationCount;
        public int TotalAutomationCount { get => _totalAutomationCount; private set => SetProperty(ref _totalAutomationCount, value); }

        private int _activeAutomationCount;
        public int ActiveAutomationCount { get => _activeAutomationCount; private set => SetProperty(ref _activeAutomationCount, value); }

        public ObservableCollection<AutomationDashboardInfo> ActiveAutomations { get; } = new();

        // --- Running Jobs ---
        public ObservableCollection<RunningJobInfo> RunningJobs { get; } = new();

        private int _runningJobCount;
        public int RunningJobCount { get => _runningJobCount; private set => SetProperty(ref _runningJobCount, value); }

        // --- Running Makros ---
        public ObservableCollection<RunningJobInfo> RunningMakros { get; } = new();

        private int _runningMakroCount;
        public int RunningMakroCount { get => _runningMakroCount; private set => SetProperty(ref _runningMakroCount, value); }

        // --- Combined running items (jobs + makros) ---
        private IReadOnlyList<GroupedRunningItem> _runningItems = Array.Empty<GroupedRunningItem>();
        public IReadOnlyList<GroupedRunningItem> RunningItems
        {
            get => _runningItems;
            private set { _runningItems = value; OnPropertyChanged(); }
        }

        private int _runningTotalCount;
        public int RunningTotalCount { get => _runningTotalCount; private set => SetProperty(ref _runningTotalCount, value); }

        // --- Commands ---
        public ICommand CancelJobCommand { get; }
        public ICommand CancelItemCommand { get; }
        public ICommand StopAllJobsCommand { get; }
        public ICommand RefreshCommand { get; }

        public StartViewModel(
            IJobExecutor executor,
            IJobDispatcher dispatcher,
            IAutomationApplicationService automationService)
        {
            _executor = executor;
            _dispatcher = dispatcher;
            _automationService = automationService;

            CancelJobCommand = new RelayCommand<object?>(param =>
            {
                if (param is Guid id)
                    _dispatcher.CancelJob(id);
                else if (param is string name && !string.IsNullOrEmpty(name))
                    _dispatcher.CancelJob(name);
            });
            CancelItemCommand = new RelayCommand<GroupedRunningItem?>(item =>
            {
                if (item == null) return;
                if (item.IsMakro)
                    _dispatcher.CancelMakro(item.Id);
                else
                    _dispatcher.CancelJobsByDefinition(item.Id);
            });
            StopAllJobsCommand = new RelayCommand(() =>
            {
                _dispatcher.CancelAllJobs();
                foreach (var id in _dispatcher.RunningMakroIds.ToList())
                    _dispatcher.CancelMakro(id);
            }, () => RunningTotalCount > 0);
            RefreshCommand = new RelayCommand(async () => await RefreshAsync());

            _dispatcher.RunningJobsChanged += OnRunningJobsChanged;
            _dispatcher.RunningMakrosChanged += OnRunningMakrosChanged;
            LocalizationService.Instance.CultureChanged += OnCultureChanged;

            _relativeTimeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _relativeTimeTimer.Tick += OnRelativeTimeTick;
            _relativeTimeTimer.Start();

            _ = RefreshAsync();
        }

        public async Task RefreshAsync()
        {
            RefreshRunningJobs();
            RefreshRunningMakros();
            RefreshCounts();
            await RefreshAutomationsAsync();
        }

        private async Task RefreshAutomationsAsync()
        {
            var automations = await _automationService.LoadAllAsync();
            TotalAutomationCount = automations.Count;
            ActiveAutomationCount = automations.Count(a => a.Active);

            ActiveAutomations.Clear();
            foreach (var automation in automations
                         .Where(a => a.Active)
                         .OrderBy(a => a.Runtime.NextRunAt ?? DateTimeOffset.MaxValue)
                         .ThenBy(a => a.Name))
            {
                ActiveAutomations.Add(new AutomationDashboardInfo
                {
                    Name = automation.Name,
                    Trigger = AutomationDisplayFormatter.Trigger(automation.Trigger),
                    Action = AutomationDisplayFormatter.Action(automation.Action),
                    LastRunAt = automation.Runtime.LastRunAt,
                    NextRun = automation.Runtime.NextRunAt?.LocalDateTime.ToString("g", LocalizationService.Instance.CurrentCulture) ?? Loc.Get("Automation.EventBased")
                });
            }
        }

        private void OnRelativeTimeTick(object? sender, EventArgs e)
        {
            foreach (var automation in ActiveAutomations)
                automation.RefreshLastRun();
        }

        private async void OnCultureChanged(object? sender, EventArgs e) => await RefreshAutomationsAsync();

        private void RefreshRunningJobs()
        {
            var instances = _dispatcher.RunningJobInstances;
            RunningJobCount = instances.Count;
            RebuildRunningItems();
        }

        private void RefreshRunningMakros()
        {
            var runningIds = _dispatcher.RunningMakroIds;
            var allMakros = _executor.AllMakros;

            RunningMakros.Clear();
            foreach (var id in runningIds)
            {
                var makro = allMakros.Values.FirstOrDefault(m => m.Id == id);
                if (makro != null)
                    RunningMakros.Add(new RunningJobInfo { Id = id, Name = makro.Name });
            }
            RunningMakroCount = RunningMakros.Count;
            RebuildRunningItems();
        }

        private void RebuildRunningItems()
        {
            var instances = _dispatcher.RunningJobInstances;
            RunningItems = instances
                .GroupBy(i => (i.JobId, i.JobName))
                .Select(g => new GroupedRunningItem { Id = g.Key.JobId, Name = g.Key.JobName, InstanceCount = g.Count(), IsMakro = false })
                .Concat(RunningMakros.Select(m => new GroupedRunningItem { Id = m.Id, Name = m.Name, InstanceCount = 1, IsMakro = true }))
                .ToList();
            RunningTotalCount = instances.Count + RunningMakros.Count;
            (StopAllJobsCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private void RefreshCounts()
        {
            TotalJobCount = _executor.AllJobs.Count;
            TotalMakroCount = _executor.AllMakros.Count;
        }

        private void OnRunningJobsChanged()
        {
            var snapshot = _dispatcher.RunningJobInstances;
            var grouped  = snapshot
                .GroupBy(i => (i.JobId, i.JobName))
                .Select(g => new GroupedRunningItem { Id = g.Key.JobId, Name = g.Key.JobName, InstanceCount = g.Count(), IsMakro = false })
                .ToList();
            var total = snapshot.Count;

            Application.Current?.Dispatcher?.InvokeAsync(() =>
            {
                RunningJobCount = total;
                RunningItems = grouped
                    .Concat(RunningMakros.Select(m => new GroupedRunningItem { Id = m.Id, Name = m.Name, InstanceCount = 1, IsMakro = true }))
                    .ToList();
                RunningTotalCount = total + RunningMakros.Count;
                (StopAllJobsCommand as RelayCommand)?.RaiseCanExecuteChanged();
            });
        }

        private void OnRunningMakrosChanged()
        {
            var runningIds  = _dispatcher.RunningMakroIds;
            var allMakros   = _executor.AllMakros;
            var makroItems  = runningIds
                .Select(id => allMakros.Values.FirstOrDefault(m => m.Id == id))
                .Where(m => m is not null)
                .Select(m => new RunningJobInfo { Id = m!.Id, Name = m.Name })
                .ToList();
            var jobSnapshot = _dispatcher.RunningJobInstances;
            var grouped     = jobSnapshot
                .GroupBy(i => (i.JobId, i.JobName))
                .Select(g => new GroupedRunningItem { Id = g.Key.JobId, Name = g.Key.JobName, InstanceCount = g.Count(), IsMakro = false })
                .ToList();
            var jobTotal = jobSnapshot.Count;

            Application.Current?.Dispatcher?.InvokeAsync(() =>
            {
                RunningMakros.Clear();
                foreach (var m in makroItems) RunningMakros.Add(m);
                RunningMakroCount = RunningMakros.Count;
                RunningJobCount   = jobTotal;
                RunningItems = grouped
                    .Concat(RunningMakros.Select(m => new GroupedRunningItem { Id = m.Id, Name = m.Name, InstanceCount = 1, IsMakro = true }))
                    .ToList();
                RunningTotalCount = jobTotal + RunningMakros.Count;
                (StopAllJobsCommand as RelayCommand)?.RaiseCanExecuteChanged();
            });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _dispatcher.RunningJobsChanged -= OnRunningJobsChanged;
                _dispatcher.RunningMakrosChanged -= OnRunningMakrosChanged;
                LocalizationService.Instance.CultureChanged -= OnCultureChanged;
                _relativeTimeTimer.Stop();
                _relativeTimeTimer.Tick -= OnRelativeTimeTick;
            }
            base.Dispose(disposing);
        }
    }

    public sealed class RunningJobInfo
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = "";
    }

    public sealed class AutomationDashboardInfo : ViewModelBase
    {
        public string Name { get; init; } = string.Empty;
        public string Trigger { get; init; } = string.Empty;
        public string Action { get; init; } = string.Empty;
        public DateTimeOffset? LastRunAt { get; init; }
        public string LastRun => AutomationDisplayFormatter.LastRun(LastRunAt);
        public string NextRun { get; init; } = string.Empty;

        public void RefreshLastRun() => OnPropertyChanged(nameof(LastRun));
    }

    /// <summary>Gruppenzeile in der "Laufende Jobs"-Liste: ein Eintrag pro Job-Definition, mit Instanz-Zähler.</summary>
    public sealed class GroupedRunningItem
    {
        public Guid Id { get; init; }           // Job-Definition-ID oder Makro-ID
        public string Name { get; init; } = "";
        public int InstanceCount { get; set; }
        public bool IsMakro { get; init; }
    }
}
