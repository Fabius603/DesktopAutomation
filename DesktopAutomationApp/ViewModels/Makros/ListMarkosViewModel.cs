using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesktopAutomationApp.ViewModels
{
    public sealed class ListMakrosViewModel : ViewModelBase
    {
        public ObservableCollection<string> Items { get; } = new()
        {
            "Fenster anordnen",
            "Screenshot & Ablage",
            "Datenexport CSV"
        };

        public string Title => "Makros";
        public string Description => "Vorhandene Makros";
    }
}
