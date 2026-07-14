using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Threading;
using DesktopAutomation.Application.Interfaces;
using DesktopAutomationApp.Models;
using DesktopAutomationApp.Localization;
using Microsoft.Extensions.Logging;

namespace DesktopAutomationApp.ViewModels
{
    public sealed class ListAutomationsViewModel : ViewModelBase
    {
        private readonly IAutomationApplicationService _automationAppService;
        private readonly IDialogService _dialogService;
        private readonly ILogger<ListAutomationsViewModel> _log;
        private readonly DispatcherTimer _relativeTimeTimer;

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
        public ICommand RunNowCommand { get; }
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
            RunNowCommand = new RelayCommand<EditableAutomation?>(async automation =>
            {
                if (automation != null) await _automationAppService.TriggerAsync(automation.Id);
                await RefreshAllAsync();
            }, automation => automation != null && automation.Active);
            OpenFolderCommand = new RelayCommand(() =>
                Process.Start(new ProcessStartInfo(_automationAppService.GetStoragePath()) { UseShellExecute = true }));

            LocalizationService.Instance.CultureChanged += OnCultureChanged;

            _relativeTimeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _relativeTimeTimer.Tick += OnRelativeTimeTick;
            _relativeTimeTimer.Start();

            _ = InitialLoadAsync();
        }

        private void OnCultureChanged(object? sender, EventArgs e)
        {
            foreach (var automation in Items)
                automation.RefreshLocalizedDisplayProperties();
        }

        private void OnRelativeTimeTick(object? sender, EventArgs e)
        {
            foreach (var automation in Items)
                automation.RefreshLocalizedDisplayProperties();
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
            var name = await _dialogService.AskForNameAsync(Loc.Get("Automation.New.Title"), Loc.Get("Automation.New.Prompt"));
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
                ? Loc.Format("Automation.Delete.One", _selectedItems[0].Name)
                : Loc.Format("Automation.Delete.Many", _selectedItems.Count);

            if (!await _dialogService.ConfirmAsync(message, Loc.Get("Dialog.Delete.Title"))) return;

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

            await _automationAppService.SaveAsync(automation.ToDomain());
            _log.LogInformation("Automation Active-Status gespeichert: {Name}", automation.Name);
        }

        private void InvalidateAllCommands()
        {
            (DeleteCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (OpenCommand as RelayCommand<EditableAutomation?>)?.RaiseCanExecuteChanged();
            (RunNowCommand as RelayCommand<EditableAutomation?>)?.RaiseCanExecuteChanged();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                LocalizationService.Instance.CultureChanged -= OnCultureChanged;
                _relativeTimeTimer.Stop();
                _relativeTimeTimer.Tick -= OnRelativeTimeTick;
            }
            base.Dispose(disposing);
        }
    }
}
