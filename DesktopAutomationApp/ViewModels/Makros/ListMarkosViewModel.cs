using DesktopAutomationApp.Services;
using DesktopAutomationApp.Views;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TaskAutomation.Jobs;
using TaskAutomation.Makros;

namespace DesktopAutomationApp.ViewModels
{
    public sealed class ListMakrosViewModel : ViewModelBase
    {
        private readonly ILogger<ListMakrosViewModel> _log;
        private readonly IJobExecutor _executor;
        private readonly IRepositoryService _repositoryService;

        public string Title => "Makros";

        public ObservableCollection<Makro> Items { get; } = new();

        private readonly List<Makro> _selectedItems = new();
        public IReadOnlyList<Makro> SelectedItems => _selectedItems;

        public void SetSelectedItems(IEnumerable<Makro> items)
        {
            _selectedItems.Clear();
            _selectedItems.AddRange(items);
            CommandManager.InvalidateRequerySuggested();
        }

        private Makro? _selected;
        public Makro? Selected
        {
            get => _selected;
            set { _selected = value; OnPropertyChanged(); CommandManager.InvalidateRequerySuggested(); }
        }

        public ICommand RefreshCommand { get; }
        public ICommand SaveAllCommand { get; }
        public ICommand NewMakroCommand { get; }
        public ICommand DeleteMakroCommand { get; }
        public ICommand OpenMakroCommand { get; }

        public event Action<Makro>? RequestOpenMakro;

        public ListMakrosViewModel(
            IJobExecutor executor,
            ILogger<ListMakrosViewModel> log,
            IRepositoryService repositoryService)
        {
            _executor = executor;
            _log = log;
            _repositoryService = repositoryService;

            RefreshCommand   = new RelayCommand(LoadMakros);
            SaveAllCommand   = new RelayCommand(async () => await SaveAllAsync(), () => Items.Count > 0);
            NewMakroCommand  = new RelayCommand(CreateNewMakro);
            DeleteMakroCommand = new RelayCommand(async () => await DeleteSelectedAsync(), () => _selectedItems.Count > 0);
            OpenMakroCommand = new RelayCommand<Makro?>(m => 
            { 
                if (m != null) RequestOpenMakro?.Invoke(m); 
            }, m => m != null);
            LoadMakros();
        }

        private async void LoadMakros()
        {
            await _executor.ReloadMakrosAsync();

            Items.Clear();
            foreach (var m in _executor.AllMakros.Values.OrderBy(m => m.Name))
                Items.Add(m);

            _log.LogInformation("Makros geladen: {Count}", Items.Count);
        }

        public async Task SaveAllAsync()
        {
            await _repositoryService.SaveAllAsync(Items);
            _log.LogInformation("Makros gespeichert: {Count}", Items.Count);
            await _executor.ReloadMakrosAsync();
        }

        private async void CreateNewMakro()
        {
            var dlg = new NewItemNameDialog("Neues Makro", "Name des neuen Makros:")
                { Owner = Application.Current.MainWindow };
            if (dlg.ShowDialog() != true) return;

            try
            {
                var newMakro = new Makro { Name = dlg.ResultName, Befehle = new() };
                await _repositoryService.SaveAsync(newMakro);
                Items.Add(newMakro);
                Selected = newMakro;
                await _executor.ReloadMakrosAsync();
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
                ? $"Möchten Sie den Makro '{_selectedItems[0].Name}' wirklich löschen?"
                : $"Möchten Sie die {_selectedItems.Count} ausgewählten Makros wirklich löschen?";

            var result = AppDialog.Show(message, "Löschen bestätigen", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            var toDelete = _selectedItems.ToList();
            foreach (var makro in toDelete)
            {
                Items.Remove(makro);
                await _repositoryService.DeleteAsync<Makro>(makro.Id.ToString());
                _log.LogInformation("Makro gelöscht: {Name}", makro.Name);
            }
            Selected = null;
        }
    }
}
