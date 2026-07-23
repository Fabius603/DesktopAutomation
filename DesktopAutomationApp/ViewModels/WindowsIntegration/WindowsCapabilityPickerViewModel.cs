using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using DesktopAutomationApp.Localization;
using TaskAutomation.WindowsIntegration;

namespace DesktopAutomationApp.ViewModels.WindowsIntegration;

public enum WindowsCapabilityPickerMode { Event, StateQuery }

public sealed class WindowsParameterValueViewModel : INotifyPropertyChanged
{
    private string? _value;
    public WindowsParameterDescriptor Descriptor { get; }
    public string? Value { get => _value; set { if (_value == value) return; _value = value; OnPropertyChanged(); ValueChanged?.Invoke(); } }
    public string DisplayName => WindowsCapabilityLocalization.ParameterName(Descriptor) + (Descriptor.Required ? " *" : string.Empty);
    public string? Placeholder => WindowsCapabilityLocalization.ParameterPlaceholder(Descriptor);
    public bool IsBoolean => Descriptor.Type == WindowsParameterType.Boolean;
    public bool BooleanValue
    {
        get => bool.TryParse(Value, out var result) && result;
        set { Value = value.ToString(); OnPropertyChanged(); }
    }
    public bool IsEnum => Descriptor.Type == WindowsParameterType.Enum;
    public bool IsText => !IsBoolean && !IsEnum;
    public IReadOnlyList<string> AllowedValues => Descriptor.AllowedValues ?? [];
    public event Action? ValueChanged;
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));

    public WindowsParameterValueViewModel(WindowsParameterDescriptor descriptor, string? value = null)
    {
        Descriptor = descriptor;
        _value = value ?? descriptor.DefaultValue;
    }
}

public sealed class WindowsCapabilityPickerViewModel : INotifyPropertyChanged
{
    private readonly IWindowsCapabilityCatalog _catalog;
    private readonly WindowsCapabilityPickerMode _mode;
    private WindowsEventCategory? _selectedCategory;
    private WindowsCapabilityDescriptor? _selectedCapability;

    public ObservableCollection<WindowsEventCategory> Categories { get; } = [];
    public ObservableCollection<WindowsCapabilityDescriptor> Capabilities { get; } = [];
    public ObservableCollection<WindowsParameterValueViewModel> Parameters { get; } = [];
    public event Action? Changed;
    public event PropertyChangedEventHandler? PropertyChanged;

    public WindowsEventCategory? SelectedCategory
    {
        get => _selectedCategory;
        set { if (_selectedCategory == value) return; _selectedCategory = value; OnPropertyChanged(); RefreshCapabilities(); Changed?.Invoke(); }
    }

    public WindowsCapabilityDescriptor? SelectedCapability
    {
        get => _selectedCapability;
        set
        {
            if (_selectedCapability == value) return;
            _selectedCapability = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedCapabilityDescription));
            RefreshParameters();
            Changed?.Invoke();
        }
    }

    public bool IsValid => SelectedCapability is not null && Parameters.All(p =>
        !p.Descriptor.Required || !string.IsNullOrWhiteSpace(p.Value));
    public bool RequiresElevation => SelectedCapability?.Requirements?.RequiresElevation == true;
    public string SelectedCapabilityDescription => SelectedCapability is null
        ? string.Empty
        : WindowsCapabilityLocalization.Description(SelectedCapability, _mode == WindowsCapabilityPickerMode.Event);
    public string AvailabilityHint => RequiresElevation
        ? Loc.Get("Ui.Windows.RequiresElevation")
        : string.Empty;

    public WindowsCapabilityPickerViewModel(IWindowsCapabilityCatalog catalog, WindowsCapabilityPickerMode mode,
        string? selectedId = null, IReadOnlyDictionary<string, string?>? values = null)
    {
        _catalog = catalog; _mode = mode;
        foreach (var category in catalog.Capabilities.Where(IsSupported).Select(x => x.Category).Distinct().OrderBy(x => x.ToString()))
            Categories.Add(category);
        var selected = string.IsNullOrWhiteSpace(selectedId) ? null : catalog.Find(selectedId);
        _selectedCategory = selected?.Category ?? Categories.FirstOrDefault();
        RefreshCapabilities(selected, values);
    }

    public Dictionary<string, string?> ToDictionary() => Parameters
        .Where(x => !string.IsNullOrWhiteSpace(x.Value))
        .ToDictionary(x => x.Descriptor.Name, x => x.Value, StringComparer.OrdinalIgnoreCase);

    public void Load(string? id, IReadOnlyDictionary<string, string?>? values)
    {
        var selected = string.IsNullOrWhiteSpace(id) ? null : _catalog.Find(id);
        _selectedCategory = selected?.Category ?? Categories.FirstOrDefault();
        OnPropertyChanged(nameof(SelectedCategory));
        RefreshCapabilities(selected, values);
    }

    private bool IsSupported(WindowsCapabilityDescriptor capability) => _mode == WindowsCapabilityPickerMode.Event
        ? capability.SupportsEvents : capability.SupportsStateQuery;

    private void RefreshCapabilities(WindowsCapabilityDescriptor? selected = null,
        IReadOnlyDictionary<string, string?>? values = null)
    {
        Capabilities.Clear();
        if (SelectedCategory.HasValue)
            foreach (var capability in _catalog.Capabilities.Where(x => x.Category == SelectedCategory && IsSupported(x))
                         .OrderBy(WindowsCapabilityLocalization.DisplayName))
                Capabilities.Add(capability);
        _selectedCapability = selected is not null && Capabilities.Contains(selected) ? selected : Capabilities.FirstOrDefault();
        OnPropertyChanged(nameof(SelectedCapability));
        OnPropertyChanged(nameof(SelectedCapabilityDescription));
        RefreshParameters(values);
    }

    private void RefreshParameters(IReadOnlyDictionary<string, string?>? values = null)
    {
        Parameters.Clear();
        foreach (var descriptor in SelectedCapability?.Parameters ?? [])
        {
            string? value = null;
            values?.TryGetValue(descriptor.Name, out value);
            var parameter = new WindowsParameterValueViewModel(descriptor, value);
            parameter.ValueChanged += ParameterChanged;
            Parameters.Add(parameter);
        }
        OnPropertyChanged(nameof(IsValid)); OnPropertyChanged(nameof(RequiresElevation)); OnPropertyChanged(nameof(AvailabilityHint));
    }

    private void ParameterChanged() { OnPropertyChanged(nameof(IsValid)); Changed?.Invoke(); }
    private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
}
