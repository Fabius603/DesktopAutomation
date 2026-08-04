using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;

namespace DesktopAutomationApp.ViewModels;

public sealed class DetectionOverlayRowViewModel : INotifyPropertyChanged
{
    private readonly ObservableCollection<DetectionOverlayRowViewModel> _owner;

    public DetectionOverlayRowViewModel(
        ObservableCollection<DetectionOverlayRowViewModel> owner,
        IReadOnlyList<SourceStepItem> sources,
        StepInputDescriptor inputContract,
        ResultBinding? binding = null)
    {
        _owner = owner;
        Source = new ResultBindingPickerViewModel(sources, inputContract, false);
        Source.PropertyChanged += (_, _) => PropertyChanged?.Invoke(this, new(nameof(Source)));
        if (binding is not null) Source.Load(binding);
        RemoveCommand = new RelayCommand(() => owner.Remove(this));
        MoveUpCommand = new RelayCommand(() => Move(-1));
        MoveDownCommand = new RelayCommand(() => Move(1));
    }

    public ResultBindingPickerViewModel Source { get; }
    public ICommand RemoveCommand { get; }
    public ICommand MoveUpCommand { get; }
    public ICommand MoveDownCommand { get; }
    public event PropertyChangedEventHandler? PropertyChanged;

    private void Move(int delta)
    {
        var index = _owner.IndexOf(this);
        var target = index + delta;
        if (index >= 0 && target >= 0 && target < _owner.Count)
            _owner.Move(index, target);
    }
}

public sealed class TextOverlayRowViewModel : INotifyPropertyChanged
{
    private readonly ObservableCollection<TextOverlayRowViewModel> _owner;
    private Guid _id = Guid.NewGuid();
    private float _fontSize = 24f;
    private Color _fontColor = Colors.White;
    private float _opacity = 1f;
    private int _desktopIndex;
    private int _offsetX;
    private int _offsetY;
    private int _durationMs = 5000;
    private bool _clearOnJobEnd = true;

    public TextOverlayRowViewModel(
        ObservableCollection<TextOverlayRowViewModel> owner,
        IReadOnlyList<SourceStepItem> sources,
        StepInputDescriptor inputContract,
        Action<TextOverlayRowViewModel>? chooseMonitor,
        TextResultOverlaySettings? settings = null)
    {
        _owner = owner;
        Source = new ResultBindingPickerViewModel(sources, inputContract, false);
        Source.PropertyChanged += (_, _) => OnChange(nameof(Source));
        if (settings is not null)
        {
            _id = settings.Id == Guid.Empty ? Guid.NewGuid() : settings.Id;
            _fontSize = settings.FontSize;
            _fontColor = ParseColor(settings.FontColor);
            _opacity = settings.Opacity;
            _desktopIndex = settings.DesktopIndex;
            _offsetX = settings.OffsetX;
            _offsetY = settings.OffsetY;
            _durationMs = settings.DurationMs;
            _clearOnJobEnd = settings.ClearOnJobEnd;
            Source.Load(settings.Result);
        }
        RemoveCommand = new RelayCommand(() => owner.Remove(this));
        MoveUpCommand = new RelayCommand(() => Move(-1));
        MoveDownCommand = new RelayCommand(() => Move(1));
        ChooseMonitorCommand = new RelayCommand(() => chooseMonitor?.Invoke(this));
    }

    public ResultBindingPickerViewModel Source { get; }
    public ICommand RemoveCommand { get; }
    public ICommand MoveUpCommand { get; }
    public ICommand MoveDownCommand { get; }
    public ICommand ChooseMonitorCommand { get; }
    public float FontSize { get => _fontSize; set { _fontSize = value; OnChange(); } }
    public Color FontColor { get => _fontColor; set { _fontColor = value; OnChange(); } }
    public float Opacity { get => _opacity; set { _opacity = value; OnChange(); } }
    public int DesktopIndex { get => _desktopIndex; set { _desktopIndex = value; OnChange(); } }
    public int OffsetX { get => _offsetX; set { _offsetX = value; OnChange(); } }
    public int OffsetY { get => _offsetY; set { _offsetY = value; OnChange(); } }
    public int DurationMs { get => _durationMs; set { _durationMs = value; OnChange(); } }
    public bool ClearOnJobEnd { get => _clearOnJobEnd; set { _clearOnJobEnd = value; OnChange(); } }

    public TextResultOverlaySettings ToSettings() => new()
    {
        Id = _id,
        Result = Source.ToBinding(),
        FontSize = FontSize,
        FontColor = $"#{FontColor.R:X2}{FontColor.G:X2}{FontColor.B:X2}",
        Opacity = Opacity,
        DesktopIndex = DesktopIndex,
        OffsetX = OffsetX,
        OffsetY = OffsetY,
        DurationMs = DurationMs,
        ClearOnJobEnd = ClearOnJobEnd
    };

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnChange([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new(name));

    private void Move(int delta)
    {
        var index = _owner.IndexOf(this);
        var target = index + delta;
        if (index >= 0 && target >= 0 && target < _owner.Count)
            _owner.Move(index, target);
    }

    private static Color ParseColor(string value)
        => WpfColorParser.TryParse(value, out var color) ? color : Colors.White;
}
