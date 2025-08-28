using DesktopAutomationApp.Services.Preview;
using DesktopOverlay;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Collections.Generic;
using TaskAutomation.Jobs;
using TaskAutomation.Makros;
using ImageHelperMethods;
using TaskAutomation.Persistence;
using DesktopAutomationApp.Views;
using TaskAutomation.Hotkeys;
using DesktopAutomationApp.Services;

namespace DesktopAutomationApp.ViewModels
{
    public sealed class ListMakrosViewModel : ViewModelBase, IDisposable
    {
        private readonly ILogger<ListMakrosViewModel> _log;
        private readonly IJobExecutor _executor;
        private readonly IMacroPreviewService _preview;
        private readonly IRepositoryService _repositoryService;
        private Overlay _overlay;
        private MacroPreviewService.PreviewResult _lastPreview;
        private readonly IGlobalHotkeyService _hotkeys;

        public string Title => "Makros";
        public string Description => "Verfügbare Makros";

        public ObservableCollection<Makro> Items { get; } = new();

        private Makro? _selected;
        public Makro? Selected
        {
            get => _selected;
            set { _selected = value; OnPropertyChanged(); OnPropertyChanged(nameof(SelectedSteps)); CommandManager.InvalidateRequerySuggested(); }
        }

        private MakroBefehl? _selectedStep;
        public MakroBefehl? SelectedStep
        {
            get => _selectedStep;
            set { _selectedStep = value; OnPropertyChanged(); }
        }

        public bool IsRecording
        {
            get => _isRecording;
            private set
            {
                if (_isRecording == value) return;
                _isRecording = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(RecordButtonText));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string RecordButtonText => IsRecording ? "Aufnahme stoppen" : "Aufnahme starten";

        public ObservableCollection<MakroBefehl>? SelectedSteps =>
            Selected?.Befehle != null ? new ObservableCollection<MakroBefehl>(Selected.Befehle) : null;

        // Commands: Laden / Speichern / Editieren
        public ICommand RefreshCommand { get; }
        public ICommand SaveAllCommand { get; }
        public ICommand ToggleEditModeCommand { get; }

        // Makro-CRUD
        public ICommand NewMakroCommand { get; }
        public ICommand DeleteMakroCommand { get; }

        // Steps: DnD/Manipulation
        public ICommand MoveStepCommand { get; }         // (int fromIdx, int toIdx)
        public ICommand DeleteStepCommand { get; }       // (MakroBefehl step)
        public ICommand DuplicateStepCommand { get; }    // (MakroBefehl step)
        public ICommand AddStepCommand { get; }          // öffnet Dialog
        public ICommand MoveStepUpCommand { get; }
        public ICommand MoveStepDownCommand { get; }
        public ICommand EditStepCommand { get; }

        // Aufnahme (jetzt aktiv)
        public ICommand RecordStepsCommand { get; }

        // Vorschau
        public ICommand CopyNameCommand { get; }
        public ICommand PreviewOverviewCommand { get; }
        public ICommand PreviewPlaybackCommand { get; }
        public ICommand PreviewStopCommand { get; }

        // Aufnahme-Status
        private bool _isRecording;

        public ListMakrosViewModel(
            IJobExecutor executor,
            ILogger<ListMakrosViewModel> log,
            IMacroPreviewService preview,
            IRepositoryService repositoryService,
            IGlobalHotkeyService hotkeys)
        {
            _executor = executor;
            _log = log;
            _preview = preview;
            _repositoryService = repositoryService;
            _hotkeys = hotkeys;

            RefreshCommand = new RelayCommand(LoadMakros);
            SaveAllCommand = new RelayCommand(async () => await SaveAllAsync(), () => Items.Count > 0);

            NewMakroCommand = new RelayCommand(CreateNewMakro);
            DeleteMakroCommand = new RelayCommand(async () => await DeleteSelectedAsync(), () => Selected != null);

            MoveStepCommand = new RelayCommand<(int from, int to)>(MoveStep);
            DeleteStepCommand = new RelayCommand<MakroBefehl?>(DeleteStep, s => Selected != null && s != null);
            DuplicateStepCommand = new RelayCommand<MakroBefehl?>(DuplicateStep, s => Selected != null && s != null);
            AddStepCommand = new RelayCommand(OpenAddStepDialog, () => Selected != null);
            MoveStepUpCommand = new RelayCommand<MakroBefehl?>(s => MoveRelative(s, -1), s => CanMoveRelative(s, -1));
            MoveStepDownCommand = new RelayCommand<MakroBefehl?>(s => MoveRelative(s, +1), s => CanMoveRelative(s, +1));
            EditStepCommand = new RelayCommand<MakroBefehl?>(EditStep, s => Selected != null && s != null);

            // Aufnahme-Button: Toggle Start/Stop
            RecordStepsCommand = new RelayCommand(async () => await ToggleRecordAsync(), () => Selected != null);

            CopyNameCommand = new RelayCommand<Makro?>(m =>
            {
                if (m?.Name is { Length: > 0 }) System.Windows.Clipboard.SetText(m.Name);
            }, m => m != null);

            PreviewOverviewCommand = new RelayCommand(ShowOverview, CanPreview);
            PreviewPlaybackCommand = new RelayCommand(() => ShowPlayback(), CanPreview);
            PreviewStopCommand = new RelayCommand(StopPreview, () => _overlay != null);

            LoadMakros();
        }

        private bool CanPreview() => Selected != null && (Selected.Befehle?.Count > 0);

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
            _lastPreview = _preview.Build(Selected!, v, v);
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

        private bool CanMoveRelative(MakroBefehl? step, int delta)
        {
            if (Selected?.Befehle == null || step == null) return false;
            var list = Selected.Befehle; // ideal: ObservableCollection<MakroBefehl>
            var idx = list.IndexOf(step);
            if (idx < 0) return false;
            var newIdx = idx + delta;
            return newIdx >= 0 && newIdx < list.Count;
        }

        private void MoveRelative(MakroBefehl? step, int delta)
        {
            if (Selected?.Befehle == null || step == null) return;
            var list = Selected.Befehle;
            var idx = list.IndexOf(step);
            if (idx < 0) return;
            var newIdx = idx + delta;
            if (newIdx < 0 || newIdx >= list.Count) return;

            // Element umsetzen
            list.RemoveAt(idx);
            list.Insert(newIdx, step);

            SelectedStep = step;
        }

        private void EditStep(MakroBefehl? step)
        {
            if (Selected?.Befehle == null || step == null) return;

            var list = Selected.Befehle; // ObservableCollection<MakroBefehl> empfohlen
            var index = list.IndexOf(step);
            if (index < 0) return;

            // Dialog-VM vorbereiten und mit aktuellen Werten füllen
            var vm = new AddStepDialogViewModel(_hotkeys) { Mode = StepDialogMode.Edit };
            Prefill(vm, step);

            // Dialog über dem Hauptfenster öffnen
            var dlg = new AddStepDialog
            {
                Owner = Application.Current.MainWindow,
                DataContext = vm,
            };

            var ok = dlg.ShowDialog() == true;
            if (!ok || vm.CreatedStep == null) return;

            // Ersetzen am selben Index
            Selected.Befehle[index] = vm.CreatedStep;

            // Auswahl halten
            SelectedStep = vm.CreatedStep;

            CommandManager.InvalidateRequerySuggested();
        }

        private async void LoadMakros()
        {
            await _executor.ReloadMakrosAsync();

            Items.Clear();
            foreach (var m in _executor.AllMakros.Values.OrderBy(m => m.Name))
                Items.Add(m);

            Selected = Items.FirstOrDefault();
            OnPropertyChanged(nameof(SelectedSteps));
            _log.LogInformation("Makros geladen: {Count}", Items.Count);
        }

        public async void EnsureUniqueNameFor(Makro? m)
        {
            if (m == null) return;

            try
            {
                await _repositoryService.EnsureUniqueNameAsync(
                    m,
                    makro => makro.Name ?? "",
                    (makro, name) => makro.Name = name,
                    makro => makro.Name);
                await _executor.ReloadMakrosAsync();
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Fehler beim Eindeutig-Machen des Makro-Namens");
            }
        }

        private static void Prefill(AddStepDialogViewModel vm, MakroBefehl step)
        {
            switch (step)
            {
                case MouseMoveBefehl mm:
                    vm.SelectedType = "MouseMove";
                    vm.X = mm.X; vm.Y = mm.Y;
                    break;

                case MouseDownBefehl md:
                    vm.SelectedType = "MouseDown";
                    vm.MouseButton = md.Button;
                    vm.X = md.X; vm.Y = md.Y;
                    break;

                case MouseUpBefehl mu:
                    vm.SelectedType = "MouseUp";
                    vm.MouseButton = mu.Button;
                    vm.X = mu.X; vm.Y = mu.Y;
                    break;

                case KeyDownBefehl kd:
                    vm.SelectedType = "KeyDown";
                    vm.Key = kd.Key;
                    break;

                case KeyUpBefehl ku:
                    vm.SelectedType = "KeyUp";
                    vm.Key = ku.Key;
                    break;

                case TimeoutBefehl to:
                    vm.SelectedType = "Timeout";
                    vm.Duration = to.Duration;
                    break;
            }
        }

        public async Task SaveAllAsync()
        {
            // Persistiert alle Makros gesammelt
            await _repositoryService.SaveAllAsync(Items);
            _log.LogInformation("Makros gespeichert: {Count}", Items.Count);
            // Nach dem Speichern ggf. erneut in Executor laden:
            await _executor.ReloadMakrosAsync();
        }

        private async void CreateNewMakro()
        {
            try
            {
                var newMakro = await _repositoryService.CreateNewAsync<Makro>(
                    "NeuesMakro",
                    name => new Makro { Name = name, Befehle = new() },
                    makro => makro.Name);

                Items.Add(newMakro);
                Selected = newMakro;
                await _executor.ReloadMakrosAsync();
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Fehler beim Erstellen eines neuen Makros");
            }
        }

        // --- Step-Manipulation ---
        private void MoveStep((int from, int to) args)
        {
            if (Selected?.Befehle == null) return;
            var list = Selected.Befehle;
            if (args.from < 0 || args.from >= list.Count || args.to < 0 || args.to >= list.Count) return;
            var item = list[args.from];
            list.RemoveAt(args.from);
            list.Insert(args.to, item);
            OnPropertyChanged(nameof(SelectedSteps));
        }

        private void DeleteStep(MakroBefehl? step)
        {
            if (Selected?.Befehle == null || step == null) return;
            Selected.Befehle.Remove(step);
            OnPropertyChanged(nameof(SelectedSteps));
        }

        private async Task DeleteSelectedAsync()
        {
            if (Selected == null) return;

            var result = MessageBox.Show(
                $"Möchten Sie den Makro „{Selected.Name}“ wirklich löschen?",
                "Löschen bestätigen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            var name = Selected.Name;
            var idx = Items.IndexOf(Selected);
            Items.Remove(Selected);
            Selected = Items.ElementAtOrDefault(Math.Max(0, idx - 1));
            await _repositoryService.DeleteAsync<Makro>(name);

            _log.LogInformation("Makro gelöscht: {Name}", name);
        }

        private void DuplicateStep(MakroBefehl? step)
        {
            if (Selected?.Befehle == null || step == null) return;
            var clone = CloneStep(step);
            Selected.Befehle.Add(clone);
            OnPropertyChanged(nameof(SelectedSteps));
        }

        private MakroBefehl CloneStep(MakroBefehl s)
        {
            // einfache, robuste Variante über STJ-Serialize/Deserialize (nutzt deine Polymorphie-Optionen)
            var json = System.Text.Json.JsonSerializer.Serialize(s, JsonOptions.Default);
            return System.Text.Json.JsonSerializer.Deserialize<MakroBefehl>(json, JsonOptions.Default)!;
        }

        private void OpenAddStepDialog()
        {
            if (Selected == null) return;

            var dlgVm = new AddStepDialogViewModel(_hotkeys) { Mode = StepDialogMode.Add }; // Standardwerte sind im VM gesetzt
            var dlg = new AddStepDialog
            {
                Owner = Application.Current.MainWindow, // über dem Hauptfenster
                DataContext = dlgVm
            };

            var result = dlg.ShowDialog();
            if (result == true && dlgVm.CreatedStep != null)
            {
                Selected.Befehle ??= new(); // ObservableCollection<MakroBefehl> empfohlen

                int insertIndex = SelectedStep != null
                    ? Math.Min(Selected.Befehle.Count, Selected.Befehle.IndexOf(SelectedStep) + 1)
                    : Selected.Befehle.Count;

                Selected.Befehle.Insert(insertIndex, dlgVm.CreatedStep);

                // neu eingefügten Step auswählen
                SelectedStep = dlgVm.CreatedStep;
            }
        }

        // ============================
        // Aufnahme-Implementierung (VM)
        // ============================
        private async Task ToggleRecordAsync()
        {
            if (Selected == null) return;

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

                    if (events == null || events.Count == 0)
                        return;

                    var mapped = MapCapturedEventsToSteps(events);

                    Selected.Befehle ??= new();
                    int insertIndex = SelectedStep != null
                        ? Math.Min(Selected.Befehle.Count, Selected.Befehle.IndexOf(SelectedStep) + 1)
                        : Selected.Befehle.Count;

                    foreach (var step in mapped)
                        Selected.Befehle.Insert(insertIndex++, step);

                    if (mapped.Count > 0)
                        SelectedStep = mapped.Last();

                    OnPropertyChanged(nameof(SelectedSteps));
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Fehler bei der Makro-Aufnahme.");
                IsRecording = false;
            }
        }

        private List<MakroBefehl> MapCapturedEventsToSteps(IReadOnlyList<CapturedInputEvent> events)
        {
            var list = new List<MakroBefehl>(events.Count);

            foreach (var ev in events)
            {
                switch (ev)
                {
                    case TimeoutEvent t:
                        if (t.Milliseconds > 0)
                            list.Add(new TimeoutBefehl { Duration = t.Milliseconds });
                        break;

                    case KeyDownCaptured kd:
                        list.Add(new KeyDownBefehl { Key = _hotkeys.FormatKey(KeyModifiers.None, kd.VirtualKey) });
                        break;

                    case KeyUpCaptured ku:
                        list.Add(new KeyUpBefehl { Key = _hotkeys.FormatKey(KeyModifiers.None, ku.VirtualKey) });
                        break;

                    case MouseDownCaptured md:
                        list.Add(new MouseDownBefehl
                        {
                            Button = _hotkeys.FormatMouseButton(md.Button),
                            X = md.X,
                            Y = md.Y
                        });
                        break;

                    case MouseUpCaptured mu:
                        list.Add(new MouseUpBefehl
                        {
                            Button = _hotkeys.FormatMouseButton(mu.Button),
                            X = mu.X,
                            Y = mu.Y
                        });
                        break;
                }
            }

            return list;
        }

        public void Dispose()
        {
            StopPreview();
            _overlay?.Dispose();
            _overlay = null;
        }
    }

    // Gemeinsame JsonOptions für CloneStep (nutzt vorhandene Konverter)
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
