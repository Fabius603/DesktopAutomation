using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using TaskAutomation.Hotkeys;
using TaskAutomation.Jobs;
using TaskAutomation.Orchestration;

namespace DesktopAutomationApp.ViewModels
{
    public sealed class StartViewModel : ViewModelBase
    {
        private readonly IGlobalHotkeyService _hotkeyService;
        private readonly IJobExecutor _executor;
        private readonly IJobDispatcher _dispatcher;

        // --- Stat cards ---
        private int _activeHotkeyCount;
        public int ActiveHotkeyCount { get => _activeHotkeyCount; private set => SetProperty(ref _activeHotkeyCount, value); }

        private int _totalHotkeyCount;
        public int TotalHotkeyCount { get => _totalHotkeyCount; private set => SetProperty(ref _totalHotkeyCount, value); }

        private int _totalJobCount;
        public int TotalJobCount { get => _totalJobCount; private set => SetProperty(ref _totalJobCount, value); }

        private int _totalMakroCount;
        public int TotalMakroCount { get => _totalMakroCount; private set => SetProperty(ref _totalMakroCount, value); }

        // --- Hotkeys ---
        public ObservableCollection<HotkeyInfo> ActiveHotkeys { get; } = new();

        private bool _isHotkeysPaused;
        public bool IsHotkeysPaused
        {
            get => _isHotkeysPaused;
            private set { _isHotkeysPaused = value; OnPropertyChanged(); OnPropertyChanged(nameof(PauseButtonText)); }
        }
        public string PauseButtonText => IsHotkeysPaused ? "Hotkeys fortsetzen" : "Hotkeys pausieren";

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
        public ICommand ToggleHotkeyPauseCommand { get; }
        public ICommand CancelJobCommand { get; }
        public ICommand CancelItemCommand { get; }
        public ICommand StopAllJobsCommand { get; }
        public ICommand RefreshCommand { get; }

        public StartViewModel(
            IGlobalHotkeyService hotkeyService,
            IJobExecutor executor,
            IJobDispatcher dispatcher)
        {
            _hotkeyService = hotkeyService;
            _executor = executor;
            _dispatcher = dispatcher;

            var assembly = Assembly.GetEntryAssembly();

            ToggleHotkeyPauseCommand = new RelayCommand(TogglePause);
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
            RefreshCommand = new RelayCommand(Refresh);

            _dispatcher.RunningJobsChanged += OnRunningJobsChanged;
            _dispatcher.RunningMakrosChanged += OnRunningMakrosChanged;
            _hotkeyService.HotkeysChanged += OnHotkeysChanged;

            Refresh();
        }

        public void Refresh()
        {
            RefreshHotkeys();
            RefreshRunningJobs();
            RefreshRunningMakros();
            RefreshCounts();
        }

        private void RefreshHotkeys()
        {
            IsHotkeysPaused = _hotkeyService.IsPaused;

            ActiveHotkeys.Clear();
            foreach (var hk in _hotkeyService.Hotkeys.Values.OrderBy(h => h.Name))
            {
                ActiveHotkeys.Add(new HotkeyInfo
                {
                    Name = hk.Name,
                    Trigger = _hotkeyService.FormatKey(hk.Modifiers, hk.VirtualKeyCode),
                    ActionName = hk.Job?.Name ?? "",
                    Command = hk.Job?.Command.ToString() ?? ""
                });
            }
            ActiveHotkeyCount = ActiveHotkeys.Count;
        }

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
            TotalHotkeyCount = _hotkeyService.Hotkeys.Count;
            TotalJobCount = _executor.AllJobs.Count;
            TotalMakroCount = _executor.AllMakros.Count;
        }

        private void TogglePause()
        {
            _hotkeyService.SetPaused(!_hotkeyService.IsPaused);
            IsHotkeysPaused = _hotkeyService.IsPaused;
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

        private void OnHotkeysChanged()
        {
            Application.Current?.Dispatcher?.InvokeAsync(() =>
            {
                RefreshHotkeys();
                RefreshCounts();
            });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _dispatcher.RunningJobsChanged -= OnRunningJobsChanged;
                _dispatcher.RunningMakrosChanged -= OnRunningMakrosChanged;
                _hotkeyService.HotkeysChanged -= OnHotkeysChanged;
            }
            base.Dispose(disposing);
        }
    }

    public sealed class HotkeyInfo
    {
        public string Name { get; init; } = "";
        public string Trigger { get; init; } = "";
        public string ActionName { get; init; } = "";
        public string Command { get; init; } = "";
    }

    public sealed class RunningJobInfo
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = "";
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
