using DesktopAutomationApp.Services.Preview;
using DesktopOverlay;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TaskAutomation.Jobs;
using TaskAutomation.Makros;
using ImageHelperMethods;
using TaskAutomation.Persistence; // <— für IJsonRepository<Makro>

namespace DesktopAutomationApp.ViewModels
{
    public sealed class ListMakrosViewModel : ViewModelBase, IDisposable
    {
        private readonly ILogger<ListMakrosViewModel> _log;
        private readonly IJobExecutor _executor;
        private readonly IMacroPreviewService _preview;
        private readonly IJsonRepository<Makro> _makroRepo;  // <— Neu: Speichern/Laden
        private Overlay _overlay;
        private MacroPreviewService.PreviewResult _lastPreview;

        public string Title => "Makros";
        public string Description => "Verfügbare Makros";

        public ObservableCollection<Makro> Items { get; } = new();

        private Makro? _selected;
        public Makro? Selected
        {
            get => _selected;
            set { _selected = value; OnPropertyChanged(); OnPropertyChanged(nameof(SelectedSteps)); }
        }

        public MakroBefehl? SelectedStep
        {
            get => _selectedStep;
            set { _selectedStep = value; OnPropertyChanged(); }
        }
        private MakroBefehl? _selectedStep;

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

        // Aufnahme (vorerst ohne Funktion)
        public ICommand RecordStepsCommand { get; }

        // Vorschau
        public ICommand CopyNameCommand { get; }
        public ICommand PreviewOverviewCommand { get; }
        public ICommand PreviewPlaybackCommand { get; }
        public ICommand PreviewStopCommand { get; }

        public ListMakrosViewModel(
            IJobExecutor executor,
            ILogger<ListMakrosViewModel> log,
            IMacroPreviewService preview,
            IJsonRepository<Makro> makroRepo)   // <— über DI registrieren
        {
            _executor = executor;
            _log = log;
            _preview = preview;
            _makroRepo = makroRepo;

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

            RecordStepsCommand = new RelayCommand(() => { /* absichtlich leer (Platzhalter) */ });

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

        private async Task SaveAllAsync()
        {
            // Persistiert alle Makros gesammelt
            await _makroRepo.SaveAllAsync(Items);
            _log.LogInformation("Makros gespeichert: {Count}", Items.Count);
            // Nach dem Speichern ggf. erneut in Executor laden:
            await _executor.ReloadMakrosAsync();
        }

        private void CreateNewMakro()
        {
            var name = UniqueName("NeuesMakro");
            var m = new Makro { Name = name, Befehle = new() };
            Items.Add(m);
            Selected = m;
        }

        private string UniqueName(string baseName)
        {
            var i = 1;
            var n = baseName;
            while (Items.Any(x => string.Equals(x.Name, n, StringComparison.OrdinalIgnoreCase)))
                n = $"{baseName}_{i++}";
            return n;
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
            await _makroRepo.DeleteAsync(name);

            _log.LogInformation("Hotkey gelöscht: {Name}", name);
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
            // einfache, robuste Variante über STJ-Serialize/Deserialize (nutzt Ihre Polymorphie-Optionen)
            var json = System.Text.Json.JsonSerializer.Serialize(s, JsonOptions.Default);
            return System.Text.Json.JsonSerializer.Deserialize<MakroBefehl>(json, JsonOptions.Default)!;
        }

        private void OpenAddStepDialog()
        {
            if (Selected == null) return;

            var dlgVm = new AddStepDialogViewModel(); // Standardwerte sind im VM gesetzt
            var dlg = new Views.AddStepDialog
            {
                Owner = System.Windows.Application.Current.MainWindow, // über dem Hauptfenster
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
