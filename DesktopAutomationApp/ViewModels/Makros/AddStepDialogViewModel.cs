using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TaskAutomation.Makros;

namespace DesktopAutomationApp.ViewModels
{
    public sealed class AddStepDialogViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnChange([CallerMemberName] string? p = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));

        public IReadOnlyList<string> StepTypes { get; } = new[]
        {
            "MouseMove", "MouseDown", "MouseUp", "KeyDown", "KeyUp", "Timeout"
        };

        private string _selectedType = "MouseMove";
        public string SelectedType { get => _selectedType; set { _selectedType = value; OnChange(); } }

        // Gemeinsame Parameter (nur die benötigten Felder werden je Typ genutzt)
        public int X { get; set; }
        public int Y { get; set; }
        public string MouseButton { get; set; } = "Left";
        public string Key { get; set; } = "A";
        public int Duration { get; set; } = 100;

        public MakroBefehl? CreatedStep { get; private set; }

        public void CreateStep()
        {
            CreatedStep = SelectedType switch
            {
                "MouseMove" => new MouseMoveBefehl { X = X, Y = Y },
                "MouseDown" => new MouseDownBefehl { Button = MouseButton, X = X, Y = Y },
                "MouseUp" => new MouseUpBefehl { Button = MouseButton, X = X, Y = Y },
                "KeyDown" => new KeyDownBefehl { Key = Key },
                "KeyUp" => new KeyUpBefehl { Key = Key },
                "Timeout" => new TimeoutBefehl { Duration = Duration },
                _ => null
            };
        }
    }
}
