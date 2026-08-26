using System.Collections.ObjectModel;
using System.ComponentModel;
using DesktopAutomationApp.Localization;

namespace DesktopAutomationApp.ViewModels;

public sealed class SettingsViewModel : ViewModelBase
{
    private SettingsSectionItem? _selectedSection;

    public SettingsViewModel(
        ILocalizationService localization,
        GeneralSettingsViewModel general,
        CredentialsSettingsViewModel credentials,
        YoloDownloadsViewModel models,
        UpdateSettingsViewModel updates)
    {
        Sections =
        [
            new("settings.general", "Settings.Navigation.General", general),
            new("settings.credentials", "Settings.Navigation.Credentials", credentials),
            new("settings.models", "Settings.Navigation.Models", models),
            new("settings.updates", "Settings.Navigation.Updates", updates)
        ];
        _selectedSection = Sections[0];
        localization.CultureChanged += (_, _) =>
        {
            foreach (var section in Sections)
                section.Refresh();
        };
    }

    public ObservableCollection<SettingsSectionItem> Sections { get; }

    public SettingsSectionItem? SelectedSection
    {
        get => _selectedSection;
        set
        {
            if (ReferenceEquals(_selectedSection, value))
                return;
            SetProperty(ref _selectedSection, value);
            if (value?.Content is CredentialsSettingsViewModel credentials)
                _ = credentials.RefreshAsync();
            else if (value?.Content is YoloDownloadsViewModel models)
                _ = models.RefreshModelsAsync();
        }
    }

    public void Select(string sectionId)
    {
        SelectedSection = Sections.FirstOrDefault(section =>
            section.Id.Equals(sectionId, StringComparison.OrdinalIgnoreCase)) ?? Sections[0];
    }
}

public sealed class SettingsSectionItem(
    string id,
    string labelKey,
    object content) : INotifyPropertyChanged
{
    public string Id { get; } = id;
    public string LabelKey { get; } = labelKey;
    public object Content { get; } = content;
    public string DisplayName => Loc.Get(LabelKey);

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Refresh() =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayName)));
}
