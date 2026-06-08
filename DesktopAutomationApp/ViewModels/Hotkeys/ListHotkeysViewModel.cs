using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using DesktopAutomation.Application.Interfaces;
using DesktopAutomationApp.Models;
using Microsoft.Extensions.Logging;
using TaskAutomation.Hotkeys;
using TaskAutomation.Jobs;

namespace DesktopAutomationApp.ViewModels
{
    public sealed class ListHotkeysViewModel : ViewModelBase
    {
        private readonly IHotkeyApplicationService _hotkeyAppService;
        private readonly IDialogService _dialogService;
        private readonly IJobApplicationService _jobAppService;
        private readonly IGlobalHotkeyService _capture;
        private readonly ILogger<ListHotkeysViewModel> _log;

        public ObservableCollection<EditableHotkey> Items { get; } = new();
        public ObservableCollection<Job> Jobs { get; } = new();

        private readonly List<EditableHotkey> _selectedItems = new();
        public IReadOnlyList<EditableHotkey> SelectedItems => _selectedItems;

        public void SetSelectedItems(IEnumerable<EditableHotkey> items)
        {
            _selectedItems.Clear();
            _selectedItems.AddRange(items);
            InvalidateAllCommands();
        }

        private EditableHotkey? _selected;
        public EditableHotkey? Selected
        {
            get => _selected;
            set { _selected = value; OnPropertyChanged(); InvalidateAllCommands(); }
        }

        public event Action<EditableHotkey>? RequestOpenHotkey;

        // Commands
        public ICommand RefreshCommand { get; }
        public ICommand NewCommand { get; }
        public ICommand OpenCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand OpenFolderCommand { get; }

        public ListHotkeysViewModel(
            IHotkeyApplicationService hotkeyAppService,
            IDialogService dialogService,
            IJobApplicationService jobAppService,
            IGlobalHotkeyService capture,
            ILogger<ListHotkeysViewModel> log)
        {
            _hotkeyAppService = hotkeyAppService;
            _dialogService = dialogService;
            _jobAppService = jobAppService;
            _capture = capture;
            _log = log;

            RefreshCommand = new RelayCommand(async () => await RefreshAllAsync());
            NewCommand     = new RelayCommand(async () => await NewHotkeyAsync());
            OpenCommand    = new RelayCommand<EditableHotkey?>(h =>
            {
                if (h != null) RequestOpenHotkey?.Invoke(h);
            }, h => h != null);
            DeleteCommand  = new RelayCommand(async () => await DeleteSelectedAsync(), () => _selectedItems.Count > 0);
            OpenFolderCommand = new RelayCommand(() =>
                Process.Start(new ProcessStartInfo(_hotkeyAppService.GetStoragePath()) { UseShellExecute = true }));

            _ = InitialLoadAsync();
        }

        private async Task InitialLoadAsync()
        {
            LoadJobs();

            var list = await _hotkeyAppService.LoadAllAsync();
            Items.Clear();
            foreach (var hk in list.OrderBy(h => h.Name))
            {
                var ehk = EditableHotkey.FromDomain(hk);
                ehk.Job.SetJobNameResolver(GetCurrentJobNameForAction);
                Items.Add(ehk);
                ehk.PropertyChanged += OnHotkeyActiveChanged;
            }
            _log.LogInformation("Hotkeys initial geladen: {HotkeyCount} Hotkeys, {JobCount} Jobs", Items.Count, Jobs.Count);
        }

        private async Task RefreshAllAsync()
        {
            LoadJobs();

            var list = await _hotkeyAppService.LoadAllAsync();
            Items.Clear();
            foreach (var hk in list.OrderBy(h => h.Name))
            {
                var ehk = EditableHotkey.FromDomain(hk);
                ehk.Job.SetJobNameResolver(GetCurrentJobNameForAction);
                Items.Add(ehk);
                ehk.PropertyChanged += OnHotkeyActiveChanged;
            }
            _log.LogInformation("Hotkeys und Jobs neu geladen: {HotkeyCount} Hotkeys, {JobCount} Jobs", Items.Count, Jobs.Count);
        }

        private async Task NewHotkeyAsync()
        {
            var name = await _dialogService.AskForNameAsync("Neuer Hotkey", "Name des neuen Hotkeys:");
            if (string.IsNullOrWhiteSpace(name)) return;

            var e = new EditableHotkey
            {
                Name = name,
                Modifiers = KeyModifiers.None,
                VirtualKeyCode = 0,
                Job = new EditableJobReference { Name = "", Command = ActionCommand.Toggle },
                Active = true
            };
            e.Job.SetJobNameResolver(GetCurrentJobNameForAction);

            Selected = e;

            RequestOpenHotkey?.Invoke(e);
        }

        private async Task DeleteSelectedAsync()
        {
            if (_selectedItems.Count == 0) return;

            var message = _selectedItems.Count == 1
                ? $"Möchten Sie den Hotkey '{_selectedItems[0].Name}' wirklich löschen?"
                : $"Möchten Sie die {_selectedItems.Count} ausgewählten Hotkeys wirklich löschen?";

            if (!await _dialogService.ConfirmAsync(message, "Löschen bestätigen")) return;

            var toDelete = _selectedItems.ToList();
            foreach (var hotkey in toDelete)
            {
                _capture.UnregisterHotkey(hotkey.Id);
                Items.Remove(hotkey);
                await _hotkeyAppService.DeleteAsync(hotkey.Id);
                _log.LogInformation("Hotkey gelöscht und Registrierung aufgehoben: {Name}", hotkey.Name);
            }
            Selected = null;
        }

        /// <summary>
        /// Auto-save when Active checkbox is toggled directly in the list.
        /// </summary>
        private async void OnHotkeyActiveChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(EditableHotkey.Active)) return;
            if (sender is not EditableHotkey hk) return;

            var error = ValidateEdited(hk);
            if (error != null) { _log.LogWarning("Hotkey ungültig: {Error}", error); return; }

            if (hk.Job.JobId.HasValue)
            {
                var currentJobName = GetCurrentJobNameForAction(hk.Job);
                if (!currentJobName.StartsWith("[Job nicht gefunden"))
                    hk.Job.Name = currentJobName;
            }

            await _hotkeyAppService.SaveAsync(hk.ToDomain());
            _log.LogInformation("Hotkey Active-Status gespeichert");
            await _capture.ReloadFromRepositoryAsync();
        }

        private void LoadJobs()
        {
            Jobs.Clear();
            foreach (var j in _jobAppService.Jobs.Values.OrderBy(j => j.Name))
                Jobs.Add(j);

            foreach (var hotkey in Items)
                hotkey.Job.SetJobNameResolver(GetCurrentJobNameForAction);
        }

        public string GetCurrentJobNameForAction(EditableJobReference job)
        {
            if (job?.JobId.HasValue == true)
            {
                var j = Jobs.FirstOrDefault(x => x.Id == job.JobId.Value);
                if (j != null) return j.Name;
                _log.LogWarning("Job mit ID {JobId} nicht gefunden", job.JobId);
                return $"[Job nicht gefunden: {job.JobId}]";
            }
            return job?.Name ?? string.Empty;
        }

        private static string? ValidateEdited(EditableHotkey hk)
        {
            if (string.IsNullOrWhiteSpace(hk.Name)) return "Name ist erforderlich.";
            if (string.IsNullOrWhiteSpace(hk.Job?.Name)) return "Job ist erforderlich.";
            if (hk.VirtualKeyCode == 0) return "Bitte eine Tastenkombination erfassen.";
            return null;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        // ---------- Command invalidation helper ----------
        private void InvalidateAllCommands()
        {
            (DeleteCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (OpenCommand   as RelayCommand<EditableHotkey?>)?.RaiseCanExecuteChanged();
        }
    }
}
