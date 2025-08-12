using Microsoft.Extensions.Logging;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using TaskAutomation.Jobs;
using TaskAutomation.Makros;

namespace DesktopAutomationApp.ViewModels
{
    public sealed class ListMakrosViewModel : ViewModelBase
    {
        private readonly ILogger<ListMakrosViewModel> _log;
        private readonly IJobExecutor _executor;

        public string Title => "Makros";
        public string Description => "Verfügbare Makros";

        public ObservableCollection<Makro> Items { get; } = new();
        private Makro? _selected;
        public Makro? Selected
        {
            get => _selected;
            set { _selected = value; OnPropertyChanged(); }
        }

        public ICommand RefreshCommand { get; }
        public ICommand CopyNameCommand { get; }

        public ListMakrosViewModel(IJobExecutor executor, ILogger<ListMakrosViewModel> log)
        {
            _executor = executor;
            _log = log;

            RefreshCommand = new RelayCommand(LoadMakros);
            CopyNameCommand = new RelayCommand<Makro?>(m =>
            {
                if (m?.Name is { Length: > 0 })
                    System.Windows.Clipboard.SetText(m.Name);
            }, m => m != null);

            LoadMakros();
        }

        private async void LoadMakros()
        {
            await _executor.ReloadMakrosAsync();

            Items.Clear();
            foreach (var m in _executor.AllMakros.Values.OrderBy(m => m.Name))
                Items.Add(m);

            Selected = Items.FirstOrDefault();
            _log.LogInformation("Makros geladen: {Count}", Items.Count);
        }
    }
}
