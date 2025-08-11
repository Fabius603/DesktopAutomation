using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesktopAutomationApp.ViewModels
{
    public sealed class ListJobsViewModel : ViewModelBase
    {
        public ObservableCollection<string> Items { get; } = new()
        {
            "DesktopVideo",
            "MakroTest",
            "Daily Report"
        };

        public string Title => "Jobs";
        public string Description => "Geplante/ausführbare Jobs";
    }
}
