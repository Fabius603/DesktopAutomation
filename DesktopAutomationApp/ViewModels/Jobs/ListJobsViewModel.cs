using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using TaskAutomation.Jobs;

namespace DesktopAutomationApp.ViewModels
{
    public sealed class ListJobsViewModel : ViewModelBase
    {
        private readonly ILogger<ListJobsViewModel> _log;
        private readonly IJobExecutor _executor;

        public string Title => "Jobs";
        public string Description => "Verfügbare Jobs";

        public ObservableCollection<Job> Items { get; } = new();

        public ICommand RefreshCommand { get; }
        public ICommand OpenJobCommand { get; } // Parameter: Job

        public event Action<Job>? RequestOpenJob; // Signal an Host (MainViewModel)

        public ListJobsViewModel(IJobExecutor executor, ILogger<ListJobsViewModel> log)
        {
            _executor = executor;
            _log = log;

            RefreshCommand = new RelayCommand(LoadJobs);
            OpenJobCommand = new RelayCommand<Job?>(job =>
            {
                if (job != null) RequestOpenJob?.Invoke(job);
            }, job => job != null);

            LoadJobs();
        }

        private async void LoadJobs()
        {
            await _executor.ReloadJobsAsync();

            Items.Clear();
            foreach (var j in _executor.AllJobs.Values.OrderBy(j => j.Name))
                Items.Add(j);

            _log.LogInformation("Jobs geladen: {Count}", Items.Count);
        }
    }
}
