using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;
using DesktopAutomation.Application.Interfaces;
using DesktopAutomationApp.Models;
using Microsoft.Extensions.Logging;

namespace DesktopAutomationApp.ViewModels
{
    public sealed class ListAutomationsViewModel : ViewModelBase
    {
        private readonly IAutomationApplicationService _automationAppService;
        private readonly IDialogService _dialogService;
        private readonly ILogger<ListAutomationsViewModel> _log;

        private readonly List<EditableAutomation> _selectedItems = new();
        private EditableAutomation? _selected;

        public ObservableCollection<EditableAutomation> Items { get; } = new();
        public IReadOnlyList<EditableAutomation> SelectedItems => _selectedItems;

        public EditableAutomation? Selected
        {
            get => _selected;
            set { _selected = value; OnPropertyChanged(); InvalidateAllCommands(); }
        }

        public ICommand RefreshCommand { get; }
        public ICommand NewCommand { get; }
        public ICommand OpenCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand OpenFolderCommand { get; }

        public event Action<EditableAutomation>? RequestOpenAutomation;

        public ListAutomationsViewModel(
            IAutomationApplicationService automationAppService,
            IDialogService dialogService,
            ILogger<ListAutomationsViewModel> log)
        {
            _automationAppService = automationAppService;
            _dialogService = dialogService;
            _log = log;

            RefreshCommand = new RelayCommand(async () => await RefreshAllAsync());
            NewCommand = new RelayCommand(async () => await NewAutomationAsync());
            OpenCommand = new RelayCommand<EditableAutomation?>(a =>
            {
                if (a != null) RequestOpenAutomation?.Invoke(a);
            }, a => a != null);
            DeleteCommand = new RelayCommand(async () => await DeleteSelectedAsync(), () => _selectedItems.Count > 0);
            OpenFolderCommand = new RelayCommand(() =>
                Process.Start(new ProcessStartInfo(_automationAppService.GetStoragePath()) { UseShellExecute = true }));

            _ = InitialLoadAsync();
        }

        public void SetSelectedItems(IEnumerable<EditableAutomation> items)
        {
            _selectedItems.Clear();
            _selectedItems.AddRange(items);
            InvalidateAllCommands();
        }

        private async Task InitialLoadAsync() => await RefreshAllAsync();

        private async Task RefreshAllAsync()
        {
            var automations = await _automationAppService.LoadAllAsync();
            Items.Clear();

            foreach (var automation in automations.OrderBy(a => a.Name))
            {
                var editable = EditableAutomation.FromDomain(automation);
                editable.PropertyChanged += OnAutomationActiveChanged;
                Items.Add(editable);
            }

            _log.LogInformation("Automationen geladen: {Count}", Items.Count);
            OnPropertyChanged(nameof(Items));
        }

        private async Task NewAutomationAsync()
        {
            var name = await _dialogService.AskForNameAsync("Neue Automation", "Name der neuen Automation:");
            if (string.IsNullOrWhiteSpace(name)) return;

            var automation = new EditableAutomation
            {
                Name = name.Trim(),
                Description = string.Empty,
                Active = true
            };

            Selected = automation;
            RequestOpenAutomation?.Invoke(automation);
        }

        private async Task DeleteSelectedAsync()
        {
            if (_selectedItems.Count == 0) return;

            var message = _selectedItems.Count == 1
                ? $"Möchten Sie die Automation '{_selectedItems[0].Name}' wirklich löschen?"
                : $"Möchten Sie die {_selectedItems.Count} ausgewählten Automationen wirklich löschen?";

            if (!await _dialogService.ConfirmAsync(message, "Löschen bestätigen")) return;

            var toDelete = _selectedItems.ToList();
            foreach (var automation in toDelete)
            {
                Items.Remove(automation);
                await _automationAppService.DeleteAsync(automation.Id);
            }

            Selected = null;
            _selectedItems.Clear();
        }

        private async void OnAutomationActiveChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(EditableAutomation.Active)) return;
            if (sender is not EditableAutomation automation) return;

            // TODO Automation: Bei echter Runtime nach Active-Änderung die AutomationEngine neu laden.
            await _automationAppService.SaveAsync(automation.ToDomain());
            _log.LogInformation("Automation Active-Status gespeichert: {Name}", automation.Name);
        }

        private void InvalidateAllCommands()
        {
            (DeleteCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (OpenCommand as RelayCommand<EditableAutomation?>)?.RaiseCanExecuteChanged();
        }
    }
}
