using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesktopAutomationApp.ViewModels
{
    public sealed class ListHotkeysViewModel : ViewModelBase
    {
        public ObservableCollection<string> Items { get; } = new()
        {
            "Strg + C — Kopieren",
            "Strg + V — Einfügen",
            "Strg + Shift + S — Speichern unter"
        };

        public string Title => "Hotkeys";
        public string Description => "Schnelltastenübersicht";
    }
}
