using System.Windows;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MahApps.Metro.Controls;
using DesktopAutomationApp.Localization;
using TaskAutomation.Hotkeys;
using TaskAutomation.Makros;

namespace DesktopAutomationApp.Views;

public partial class RecordingSettingsDialog : MetroWindow
{
    private readonly IGlobalHotkeyService _hotkeys;
    private CancellationTokenSource? _captureCancellation;

    public RecordingSettingsDialog(MakroRecordingSettings settings, IGlobalHotkeyService hotkeys)
    {
        InitializeComponent();
        _hotkeys = hotkeys;
        Settings = settings;
        DataContext = new RecordingSettingsDialogModel(
            settings,
            hotkeys.FormatKey,
            hotkeys.ForceStopVirtualKey);
        LocalizationService.Instance.CultureChanged += OnCultureChanged;
    }

    public MakroRecordingSettings Settings { get; private set; }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        var model = (RecordingSettingsDialogModel)DataContext;
        if (!model.TryCreate(out var settings, out var error))
        {
            MessageBox.Show(this, error, Loc.Get("Validation.Title"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        Settings = settings;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private async void CaptureHotkey_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button) return;
        _captureCancellation?.Cancel();
        _captureCancellation = new CancellationTokenSource();
        button.IsEnabled = false;
        try
        {
            var (modifiers, virtualKey) = await _hotkeys.CaptureNextAsync(_captureCancellation.Token);
            if (virtualKey == _hotkeys.ForceStopVirtualKey)
            {
                MessageBox.Show(this, Loc.Get("Ui.Macro.Recording.Hotkey.Invalid"),
                    Loc.Get("Ui.Macro.Recording.Settings"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            ((RecordingSettingsDialogModel)DataContext).SetHotkey(modifiers, virtualKey);
        }
        catch (OperationCanceledException) { }
        finally { button.IsEnabled = true; }
    }

    private void OnCultureChanged(object? sender, EventArgs e) => ((RecordingSettingsDialogModel)DataContext).RefreshLocalization();

    protected override void OnClosed(EventArgs e)
    {
        _captureCancellation?.Cancel();
        _captureCancellation?.Dispose();
        LocalizationService.Instance.CultureChanged -= OnCultureChanged;
        base.OnClosed(e);
    }
}

internal sealed class RecordingSettingsDialogModel : INotifyPropertyChanged
{
    private readonly MakroRecordingSettings settings;
    private readonly Func<KeyModifiers, uint, string> _formatHotkey;
    private readonly uint _forceStopVirtualKey;

    public RecordingSettingsDialogModel(
        MakroRecordingSettings settings,
        Func<KeyModifiers, uint, string> formatHotkey,
        uint forceStopVirtualKey)
    {
        this.settings = settings.Clone();
        _formatHotkey = formatHotkey;
        _forceStopVirtualKey = forceStopVirtualKey;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private string _section = "Recording";
    public bool IsRecordingSection { get => _section == "Recording"; set { if (value) SelectSection("Recording"); } }
    public bool IsInputsSection { get => _section == "Inputs"; set { if (value) SelectSection("Inputs"); } }
    public bool IsPrecisionSection { get => _section == "Precision"; set { if (value) SelectSection("Precision"); } }
    private void SelectSection(string section)
    {
        if (_section == section) return;
        _section = section;
        OnPropertyChanged(nameof(IsRecordingSection));
        OnPropertyChanged(nameof(IsInputsSection));
        OnPropertyChanged(nameof(IsPrecisionSection));
    }
    public string InputsSummary => Loc.Get((RecordKeyboard, RecordMouseButtons) switch
    {
        (true, true) => "Ui.Macro.Recording.Summary.Both",
        (true, false) => "Ui.Macro.Recording.Summary.Keyboard",
        (false, true) => "Ui.Macro.Recording.Summary.Mouse",
        _ => "Ui.Macro.Recording.Summary.None"
    });
    public string PrecisionSummary => Loc.Format("Ui.Macro.Recording.Summary.Precision", MinimumIntervalMilliseconds, MinimumDistancePixels);
    public void RefreshLocalization()
    {
        OnPropertyChanged(nameof(InputsSummary));
        OnPropertyChanged(nameof(PrecisionSummary));
    }
    public bool IsScreenAccurate { get => settings.Mode == MakroRecordingMode.ScreenAccurateAbsolute; set { if (value) SetMode(MakroRecordingMode.ScreenAccurateAbsolute); } }
    public bool IsMotionFaithful { get => settings.Mode == MakroRecordingMode.MotionFaithfulRelative; set { if (value) SetMode(MakroRecordingMode.MotionFaithfulRelative); } }
    public bool IsClicksOnly { get => settings.Mode == MakroRecordingMode.ClicksOnly; set { if (value) SetMode(MakroRecordingMode.ClicksOnly); } }
    private void SetMode(MakroRecordingMode mode)
    {
        if (settings.Mode == mode) return;
        settings.Mode = mode;
        OnPropertyChanged(nameof(IsScreenAccurate));
        OnPropertyChanged(nameof(IsMotionFaithful));
        OnPropertyChanged(nameof(IsClicksOnly));
    }
    public int MinimumIntervalMicroseconds { get => settings.MinimumIntervalMicroseconds; set { settings.MinimumIntervalMicroseconds = value; OnPropertyChanged(nameof(PrecisionSummary)); } }
    public double MinimumIntervalMilliseconds { get => settings.MinimumIntervalMicroseconds / 1_000d; set { settings.MinimumIntervalMicroseconds = (int)Math.Round(value * 1_000d); OnPropertyChanged(nameof(PrecisionSummary)); } }
    public int MinimumDistancePixels { get => settings.MinimumDistancePixels; set { settings.MinimumDistancePixels = value; OnPropertyChanged(nameof(PrecisionSummary)); } }
    public bool RecordKeyboard { get => settings.RecordKeyboard; set { settings.RecordKeyboard = value; OnPropertyChanged(nameof(InputsSummary)); } }
    public bool CombineKeyboardInputs { get => settings.CombineKeyboardInputs; set => settings.CombineKeyboardInputs = value; }
    public bool RecordMouseButtons { get => settings.RecordMouseButtons; set { settings.RecordMouseButtons = value; OnPropertyChanged(nameof(InputsSummary)); } }
    public bool RemoveStopGesture { get => settings.RemoveStopGesture; set => settings.RemoveStopGesture = value; }
    public bool AutomaticMovementGroups { get => settings.AutomaticMovementGroups; set => settings.AutomaticMovementGroups = value; }
    public string HotkeyDisplay => _formatHotkey(settings.RecordingHotkeyModifiers, settings.RecordingHotkeyVirtualKey);

    public void SetHotkey(KeyModifiers modifiers, uint virtualKey)
    {
        settings.RecordingHotkeyModifiers = modifiers;
        settings.RecordingHotkeyVirtualKey = virtualKey;
        OnPropertyChanged(nameof(HotkeyDisplay));
    }

    public bool TryCreate(out MakroRecordingSettings result, out string error)
    {
        result = settings.Clone();
        if (MinimumIntervalMicroseconds is < 0 or > 1_000_000)
        {
            error = Loc.Get("Ui.Macro.Recording.Interval.Invalid");
            return false;
        }
        if (MinimumDistancePixels is < 0 or > 10_000)
        {
            error = Loc.Get("Ui.Macro.Recording.Distance.Invalid");
            return false;
        }
        if (settings.RecordingHotkeyVirtualKey == 0
            || settings.RecordingHotkeyVirtualKey == _forceStopVirtualKey)
        {
            error = Loc.Get("Ui.Macro.Recording.Hotkey.Invalid");
            return false;
        }
        error = string.Empty;
        return true;
    }
}
