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
        DataContext = new RecordingSettingsDialogModel(settings, hotkeys.FormatKey);
    }

    public MakroRecordingSettings Settings { get; private set; }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        var model = (RecordingSettingsDialogModel)DataContext;
        if (!model.TryCreate(out var settings, out var error))
        {
            MessageBox.Show(this, error, "Ungültige Einstellungen", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            if (virtualKey == 0x79)
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

    protected override void OnClosed(EventArgs e)
    {
        _captureCancellation?.Cancel();
        _captureCancellation?.Dispose();
        base.OnClosed(e);
    }
}

internal sealed class RecordingSettingsDialogModel : INotifyPropertyChanged
{
    private readonly MakroRecordingSettings settings;
    private readonly Func<KeyModifiers, uint, string> _formatHotkey;

    public RecordingSettingsDialogModel(MakroRecordingSettings settings, Func<KeyModifiers, uint, string> formatHotkey)
    {
        this.settings = settings;
        _formatHotkey = formatHotkey;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public bool IsScreenAccurate { get => settings.Mode == MakroRecordingMode.ScreenAccurateAbsolute; set { if (value) settings.Mode = MakroRecordingMode.ScreenAccurateAbsolute; } }
    public bool IsMotionFaithful { get => settings.Mode == MakroRecordingMode.MotionFaithfulRelative; set { if (value) settings.Mode = MakroRecordingMode.MotionFaithfulRelative; } }
    public bool IsClicksOnly { get => settings.Mode == MakroRecordingMode.ClicksOnly; set { if (value) settings.Mode = MakroRecordingMode.ClicksOnly; } }
    public int MinimumIntervalMicroseconds { get => settings.MinimumIntervalMicroseconds; set => settings.MinimumIntervalMicroseconds = value; }
    public double MinimumIntervalMilliseconds { get => settings.MinimumIntervalMicroseconds / 1_000d; set => settings.MinimumIntervalMicroseconds = (int)Math.Round(value * 1_000d); }
    public int MinimumDistancePixels { get => settings.MinimumDistancePixels; set => settings.MinimumDistancePixels = value; }
    public bool RecordKeyboard { get => settings.RecordKeyboard; set => settings.RecordKeyboard = value; }
    public bool RecordMouseButtons { get => settings.RecordMouseButtons; set => settings.RecordMouseButtons = value; }
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
            error = "Das Aufnahmeintervall muss zwischen 0 und 1.000.000 µs liegen.";
            return false;
        }
        if (MinimumDistancePixels is < 0 or > 10_000)
        {
            error = "Die Mindestbewegung muss zwischen 0 und 10.000 Pixeln liegen.";
            return false;
        }
        if (settings.RecordingHotkeyVirtualKey is 0 or 0x79)
        {
            error = Loc.Get("Ui.Macro.Recording.Hotkey.Invalid");
            return false;
        }
        error = string.Empty;
        return true;
    }
}
