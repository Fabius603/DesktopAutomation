using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TaskAutomation.Hotkeys;
using TaskAutomation.Makros;

namespace DesktopAutomationApp.ViewModels
{
    public sealed class AddStepDialogViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnChange([CallerMemberName] string? p = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));

        private readonly IGlobalHotkeyService _capture;
        private CancellationTokenSource? _captureCts;

        public AddStepDialogViewModel(IGlobalHotkeyService capture)
        {
            _capture = capture;
            CaptureKeyCommand = new RelayCommand(async () => await CaptureKeyAsync(), () => ShowKey && !IsCapturing);
        }

        public ICommand CaptureKeyCommand { get; }

        public string[] StepTypes { get; } = { "MouseMoveAbsolute", "MouseMoveRelative", "MouseDown", "MouseUp", "KeyDown", "KeyUp", "Timeout" };

        private string _selectedType = "MouseMoveAbsolute";
        public string SelectedType
        {
            get => _selectedType;
            set
            {
                _selectedType = value;
                OnChange();
                OnChange(nameof(ShowMouseXY));
                OnChange(nameof(ShowMouseDelta));
                OnChange(nameof(ShowMouseButton));
                OnChange(nameof(ShowKey));
                OnChange(nameof(ShowDuration));
                // Command-Enable für CaptureButton neu bewerten
                (CaptureKeyCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        private StepDialogMode _mode = StepDialogMode.Add;
        public StepDialogMode Mode
        {
            get => _mode;
            set { _mode = value; OnChange(); OnChange(nameof(DialogTitle)); OnChange(nameof(ConfirmButtonText)); }
        }

        public string DialogTitle => Mode == StepDialogMode.Edit ? "Step anpassen" : "Neuen Step hinzufügen";
        public string ConfirmButtonText => Mode == StepDialogMode.Edit ? "Anpassen" : "Hinzufügen";

        // Sichtbarkeiten
        public bool ShowMouseXY => SelectedType is "MouseMoveAbsolute";
        public bool ShowMouseDelta => SelectedType is "MouseMoveRelative";
        public bool ShowMouseButton => SelectedType is "MouseDown" or "MouseUp";
        public bool ShowKey => SelectedType is "KeyDown" or "KeyUp";
        public bool ShowDuration => SelectedType is "Timeout";

        // Eingabefelder
        private int _x;
        public int X { get => _x; set { _x = value; OnChange(); } }

        private int _y;
        public int Y { get => _y; set { _y = value; OnChange(); } }

        private int _deltaX;
        public int DeltaX { get => _deltaX; set { _deltaX = value; OnChange(); } }

        private int _deltaY;
        public int DeltaY { get => _deltaY; set { _deltaY = value; OnChange(); } }

        private string _mouseButton = "Left";
        public string MouseButton { get => _mouseButton; set { _mouseButton = value; OnChange(); } }

        private string _key = string.Empty; // Anzeige, z. B. "Ctrl+Alt+M"
        public string Key { get => _key; set { _key = value; OnChange(); } }

        private int _duration = 100;
        public int Duration { get => _duration; set { _duration = value; OnChange(); } }

        private bool _isCapturing;
        public bool IsCapturing { get => _isCapturing; private set { _isCapturing = value; OnChange(); (CaptureKeyCommand as RelayCommand)?.RaiseCanExecuteChanged(); } }

        public MakroBefehl? CreatedStep { get; private set; }

        public void CancelCapture()
        {
            try { _captureCts?.Cancel(); } catch { /* ignore */ }
        }

        public void CreateStep()
        {
            CreatedStep = SelectedType switch
            {
                "MouseMoveAbsolute" => new MouseMoveAbsoluteBefehl { X = X, Y = Y },
                "MouseMoveRelative" => new MouseMoveRelativeBefehl { DeltaX = DeltaX, DeltaY = DeltaY },
                "MouseDown" => new MouseDownBefehl { Button = MouseButton },
                "MouseUp" => new MouseUpBefehl { Button = MouseButton },
                "KeyDown" => new KeyDownBefehl { Key = Key },
                "KeyUp" => new KeyUpBefehl { Key = Key },
                "Timeout" => new TimeoutBefehl { Duration = Duration },
                _ => null
            };
        }

        private async System.Threading.Tasks.Task CaptureKeyAsync()
        {
            if (!ShowKey || IsCapturing) return;

            IsCapturing = true;
            _captureCts = new CancellationTokenSource();
            try
            {
                var (mods, vk) = await _capture.CaptureNextAsync(_captureCts.Token);
                Key = _capture.FormatKey(mods, vk);
            }
            catch (OperationCanceledException)
            {
                // Aufnahme abgebrochen → nichts tun
            }
            finally
            {
                _captureCts.Dispose();
                _captureCts = null;
                IsCapturing = false;
            }
        }
    }
}
