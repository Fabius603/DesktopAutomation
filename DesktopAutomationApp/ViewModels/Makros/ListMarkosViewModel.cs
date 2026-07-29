using DesktopAutomation.Application.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TaskAutomation.Jobs;
using TaskAutomation.Makros;
using TaskAutomation.Orchestration;
using DesktopAutomationApp.Localization;
using DesktopAutomation.Application.Organization;
using DesktopAutomationApp.Settings;
using DesktopAutomationApp.ViewModels.Library;

namespace DesktopAutomationApp.ViewModels
{
    public sealed class ListMakrosViewModel : ViewModelBase
    {
        private readonly ILogger<ListMakrosViewModel> _log;
        private readonly IJobExecutor _executor;
        private readonly IMakroApplicationService _makroAppService;
        private readonly IDialogService _dialogService;
        private readonly IJobDispatcher _dispatcher;
        private readonly ILibraryOrganizationService _libraryOrganization;

        public string Title => "Makros";

        public ObservableCollection<Makro> Items { get; } = new();
        public LibraryTreeViewModel Library { get; }

        private IReadOnlyCollection<Guid> _runningMakroIds = Array.Empty<Guid>();
        public IReadOnlyCollection<Guid> RunningMakroIds
        {
            get => _runningMakroIds;
            private set { _runningMakroIds = value; OnPropertyChanged(); }
        }

        private readonly List<Makro> _selectedItems = new();
        public IReadOnlyList<Makro> SelectedItems => _selectedItems;

        public void SetSelectedItems(IEnumerable<Makro> items)
        {
            _selectedItems.Clear();
            _selectedItems.AddRange(items);
            InvalidateAllCommands();
        }

        public void ClearSelection()
        {
            Selected = null;
            SetSelectedItems(Array.Empty<Makro>());
        }

        private Makro? _selected;
        public Makro? Selected
        {
            get => _selected;
            set { _selected = value; OnPropertyChanged(); InvalidateAllCommands(); }
        }

        public ICommand RefreshCommand { get; }
        public ICommand SaveAllCommand { get; }
        public ICommand NewMakroCommand { get; }
        public ICommand DeleteMakroCommand { get; }
        public ICommand OpenMakroCommand { get; }
        public ICommand StartMakroCommand { get; }
        public ICommand StopMakroCommand { get; }
        public ICommand OpenFolderCommand { get; }

        public event Action<Makro>? RequestOpenMakro;

        public ListMakrosViewModel(
            IJobExecutor executor,
            ILogger<ListMakrosViewModel> log,
            IMakroApplicationService makroAppService,
            IDialogService dialogService,
            IJobDispatcher dispatcher,
            ILibraryOrganizationService libraryOrganization,
            IUserPreferencesService preferences)
        {
            _executor = executor;
            _log = log;
            _makroAppService = makroAppService;
            _dialogService = dialogService;
            _dispatcher = dispatcher;
            _libraryOrganization = libraryOrganization;
            Library = new LibraryTreeViewModel(
                libraryOrganization, dialogService, preferences, LibraryItemKind.Makro,
                Loc.Get("Ui.Macro.List.NewMacro"));
            Library.RequestCreateItem += CreateNewMakroInFolderAsync;

            RefreshCommand   = new RelayCommand(async () => await RefreshAsync());
            SaveAllCommand   = new RelayCommand(async () => await SaveAllAsync(), () => Items.Count > 0);
            NewMakroCommand  = new RelayCommand(CreateNewMakro);
            DeleteMakroCommand = new RelayCommand(async () => await DeleteSelectedAsync(), () => _selectedItems.Count > 0);
            OpenMakroCommand = new RelayCommand<Makro?>(m =>
            {
                if (m != null) RequestOpenMakro?.Invoke(m);
            }, m => m != null);
            StartMakroCommand = new RelayCommand<object?>(param =>
            {
                if (param is Guid id) _dispatcher.StartMakro(id);
            });
            StopMakroCommand = new RelayCommand<object?>(param =>
            {
                if (param is Guid id) _dispatcher.CancelMakro(id);
            });
            OpenFolderCommand = new RelayCommand(() =>
                Process.Start(new ProcessStartInfo(_makroAppService.GetStoragePath()) { UseShellExecute = true }));

            _dispatcher.RunningMakrosChanged += OnRunningMakrosChanged;

            _ = RefreshAsync();
        }


        public async Task RefreshAsync()
        {
            await _makroAppService.ReloadAsync();

            Items.Clear();
            foreach (var m in _makroAppService.Makros.Values.OrderBy(m => m.Name))
                Items.Add(m);
            await Library.SetItemsAsync(Items.Select(CreateLibraryDescriptor));

            _log.LogInformation("Makros geladen: {Count}", Items.Count);
        }

        public async Task SaveAllAsync()
        {
            foreach (var m in Items) await _makroAppService.SaveMakroAsync(m);
            _log.LogInformation("Makros gespeichert: {Count}", Items.Count);
        }

        private void CreateNewMakro() => _ = CreateNewMakroInFolderAsync(Library.SelectedFolderId);

        private async Task CreateNewMakroInFolderAsync(Guid? folderId)
        {
            var name = await _dialogService.AskForNameAsync(Loc.Get("Macro.New.Title"), Loc.Get("Macro.New.Prompt"));
            if (name == null) return;

            try
            {
                var newMakro = await _makroAppService.CreateMakroAsync(name);
                Items.Add(newMakro);
                if (folderId.HasValue)
                    await _libraryOrganization.PlaceItemAsync(LibraryItemKind.Makro, newMakro.Id, folderId);
                await Library.SetItemsAsync(Items.Select(CreateLibraryDescriptor));
                Selected = newMakro;
                RequestOpenMakro?.Invoke(newMakro);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Fehler beim Erstellen eines neuen Makros");
            }
        }

        private async Task DeleteSelectedAsync()
        {
            if (_selectedItems.Count == 0) return;

            var message = _selectedItems.Count == 1
                ? Loc.Format("Macro.Delete.One", _selectedItems[0].Name)
                : Loc.Format("Macro.Delete.Many", _selectedItems.Count);

            var confirmed = await _dialogService.ConfirmAsync(message, Loc.Get("Dialog.Delete.Title"));
            if (!confirmed) return;

            var toDelete = _selectedItems.ToList();
            foreach (var makro in toDelete)
            {
                Items.Remove(makro);
                await _makroAppService.DeleteMakroAsync(makro.Id);
                _log.LogInformation("Makro gelöscht: {Name}", makro.Name);
            }
            Selected = null;
        }

        private void OnRunningMakrosChanged()
        {
            // Snapshot auf TP-Thread – nur das Ergebnis per InvokeAsync an UI übergeben.
            var ids = new HashSet<Guid>(_dispatcher.RunningMakroIds);
            Application.Current?.Dispatcher?.InvokeAsync(() =>
            {
                RunningMakroIds = ids;
                Library.RefreshItemStates();
            });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _dispatcher.RunningMakrosChanged -= OnRunningMakrosChanged;
            base.Dispose(disposing);
        }

        // ---------- Command invalidation helper ----------
        private void InvalidateAllCommands()
        {
            (SaveAllCommand     as RelayCommand)?.RaiseCanExecuteChanged();
            (DeleteMakroCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (OpenMakroCommand   as RelayCommand<Makro?>)?.RaiseCanExecuteChanged();
        }

        private LibraryItemDescriptor CreateLibraryDescriptor(Makro makro) => new()
        {
            Id = makro.Id,
            Name = makro.Name,
            Subtitle = Loc.Format("Ui.Library.MacroSubtitle", makro.Befehle.Count),
            Model = makro,
            Open = () => RequestOpenMakro?.Invoke(makro),
            Execute = () => _dispatcher.StartMakro(makro.Id),
            Stop = () => _dispatcher.CancelMakro(makro.Id),
            IsRunning = () => RunningMakroIds.Contains(makro.Id),
            DeleteAsync = () => DeleteMakroFromLibraryAsync(makro)
        };

        private async Task<bool> DeleteMakroFromLibraryAsync(Makro makro)
        {
            if (!await _dialogService.ConfirmAsync(
                    Loc.Format("Macro.Delete.One", makro.Name),
                    Loc.Get("Dialog.Delete.Title"))) return false;
            Items.Remove(makro);
            await _makroAppService.DeleteMakroAsync(makro.Id);
            return true;
        }
    }
}
