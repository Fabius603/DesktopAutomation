using DesktopAutomationApp.Services;
using DesktopAutomationApp.Views;
using Microsoft.Extensions.Logging;
using System;
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
            DeleteMakroCommand = new RelayCommand(async () => await DeleteSelectedAsync(), () => Selected != null);
            OpenMakroCommand = new RelayCommand<Makro?>(m => { if (m != null) RequestOpenMakro?.Invoke(m); }, m => m != null);

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

        public async Task SaveSingleAsync(Makro m) => await _repositoryService.SaveAsync(m);

        public async Task EnsureUniqueNameFor(Makro m)
        {
            var result = await _repositoryService.EnsureUniqueNameAsync(
                m,
                x => x.Name,
                (x, name) => x.Name = name,
                x => x.Id.ToString(),
                x => x.Name ?? ""
            );

            if (result.changed)
                await _repositoryService.SaveAsync(m);
        }

        private async void CreateNewMakro()
        {
            var dlg = new NewItemNameDialog("Neues Makro", "Name des neuen Makros:")
                { Owner = Application.Current.MainWindow };
            if (dlg.ShowDialog() != true) return;

            try
            {
                var newMakro = await _repositoryService.CreateNewAsync<Makro>(
                    dlg.ResultName,
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

        private async Task DeleteSelectedAsync()
        {
            if (Selected == null) return;

            var result = MessageBox.Show(
                $"Moechten Sie den Makro '{Selected.Name}' wirklich loeschen?",
                "Loeschen bestaetigen",
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
    }
}
