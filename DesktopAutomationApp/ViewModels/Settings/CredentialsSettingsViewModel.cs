using System.Collections.ObjectModel;
using DesktopAutomation.Application.Interfaces;
using DesktopAutomationApp.Localization;
using Microsoft.Extensions.Logging;
using TaskAutomation.Security;

namespace DesktopAutomationApp.ViewModels;

public sealed class CredentialsSettingsViewModel : ViewModelBase
{
    private readonly ISecretStore _secretStore;
    private readonly IDialogService _dialogs;
    private readonly ILogger<CredentialsSettingsViewModel> _logger;
    private readonly List<SecretListItemViewModel> _allSecrets = [];
    private SecretListItemViewModel? _selectedSecret;
    private string _searchText = string.Empty;
    private bool _isBusy;
    private bool _isEditing;
    private bool _isCreating;
    private bool _isReplacingValue;
    private string _editorName = string.Empty;
    private string _editorDescription = string.Empty;
    private string _secretValue = string.Empty;
    private string _validationMessage = string.Empty;
    private int _editorSessionVersion;

    public CredentialsSettingsViewModel(
        ISecretStore secretStore,
        IDialogService dialogs,
        ILocalizationService localization,
        ILogger<CredentialsSettingsViewModel> logger)
    {
        _secretStore = secretStore;
        _dialogs = dialogs;
        _logger = logger;
        NewCommand = new RelayCommand(BeginCreate);
        EditCommand = new RelayCommand(BeginEdit);
        ReplaceCommand = new RelayCommand(BeginReplace);
        CancelCommand = new RelayCommand(CancelEditing);
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        DeleteCommand = new AsyncRelayCommand(DeleteAsync);
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
    }

    public ObservableCollection<SecretListItemViewModel> Secrets { get; } = [];
    public RelayCommand NewCommand { get; }
    public RelayCommand EditCommand { get; }
    public RelayCommand ReplaceCommand { get; }
    public RelayCommand CancelCommand { get; }
    public AsyncRelayCommand SaveCommand { get; }
    public AsyncRelayCommand DeleteCommand { get; }
    public AsyncRelayCommand RefreshCommand { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            SetProperty(ref _searchText, value);
            ApplyFilter();
        }
    }

    public SecretListItemViewModel? SelectedSecret
    {
        get => _selectedSecret;
        set
        {
            SetProperty(ref _selectedSecret, value);
            OnPropertyChanged(nameof(ShowDetails));
            OnPropertyChanged(nameof(ShowEmptyState));
        }
    }

    public bool ShowDetails => SelectedSecret is not null && !IsEditing;
    public bool ShowEmptyState => SelectedSecret is null && !IsEditing;

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public bool IsEditing
    {
        get => _isEditing;
        private set
        {
            SetProperty(ref _isEditing, value);
            OnPropertyChanged(nameof(ShowDetails));
            OnPropertyChanged(nameof(ShowEmptyState));
        }
    }

    public bool IsCreating
    {
        get => _isCreating;
        private set
        {
            SetProperty(ref _isCreating, value);
            OnPropertyChanged(nameof(IsSecretInputVisible));
        }
    }

    public bool IsReplacingValue
    {
        get => _isReplacingValue;
        private set
        {
            SetProperty(ref _isReplacingValue, value);
            OnPropertyChanged(nameof(IsSecretInputVisible));
        }
    }

    public bool IsSecretInputVisible => IsCreating || IsReplacingValue;

    public string EditorName
    {
        get => _editorName;
        set => SetProperty(ref _editorName, value);
    }

    public string EditorDescription
    {
        get => _editorDescription;
        set => SetProperty(ref _editorDescription, value);
    }

    public string SecretValue
    {
        get => _secretValue;
        set => SetProperty(ref _secretValue, value);
    }

    public string ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    public int EditorSessionVersion
    {
        get => _editorSessionVersion;
        private set => SetProperty(ref _editorSessionVersion, value);
    }

    public async Task RefreshAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            await RefreshAsyncCore(SelectedSecret?.Id);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Die Secrets konnten nicht geladen werden.");
            _dialogs.ShowError(Loc.Get("Settings.Credentials.LoadError"), Loc.Get("Settings.Credentials.Title"));
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void BeginCreate()
    {
        SelectedSecret = null;
        IsEditing = true;
        IsCreating = true;
        IsReplacingValue = true;
        EditorName = string.Empty;
        EditorDescription = string.Empty;
        ClearSecretValue();
        ValidationMessage = string.Empty;
    }

    public void BeginEdit()
    {
        if (SelectedSecret is null) return;
        IsEditing = true;
        IsCreating = false;
        IsReplacingValue = false;
        EditorName = SelectedSecret.Name;
        EditorDescription = SelectedSecret.Description;
        ClearSecretValue();
        ValidationMessage = string.Empty;
    }

    public void BeginReplace()
    {
        BeginEdit();
        if (IsEditing)
            IsReplacingValue = true;
    }

    public void CancelEditing()
    {
        IsEditing = false;
        IsCreating = false;
        IsReplacingValue = false;
        ValidationMessage = string.Empty;
        ClearSecretValue();
    }

    public async Task SaveAsync()
    {
        ValidationMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(EditorName))
        {
            ValidationMessage = Loc.Get("Settings.Credentials.Validation.Metadata");
            return;
        }
        if (IsSecretInputVisible && string.IsNullOrEmpty(SecretValue))
        {
            ValidationMessage = Loc.Get("Settings.Credentials.Validation.Secret");
            return;
        }

        IsBusy = true;
        try
        {
            Guid selectedId;
            if (IsCreating)
            {
                var descriptor = await _secretStore.CreateAsync(new(
                    EditorName.Trim(),
                    EditorDescription,
                    SecretValue));
                selectedId = descriptor.Id;
            }
            else
            {
                if (SelectedSecret is null) return;
                selectedId = SelectedSecret.Id;
                await _secretStore.UpdateMetadataAsync(selectedId, EditorName.Trim(), EditorDescription);
                if (IsReplacingValue)
                    await _secretStore.ReplaceValueAsync(selectedId, SecretValue);
            }

            IsEditing = false;
            IsCreating = false;
            IsReplacingValue = false;
            ClearSecretValue();
            await RefreshAsyncCore(selectedId);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Das Secret konnte nicht gespeichert werden.");
            ValidationMessage = Loc.Get("Settings.Credentials.SaveError");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task DeleteAsync()
    {
        if (SelectedSecret is null) return;
        var secret = SelectedSecret;
        if (!await _dialogs.ConfirmAsync(
                Loc.Format("Settings.Credentials.DeleteConfirmation", secret.Name),
                Loc.Get("Settings.Credentials.Delete")))
            return;

        IsBusy = true;
        try
        {
            await _secretStore.DeleteAsync(secret.Id);
            CancelEditing();
            await RefreshAsyncCore();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Das Secret konnte nicht gelöscht werden.");
            _dialogs.ShowError(Loc.Get("Settings.Credentials.DeleteError"), Loc.Get("Settings.Credentials.Title"));
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RefreshAsyncCore(Guid? selectedId = null)
    {
        var descriptors = await _secretStore.ListAsync();
        _allSecrets.Clear();
        _allSecrets.AddRange(descriptors.Select(descriptor => new SecretListItemViewModel(descriptor)));
        ApplyFilter();
        SelectedSecret = Secrets.FirstOrDefault(secret => secret.Id == selectedId) ?? Secrets.FirstOrDefault();
    }

    private void ApplyFilter()
    {
        var selectedId = SelectedSecret?.Id;
        var filter = SearchText.Trim();
        Secrets.Clear();
        foreach (var secret in _allSecrets.Where(secret =>
                     filter.Length == 0
                     || secret.Name.Contains(filter, StringComparison.CurrentCultureIgnoreCase)
                     || secret.Description.Contains(filter, StringComparison.CurrentCultureIgnoreCase)))
            Secrets.Add(secret);
        SelectedSecret = Secrets.FirstOrDefault(secret => secret.Id == selectedId) ?? Secrets.FirstOrDefault();
    }

    private void ClearSecretValue()
    {
        SecretValue = string.Empty;
        EditorSessionVersion++;
    }
}

public sealed class SecretListItemViewModel(SecretDescriptor descriptor)
{
    public Guid Id { get; } = descriptor.Id;
    public string Name { get; } = descriptor.Name;
    public string Description { get; } = descriptor.Description;
    public DateTime UpdatedAtUtc { get; } = descriptor.UpdatedAtUtc;
    public string UpdatedDisplay => UpdatedAtUtc.ToLocalTime().ToString("g");
}
