using DesktopAutomationApp.Localization;
using TaskAutomation.Security;

namespace DesktopAutomationApp.ViewModels;

public sealed class QuickCreateSecretViewModel : ViewModelBase
{
    private readonly ISecretStore _secretStore;
    private string _name = string.Empty;
    private string _description = string.Empty;
    private string _value = string.Empty;
    private string _validationMessage = string.Empty;
    private bool _isBusy;

    public QuickCreateSecretViewModel(ISecretStore secretStore) =>
        _secretStore = secretStore ?? throw new ArgumentNullException(nameof(secretStore));

    public SecretDescriptor? CreatedSecret { get; private set; }

    public string Name
    {
        get => _name;
        set
        {
            if (_name == value) return;
            SetProperty(ref _name, value);
            OnPropertyChanged(nameof(CanCreate));
        }
    }

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public string Value
    {
        get => _value;
        set
        {
            if (_value == value) return;
            SetProperty(ref _value, value);
            OnPropertyChanged(nameof(CanCreate));
        }
    }

    public string ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (_isBusy == value) return;
            SetProperty(ref _isBusy, value);
            OnPropertyChanged(nameof(CanCreate));
        }
    }

    public bool CanCreate => !IsBusy && !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrEmpty(Value);

    public async Task<bool> CreateAsync()
    {
        if (!CanCreate)
        {
            ValidationMessage = Loc.Get("Ui.ValueReference.CreateSecret.Validation");
            return false;
        }

        IsBusy = true;
        ValidationMessage = string.Empty;
        try
        {
            CreatedSecret = await _secretStore.CreateAsync(new SecretCreateRequest(
                Name.Trim(), Description.Trim(), Value));
            Value = string.Empty;
            return true;
        }
        catch (Exception)
        {
            ValidationMessage = Loc.Get("Ui.ValueReference.CreateSecret.Error");
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
