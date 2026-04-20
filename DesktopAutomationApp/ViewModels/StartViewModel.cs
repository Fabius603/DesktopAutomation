using System;
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

        // --- Commands ---
        public ICommand ToggleHotkeyPauseCommand { get; }
        public ICommand CancelJobCommand { get; }
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
            RefreshCommand = new RelayCommand(Refresh);

            _dispatcher.RunningJobsChanged += OnRunningJobsChanged;
            _hotkeyService.HotkeysChanged += OnHotkeysChanged;

            Refresh();
        }

        public void Refresh()
        {
            RefreshHotkeys();
            RefreshRunningJobs();
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
            var runningIds = _dispatcher.RunningJobIds;
            var allJobs = _executor.AllJobs;

            RunningJobs.Clear();
            foreach (var id in runningIds)
            {
                var job = allJobs.Values.FirstOrDefault(j => j.Id == id);
                if (job != null)
                {
                    RunningJobs.Add(new RunningJobInfo { Id = id, Name = job.Name });
                }
            }
            RunningJobCount = RunningJobs.Count;
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
            Application.Current?.Dispatcher?.Invoke(RefreshRunningJobs);
        }

        private void OnHotkeysChanged()
        {
            Application.Current?.Dispatcher?.Invoke(() =>
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
}
