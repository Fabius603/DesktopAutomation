using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using DesktopAutomationApp.Localization;
using DesktopAutomationApp.ViewModels;
using TaskAutomation.WindowsIntegration;

namespace DesktopAutomationApp.ViewModels.WindowsIntegration;

public enum WindowsCapabilityPickerMode { Event, StateQuery, SettingChange }

public sealed record WindowsParameterOptionViewModel(string Value, string DisplayName);

public sealed class WindowsParameterValueViewModel : INotifyPropertyChanged
{
    private readonly IWindowsSettingOptionProvider _optionProvider;
    private string? _value;
    private bool _isLoadingOptions;
    private string _optionStatus = string.Empty;
    private bool _isVisible = true;
    public WindowsParameterDescriptor Descriptor { get; }
    public string? Value { get => _value; set { if (_value == value) return; _value = value; OnPropertyChanged(); ValueChanged?.Invoke(); } }
    public string DisplayName => WindowsCapabilityLocalization.ParameterName(Descriptor) + (Descriptor.Required ? " *" : string.Empty);
    public string? Placeholder => WindowsCapabilityLocalization.ParameterPlaceholder(Descriptor);
    public ObservableCollection<WindowsParameterOptionViewModel> Options { get; } = [];
    public bool IsBoolean => Descriptor.Type == WindowsParameterType.Boolean;
    public bool BooleanValue
    {
        get => bool.TryParse(Value, out var result) && result;
        set { Value = value.ToString(); OnPropertyChanged(); }
    }
    public bool IsEnum => Descriptor.Type == WindowsParameterType.Enum;
    public bool HasDynamicOptions => Descriptor.DynamicOptionSource.HasValue;
    public bool IsSelection => IsEnum || HasDynamicOptions;
    public bool IsText => !IsBoolean && !IsSelection;
    public bool IsLoadingOptions
    {
        get => _isLoadingOptions;
        private set { if (_isLoadingOptions == value) return; _isLoadingOptions = value; OnPropertyChanged(); }
    }
    public string OptionStatus
    {
        get => _optionStatus;
        private set { if (_optionStatus == value) return; _optionStatus = value; OnPropertyChanged(); }
    }
    public bool IsVisible
    {
        get => _isVisible;
        set { if (_isVisible == value) return; _isVisible = value; OnPropertyChanged(); }
    }
    public ICommand RefreshOptionsCommand { get; }
    public event Action? ValueChanged;
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));

    public WindowsParameterValueViewModel(
        WindowsParameterDescriptor descriptor,
        IWindowsSettingOptionProvider optionProvider,
        string? value = null)
    {
        Descriptor = descriptor;
        _optionProvider = optionProvider;
        _value = value ?? descriptor.DefaultValue;
        RefreshOptionsCommand = new AsyncRelayCommand(RefreshOptionsAsync, () => HasDynamicOptions && !IsLoadingOptions);
        foreach (var allowedValue in descriptor.AllowedValues ?? [])
            Options.Add(new WindowsParameterOptionViewModel(
                allowedValue, WindowsCapabilityLocalization.OptionName(allowedValue)));
    }

    public async Task RefreshOptionsAsync()
    {
        if (Descriptor.DynamicOptionSource is not { } source) return;
        IsLoadingOptions = true;
        OptionStatus = Loc.Get("Ui.Windows.Options.Loading");
        (RefreshOptionsCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        try
        {
            var discovered = await _optionProvider.GetOptionsAsync(source, CancellationToken.None);
            var options = WindowsSettingOptionList.PreserveCurrent(
                discovered,
                Value,
                Loc.Format("Ui.Windows.Options.Unavailable", Value ?? string.Empty));
            Options.Clear();
            foreach (var option in options)
                Options.Add(new WindowsParameterOptionViewModel(option.Value, option.DisplayName));

            if (string.IsNullOrWhiteSpace(Value) && Options.Count > 0)
                Value = Options[0].Value;

            OptionStatus = Options.Count == 0
                ? Loc.Get("Ui.Windows.Options.None")
                : string.Empty;
        }
        catch (Exception exception)
        {
            OptionStatus = Loc.Format("Ui.Windows.Options.Failed", exception.Message);
        }
        finally
        {
            IsLoadingOptions = false;
            (RefreshOptionsCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        }
    }
}

public sealed class WindowsCapabilityPickerViewModel : INotifyPropertyChanged
{
    private readonly IWindowsCapabilityCatalog _catalog;
    private readonly IWindowsSettingOptionProvider _optionProvider;
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
        : WindowsCapabilityLocalization.Description(SelectedCapability, _mode);
    public string AvailabilityHint => RequiresElevation
        ? Loc.Get("Ui.Windows.RequiresElevation")
        : string.Empty;

    public WindowsCapabilityPickerViewModel(IWindowsCapabilityCatalog catalog, WindowsCapabilityPickerMode mode,
        string? selectedId = null, IReadOnlyDictionary<string, string?>? values = null,
        IWindowsSettingOptionProvider? optionProvider = null)
    {
        _catalog = catalog;
        _mode = mode;
        _optionProvider = optionProvider ?? new DefaultWindowsSettingOptionProvider();
        foreach (var category in catalog.Capabilities.Where(IsSupported).Select(x => x.Category).Distinct().OrderBy(x => x.ToString()))
            Categories.Add(category);
        var selected = string.IsNullOrWhiteSpace(selectedId) ? null : catalog.Find(selectedId);
        _selectedCategory = selected?.Category ?? Categories.FirstOrDefault();
        RefreshCapabilities(selected, values);
    }

    public Dictionary<string, string?> ToDictionary() => Parameters
        .Where(x => x.IsVisible && !string.IsNullOrWhiteSpace(x.Value))
        .ToDictionary(x => x.Descriptor.Name, x => x.Value, StringComparer.OrdinalIgnoreCase);

    public void Load(string? id, IReadOnlyDictionary<string, string?>? values)
    {
        var selected = string.IsNullOrWhiteSpace(id) ? null : _catalog.Find(id);
        _selectedCategory = selected?.Category ?? Categories.FirstOrDefault();
        OnPropertyChanged(nameof(SelectedCategory));
        RefreshCapabilities(selected, values);
    }

    private bool IsSupported(WindowsCapabilityDescriptor capability) => _mode switch
    {
        WindowsCapabilityPickerMode.Event => capability.SupportsEvents,
        WindowsCapabilityPickerMode.StateQuery => capability.SupportsStateQuery,
        WindowsCapabilityPickerMode.SettingChange => capability.SupportsSettingChange,
        _ => false
    };

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
            var parameter = new WindowsParameterValueViewModel(descriptor, _optionProvider, value);
            parameter.ValueChanged += ParameterChanged;
            Parameters.Add(parameter);
            if (parameter.HasDynamicOptions) _ = parameter.RefreshOptionsAsync();
        }
        UpdateParameterVisibility();
        OnPropertyChanged(nameof(IsValid)); OnPropertyChanged(nameof(RequiresElevation)); OnPropertyChanged(nameof(AvailabilityHint));
    }

    private void ParameterChanged()
    {
        UpdateParameterVisibility();
        OnPropertyChanged(nameof(IsValid));
        Changed?.Invoke();
    }

    private void UpdateParameterVisibility()
    {
        var action = Parameters.FirstOrDefault(parameter =>
            parameter.Descriptor.Name.Equals("action", StringComparison.OrdinalIgnoreCase))?.Value;
        foreach (var parameter in Parameters)
            parameter.IsVisible = parameter.Descriptor.Name != "profile"
                                  || !string.Equals(action, "disconnect", StringComparison.OrdinalIgnoreCase);
    }
    private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
}
