using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using TaskAutomation.Jobs;

namespace DesktopAutomationApp.ViewModels
{
    public sealed class JobStepsViewModel : ViewModelBase
    {
        public Job Job { get; }
        public string Title => Job.Name;

        // vorher: ObservableCollection<object>
        public ObservableCollection<JobStep> Steps { get; } = new();

        public ICommand BackCommand { get; }
        public event Action? RequestBack;

        public JobStepsViewModel(Job job)
        {
            Job = job;
            foreach (var s in job.Steps ?? Enumerable.Empty<JobStep>())
                Steps.Add(s);

            BackCommand = new RelayCommand(() => RequestBack?.Invoke());
        }
    }
}
