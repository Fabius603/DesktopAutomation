using DesktopAutomationApp.Services;
using DesktopAutomationApp.Services.Preview;
using DesktopAutomationApp.Views;
using DesktopOverlay;
using ImageHelperMethods;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TaskAutomation.Hotkeys;
using TaskAutomation.Jobs;
using TaskAutomation.Makros;
using TaskAutomation.Orchestration;

namespace DesktopAutomationApp.ViewModels
{
    public sealed class MakroStepsViewModel : ViewModelBase, INavigationGuard, IDisposable
    {
        private readonly ILogger<MakroStepsViewModel> _log;
        private readonly IJobExecutor _executor;
        private readonly IMacroPreviewService _preview;
        private readonly IRepositoryService _repositoryService;
        private readonly IGlobalHotkeyService _hotkeys;
        private readonly IJobDispatcher _dispatcher;

        private readonly List<MakroBefehl> _originalSteps;
        private Overlay _overlay;
        private MacroPreviewService.PreviewResult _lastPreview;

        private readonly Stack<List<MakroBefehl>> _undoStack = new();
        private readonly Stack<List<MakroBefehl>> _redoStack = new();
        private List<MakroBefehl> _clipboard = new();

        public Makro Makro { get; }
        public string Title => Makro.Name;

        public ObservableCollection<MakroBefehl> Steps { get; }

        /// <summary>Incrementiert bei jeder Listenänderung; wird von Konvertern als Cache-Schlüssel genutzt.</summary>
        public int StepsVersion { get; private set; }

        /// <summary>All currently selected steps (synced from the view's ListBox.SelectedItems).</summary>
        public List<MakroBefehl> SelectedSteps { get; } = new();

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        private MakroBefehl? _selectedStep;
        public MakroBefehl? SelectedStep
        {
            get => _selectedStep;
            set { _selectedStep = value; OnPropertyChanged(); InvalidateAllCommands(); }
        }

        private bool _hasUnsavedChanges;
        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            private set { _hasUnsavedChanges = value; OnPropertyChanged(); InvalidateAllCommands(); }
        }

        // --- Recording state ---
        private bool _isRecording;
        public bool IsRecording
        {
            get => _isRecording;
            private set { if (_isRecording == value) return; _isRecording = value; OnPropertyChanged(); OnPropertyChanged(nameof(RecordButtonText)); InvalidateAllCommands(); }
        }
        public string RecordButtonText => IsRecording ? "Aufnahme stoppen" : "Aufnahme starten";

        private bool _isCapturingClick;
        public bool IsCapturingClick
        {
            get => _isCapturingClick;
            private set { if (_isCapturingClick == value) return; _isCapturingClick = value; OnPropertyChanged(); OnPropertyChanged(nameof(CaptureClickButtonText)); InvalidateAllCommands(); }
        }
        public string CaptureClickButtonText => IsCapturingClick ? "Klick-Erfassung läuft..." : "Einzelnen Klick erfassen";

        private bool _recordMousePath;
        public bool RecordMousePath
        {
            get => _recordMousePath;
            set { if (_recordMousePath == value) return; _recordMousePath = value; OnPropertyChanged(); OnPropertyChanged(nameof(RecordMousePathText)); }
        }
        public string RecordMousePathText => RecordMousePath ? "Mauspfad: AN" : "Mauspfad: AUS";

        private bool _isMakroRunning;
        public bool IsMakroRunning
        {
            get => _isMakroRunning;
            private set { _isMakroRunning = value; OnPropertyChanged(); InvalidateAllCommands(); }
        }

        // --- Commands ---
        public ICommand BackCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand RenameCommand { get; }
        public ICommand AddStepCommand { get; }
        public ICommand EditStepCommand { get; }
        public ICommand MoveStepUpCommand { get; }
        public ICommand MoveStepDownCommand { get; }
        public ICommand ReorderStepCommand { get; }
        public ICommand DeleteStepCommand { get; }
        public ICommand DeleteSelectedCommand { get; }
        public ICommand UndoCommand { get; }
        public ICommand RedoCommand { get; }
        public ICommand CopyCommand { get; }
        public ICommand PasteCommand { get; }
        public ICommand DuplicateStepCommand { get; }
        public ICommand RecordStepsCommand { get; }
        public ICommand CaptureClickCommand { get; }
        public ICommand ToggleMousePathCommand { get; }
        public ICommand PreviewOverviewCommand { get; }
        public ICommand PreviewPlaybackCommand { get; }
        public ICommand PreviewStopCommand { get; }
        public ICommand StartMakroCommand { get; }
        public ICommand StopMakroCommand { get; }

        public event Action? RequestBack;

        public MakroStepsViewModel(
            Makro makro,
            IJobExecutor executor,
            ILogger<MakroStepsViewModel> log,
            IMacroPreviewService preview,
            IRepositoryService repositoryService,
            IGlobalHotkeyService hotkeys,
            IJobDispatcher dispatcher)
        {
            Makro = makro ?? throw new ArgumentNullException(nameof(makro));
            _executor = executor;
            _log = log;
            _preview = preview;
            _repositoryService = repositoryService;
            _hotkeys = hotkeys;
            _dispatcher = dispatcher;

            _originalSteps = new List<MakroBefehl>(makro.Befehle ?? Enumerable.Empty<MakroBefehl>());
            Steps = new ObservableCollection<MakroBefehl>(makro.Befehle ?? Enumerable.Empty<MakroBefehl>());
            Steps.CollectionChanged += (_, _) =>
            {
                StepsVersion++;
                OnPropertyChanged(nameof(StepsVersion));
                InvalidateAllCommands();
            };

            BackCommand   = new RelayCommand(() => RequestBack?.Invoke());
            SaveCommand   = new RelayCommand(async () => await SaveInternal(), () => HasUnsavedChanges);
            CancelCommand = new RelayCommand(DiscardChanges, () => HasUnsavedChanges);
            RenameCommand = new RelayCommand(() => Rename());

            AddStepCommand      = new RelayCommand(async () => await OpenAddStepDialog());
            EditStepCommand     = new RelayCommand<MakroBefehl?>(EditStep, s => s != null);
            MoveStepUpCommand   = new RelayCommand<MakroBefehl?>(s => MoveRelative(s, -1), s => CanMoveRelative(s, -1));
            MoveStepDownCommand = new RelayCommand<MakroBefehl?>(s => MoveRelative(s, +1), s => CanMoveRelative(s, +1));
            ReorderStepCommand  = new RelayCommand<(int from, int to)>(t => MoveToIndex(t.from, t.to));
            DeleteStepCommand     = new RelayCommand<MakroBefehl?>(DeleteStep, s => s != null);
            DeleteSelectedCommand = new RelayCommand(DeleteSelected, () => SelectedSteps.Count > 0 || SelectedStep != null);
            UndoCommand           = new RelayCommand(Undo, () => CanUndo);
            RedoCommand           = new RelayCommand(Redo, () => CanRedo);
            CopyCommand           = new RelayCommand(CopySelected, () => SelectedSteps.Count > 0 || SelectedStep != null);
            PasteCommand          = new RelayCommand(Paste, () => _clipboard.Count > 0);
            DuplicateStepCommand  = new RelayCommand<MakroBefehl?>(DuplicateStep, s => s != null);

            RecordStepsCommand   = new RelayCommand(async () => await ToggleRecordAsync());
            CaptureClickCommand  = new RelayCommand(async () => await CaptureClickAsync(), () => !IsCapturingClick && !IsRecording);
            ToggleMousePathCommand = new RelayCommand(() => { RecordMousePath = !RecordMousePath; InvalidateAllCommands(); });

            PreviewOverviewCommand = new RelayCommand(ShowOverview, CanPreview);
            PreviewPlaybackCommand = new RelayCommand(() => ShowPlayback(), CanPreview);
            PreviewStopCommand     = new RelayCommand(StopPreview, () => _overlay != null);

            StartMakroCommand = new RelayCommand(() => _dispatcher.StartMakro(Makro.Id), () => !IsMakroRunning);
            StopMakroCommand  = new RelayCommand(() => _dispatcher.CancelMakro(Makro.Id), () => IsMakroRunning);

            _dispatcher.RunningMakrosChanged += OnRunningMakrosChanged;
            IsMakroRunning = _dispatcher.RunningMakroIds.Contains(Makro.Id);
        }

        // ---------- Selection sync (called from code-behind) ----------
        public void SetSelectedSteps(IEnumerable<object> items)
        {
            SelectedSteps.Clear();
            SelectedSteps.AddRange(items.OfType<MakroBefehl>());
            if (SelectedSteps.Count > 0)
                SelectedStep = SelectedSteps[^1];
            InvalidateAllCommands();
        }

        // ---------- INavigationGuard ----------
        public async Task SaveAsync() => await SaveInternal();

        public void DiscardChanges()
        {
            Steps.Clear();
            foreach (var s in _originalSteps)
                Steps.Add(s);
            HasUnsavedChanges = false;
        }

        // ---------- Save ----------
        private async Task SaveInternal()
        {
            Makro.Befehle = new ObservableCollection<MakroBefehl>(Steps);
            await _repositoryService.SaveAsync(Makro);
            await _executor.ReloadMakrosAsync();
            // Update snapshot for future cancel
            _originalSteps.Clear();
            _originalSteps.AddRange(Steps);
            HasUnsavedChanges = false;
        }

        // ---------- Rename ----------
        private async void Rename()
        {
            var dlg = new NewItemNameDialog("Umbenennen", "Neuer Name:", Makro.Name)
                { Owner = Application.Current?.MainWindow };
            if (dlg.ShowDialog() != true) return;

            Makro.Name = dlg.ResultName.Trim();
            OnPropertyChanged(nameof(Title));
            await _repositoryService.SaveAsync(Makro);
            await _executor.ReloadMakrosAsync();
        }

        // ---------- Steps ----------
        private bool CanMoveRelative(MakroBefehl? step, int delta)
        {
            if (step == null) return false;
            var idx = Steps.IndexOf(step);
            if (idx < 0) return false;
            var newIdx = idx + delta;
            return newIdx >= 0 && newIdx < Steps.Count;
        }

        private void MoveRelative(MakroBefehl? step, int delta)
        {
            if (!CanMoveRelative(step, delta) || step == null) return;
            PushUndo();
            var idx = Steps.IndexOf(step);
            Steps.RemoveAt(idx);
            Steps.Insert(idx + delta, step);
            SelectedStep = step;
            HasUnsavedChanges = true;
            InvalidateAllCommands();
        }

        private void MoveToIndex(int from, int to)
        {
            if (from < 0 || from >= Steps.Count || to < 0 || to >= Steps.Count || from == to) return;
            PushUndo();
            var step = Steps[from];
            Steps.RemoveAt(from);
            Steps.Insert(to, step);
            SelectedStep = step;
            HasUnsavedChanges = true;
            InvalidateAllCommands();
        }

        private void EditStep(MakroBefehl? step)
        {
            if (step == null) return;
            var index = Steps.IndexOf(step);
            if (index < 0) return;

            var vm = new AddStepDialogViewModel(_hotkeys) { Mode = StepDialogMode.Edit };
            Prefill(vm, step);

            var dlg = new AddStepDialog { Owner = Application.Current.MainWindow, DataContext = vm };
            if (dlg.ShowDialog() != true || vm.CreatedStep == null) return;

            PushUndo();
            Steps[index] = vm.CreatedStep;
            SelectedStep = vm.CreatedStep;
            HasUnsavedChanges = true;
            InvalidateAllCommands();
        }

        private async Task OpenAddStepDialog()
        {
            var dlgVm = new AddStepDialogViewModel(_hotkeys) { Mode = StepDialogMode.Add };
            var dlg = new AddStepDialog { Owner = Application.Current.MainWindow, DataContext = dlgVm };

            if (dlg.ShowDialog() != true || dlgVm.CreatedStep == null) return;

            int insertIndex = SelectedStep != null
                ? Math.Min(Steps.Count, Steps.IndexOf(SelectedStep) + 1)
                : Steps.Count;

            PushUndo();
            Steps.Insert(insertIndex, dlgVm.CreatedStep);
            SelectedStep = dlgVm.CreatedStep;
            HasUnsavedChanges = true;
        }

        private void DeleteStep(MakroBefehl? step)
        {
            if (step == null) return;

            var result = AppDialog.Show(
                $"Möchten Sie diesen Step wirklich löschen?",
                "Löschen bestätigen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            PushUndo();
            var idx = Steps.IndexOf(step);
            if (idx < 0) return;

            var next = Steps.ElementAtOrDefault(Math.Max(0, idx - 1))
                      ?? Steps.ElementAtOrDefault(idx + 1);
            Steps.RemoveAt(idx);
            SelectedStep = next;
            HasUnsavedChanges = true;
            InvalidateAllCommands();
        }

        private void DuplicateStep(MakroBefehl? step)
        {
            if (step == null) return;
            PushUndo();
            var clone = CloneStep(step);
            int insertIndex = SelectedStep != null
                ? Math.Min(Steps.Count, Steps.IndexOf(SelectedStep) + 1)
                : Steps.Count;
            Steps.Insert(insertIndex, clone);
            SelectedStep = clone;
            HasUnsavedChanges = true;
        }

        private static MakroBefehl CloneStep(MakroBefehl s)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(s, JsonOptions.Default);
            return System.Text.Json.JsonSerializer.Deserialize<MakroBefehl>(json, JsonOptions.Default)!;
        }

        // ---------- Undo / Redo ----------
        private void PushUndo()
        {
            _undoStack.Push(Steps.Select(s => CloneStep(s)).ToList());
            _redoStack.Clear();
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
        }

        private void Undo()
        {
            if (_undoStack.Count == 0) return;
            _redoStack.Push(Steps.Select(s => CloneStep(s)).ToList());
            RestoreSnapshot(_undoStack.Pop());
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
        }

        private void Redo()
        {
            if (_redoStack.Count == 0) return;
            _undoStack.Push(Steps.Select(s => CloneStep(s)).ToList());
            RestoreSnapshot(_redoStack.Pop());
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
        }

        private void RestoreSnapshot(List<MakroBefehl> snapshot)
        {
            Steps.Clear();
            foreach (var s in snapshot) Steps.Add(s);
            SelectedStep = null;
            SelectedSteps.Clear();
            HasUnsavedChanges = true;
        }

        // ---------- Copy / Paste ----------
        private void CopySelected()
        {
            var sources = SelectedSteps.Count > 0
                ? SelectedSteps.OrderBy(s => Steps.IndexOf(s)).ToList()
                : (SelectedStep != null ? new List<MakroBefehl> { SelectedStep } : null);
            if (sources == null) return;
            _clipboard = sources.Select(s => CloneStep(s)).ToList();
            InvalidateAllCommands();
        }

        private void Paste()
        {
            if (_clipboard.Count == 0) return;

            int insertAt = SelectedStep != null
                ? Math.Min(Steps.Count, Steps.IndexOf(SelectedStep) + 1)
                : Steps.Count;

            var toInsert = _clipboard.Select(s => CloneStep(s)).ToList();
            PushUndo();
            for (int i = 0; i < toInsert.Count; i++)
                Steps.Insert(insertAt + i, toInsert[i]);

            SelectedStep = toInsert[^1];
            HasUnsavedChanges = true;
        }

        // ---------- Delete selected ----------
        private void DeleteSelected()
        {
            var targets = SelectedSteps.Count > 0
                ? SelectedSteps.ToList()
                : (SelectedStep != null ? new List<MakroBefehl> { SelectedStep } : null);
            if (targets == null || targets.Count == 0) return;

            string message = targets.Count == 1
                ? "Möchten Sie den Step wirklich löschen?"
                : $"Möchten Sie die {targets.Count} ausgewählten Steps löschen?";

            if (AppDialog.Show(message, "Löschen bestätigen", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            PushUndo();
            int firstIdx = targets.Select(t => Steps.IndexOf(t)).Where(i => i >= 0).DefaultIfEmpty(0).Min();
            foreach (var t in targets)
            {
                var idx = Steps.IndexOf(t);
                if (idx >= 0) Steps.RemoveAt(idx);
            }

            SelectedStep = Steps.ElementAtOrDefault(Math.Max(0, firstIdx - 1));
            SelectedSteps.Clear();
            HasUnsavedChanges = true;
            InvalidateAllCommands();
        }

        private static void Prefill(AddStepDialogViewModel vm, MakroBefehl step)
        {
            switch (step)
            {
                case MouseMoveAbsoluteBefehl mm:
                    vm.SelectedType = "MouseMoveAbsolute"; vm.X = mm.X; vm.Y = mm.Y; break;
                case MouseMoveRelativeBefehl mr:
                    vm.SelectedType = "MouseMoveRelative"; vm.DeltaX = mr.DeltaX; vm.DeltaY = mr.DeltaY; break;
                case MouseDownBefehl md:
                    vm.SelectedType = "MouseDown"; vm.MouseButton = md.Button; break;
                case MouseUpBefehl mu:
                    vm.SelectedType = "MouseUp"; vm.MouseButton = mu.Button; break;
                case KeyDownBefehl kd:
                    vm.SelectedType = "KeyDown"; vm.Key = kd.Key; break;
                case KeyUpBefehl ku:
                    vm.SelectedType = "KeyUp"; vm.Key = ku.Key; break;
                case TimeoutBefehl to:
                    vm.SelectedType = "Timeout"; vm.Duration = to.Duration; break;
            }
        }

        // ---------- Recording ----------
        private async Task ToggleRecordAsync()
        {
            try
            {
                if (!IsRecording)
                {
                    _hotkeys.StartRecordHotkeys();
                    IsRecording = true;
                    _log.LogInformation("Makro-Aufnahme gestartet.");
                }
                else
                {
                    var events = _hotkeys.StopRecordHotkeys();
                    IsRecording = false;
                    _log.LogInformation("Makro-Aufnahme gestoppt. Events: {Count}", events?.Count ?? 0);

                    if (events == null || events.Count == 0) return;

                    var mapped = MapCapturedEventsToSteps(events);
                    int insertIndex = SelectedStep != null
                        ? Math.Min(Steps.Count, Steps.IndexOf(SelectedStep) + 1)
                        : Steps.Count;

                    foreach (var s in mapped)
                        Steps.Insert(insertIndex++, s);

                    if (mapped.Count > 0)
                    {
                        SelectedStep = mapped.Last();
                        HasUnsavedChanges = true;
                    }
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Fehler bei der Makro-Aufnahme.");
                IsRecording = false;
            }
        }

        private async Task CaptureClickAsync()
        {
            try
            {
                IsCapturingClick = true;
                _log.LogInformation("Einzelklick-Erfassung gestartet.");

                using var clickOverlay = new ClickCaptureOverlay();
                var clickPoint = await clickOverlay.CaptureClickAsync();

                _log.LogInformation("Klick erfasst bei: {X}, {Y}", clickPoint.X, clickPoint.Y);

                var moveCommand = new MouseMoveAbsoluteBefehl { X = clickPoint.X, Y = clickPoint.Y };

                int insertIndex = SelectedStep != null
                    ? Math.Min(Steps.Count, Steps.IndexOf(SelectedStep) + 1)
                    : Steps.Count;

                Steps.Insert(insertIndex, moveCommand);
                SelectedStep = moveCommand;
                HasUnsavedChanges = true;
            }
            catch (OperationCanceledException)
            {
                _log.LogInformation("Einzelklick-Erfassung abgebrochen.");
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Fehler bei der Einzelklick-Erfassung.");
            }
            finally
            {
                IsCapturingClick = false;
            }
        }

        private List<MakroBefehl> MapCapturedEventsToSteps(IReadOnlyList<CapturedInputEvent> events)
        {
            var list = new List<MakroBefehl>(events.Count);
            int? lastMouseX = null, lastMouseY = null;
            int accumulatedTimeout = 0;

            foreach (var ev in events)
            {
                switch (ev)
                {
                    case TimeoutEvent t:
                        accumulatedTimeout += t.Milliseconds;
                        break;
                    case KeyDownCaptured kd:
                        AddAccumulatedTimeout(list, ref accumulatedTimeout);
                        list.Add(new KeyDownBefehl { Key = _hotkeys.FormatKey(KeyModifiers.None, kd.VirtualKey) });
                        break;
                    case KeyUpCaptured ku:
                        AddAccumulatedTimeout(list, ref accumulatedTimeout);
                        list.Add(new KeyUpBefehl { Key = _hotkeys.FormatKey(KeyModifiers.None, ku.VirtualKey) });
                        break;
                    case MouseMoveCaptured mm:
                        if (RecordMousePath)
                        {
                            AddAccumulatedTimeout(list, ref accumulatedTimeout);
                            list.Add(new MouseMoveAbsoluteBefehl { X = mm.X, Y = mm.Y });
                            lastMouseX = mm.X; lastMouseY = mm.Y;
                        }
                        break;
                    case MouseDownCaptured md:
                        AddAccumulatedTimeout(list, ref accumulatedTimeout);
                        if (!RecordMousePath && (lastMouseX != md.X || lastMouseY != md.Y))
                        {
                            list.Add(new MouseMoveAbsoluteBefehl { X = md.X, Y = md.Y });
                            lastMouseX = md.X; lastMouseY = md.Y;
                        }
                        list.Add(new MouseDownBefehl { Button = _hotkeys.FormatMouseButton(md.Button) });
                        break;
                    case MouseUpCaptured mu:
                        AddAccumulatedTimeout(list, ref accumulatedTimeout);
                        if (!RecordMousePath && (lastMouseX != mu.X || lastMouseY != mu.Y))
                        {
                            list.Add(new MouseMoveAbsoluteBefehl { X = mu.X, Y = mu.Y });
                            lastMouseX = mu.X; lastMouseY = mu.Y;
                        }
                        list.Add(new MouseUpBefehl { Button = _hotkeys.FormatMouseButton(mu.Button) });
                        break;
                }
            }

            if (list.Count > 5) list.RemoveRange(list.Count - 5, 5);
            else list.Clear();

            return list;
        }

        private static void AddAccumulatedTimeout(List<MakroBefehl> list, ref int accumulatedTimeout)
        {
            if (accumulatedTimeout > 0)
            {
                list.Add(new TimeoutBefehl { Duration = accumulatedTimeout });
                accumulatedTimeout = 0;
            }
        }

        // ---------- Preview ----------
        private bool CanPreview() => Steps.Count > 0;

        private void EnsureOverlay()
        {
            if (_overlay != null) return;
            var v = ScreenHelper.GetVirtualDesktopBounds();
            _overlay = new Overlay(v.Left, v.Top, v.Width, v.Height);
            _overlay.RunInNewThread();
        }

        private void BuildPreview()
        {
            var v = ScreenHelper.GetVirtualDesktopBounds();
            // Build preview from current Steps (not saved Makro.Befehle)
            var tempMakro = new Makro { Name = Makro.Name, Befehle = new ObservableCollection<MakroBefehl>(Steps) };
            _lastPreview = _preview.Build(tempMakro, v, v);
        }

        private void ShowOverview()
        {
            EnsureOverlay();
            BuildPreview();
            _overlay.StopPlayback();
            _overlay.ClearItems();
            _overlay.AddItems(_lastPreview.StaticItems);
        }

        private void ShowPlayback(double speed = 1.0)
        {
            EnsureOverlay();
            BuildPreview();
            _overlay.ClearItems();
            _overlay.AddItems(_lastPreview.StaticItems);
            _overlay.AddItems(_lastPreview.TimedItems);
            _overlay.PlaybackSpeed = speed;
            _overlay.StartPlayback(0.0);
        }

        private void StopPreview()
        {
            if (_overlay == null) return;
            _overlay.StopPlayback();
            _overlay.ClearItems();
        }

        public new void Dispose()
        {
            _dispatcher.RunningMakrosChanged -= OnRunningMakrosChanged;
            StopPreview();
            _overlay?.Dispose();
            _overlay = null;
        }

        private void OnRunningMakrosChanged()
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                IsMakroRunning = _dispatcher.RunningMakroIds.Contains(Makro.Id);
            });
        }

        // ---------- Command invalidation helper ----------
        private void InvalidateAllCommands()
        {
            (SaveCommand            as RelayCommand)?.RaiseCanExecuteChanged();
            (CancelCommand          as RelayCommand)?.RaiseCanExecuteChanged();
            (StartMakroCommand      as RelayCommand)?.RaiseCanExecuteChanged();
            (StopMakroCommand       as RelayCommand)?.RaiseCanExecuteChanged();
            (RecordStepsCommand     as RelayCommand)?.RaiseCanExecuteChanged();
            (CaptureClickCommand    as RelayCommand)?.RaiseCanExecuteChanged();
            (PreviewOverviewCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (PreviewPlaybackCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (PreviewStopCommand     as RelayCommand)?.RaiseCanExecuteChanged();
            (EditStepCommand        as RelayCommand<MakroBefehl?>)?.RaiseCanExecuteChanged();
            (MoveStepUpCommand      as RelayCommand<MakroBefehl?>)?.RaiseCanExecuteChanged();
            (MoveStepDownCommand    as RelayCommand<MakroBefehl?>)?.RaiseCanExecuteChanged();
            (DeleteStepCommand      as RelayCommand<MakroBefehl?>)?.RaiseCanExecuteChanged();
            (DuplicateStepCommand   as RelayCommand<MakroBefehl?>)?.RaiseCanExecuteChanged();
        }
    }

    internal static class JsonOptions
    {
        public static readonly System.Text.Json.JsonSerializerOptions Default = new()
        {
            WriteIndented = false,
            ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };
    }
}
