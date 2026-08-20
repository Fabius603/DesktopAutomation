using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Text.Json.Nodes;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;

namespace DesktopAutomationApp.ViewModels;

public sealed class UserChoiceOptionEditorViewModel : INotifyPropertyChanged
{
    private readonly ObservableCollection<UserChoiceOptionEditorViewModel> _owner;
    private string _label;
    private string _value;

    public UserChoiceOptionEditorViewModel(
        ObservableCollection<UserChoiceOptionEditorViewModel> owner,
        string? id = null,
        string label = "",
        string value = "")
    {
        _owner = owner;
        Id = string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString("N") : id;
        _label = label;
        _value = value;
        MoveUpCommand = new RelayCommand(MoveUp, () => _owner.IndexOf(this) > 0);
        MoveDownCommand = new RelayCommand(MoveDown, () =>
            _owner.IndexOf(this) >= 0 && _owner.IndexOf(this) < _owner.Count - 1);
        RemoveCommand = new RelayCommand(() => _owner.Remove(this), () => _owner.Count > 2);
        _owner.CollectionChanged += (_, _) => RefreshCommands();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public string Id { get; }
    public int Number => _owner.IndexOf(this) + 1;
    public string Label
    {
        get => _label;
        set { _label = value; OnChange(); }
    }
    public string Value
    {
        get => _value;
        set { _value = value; OnChange(); }
    }

    public GeneratedStepFieldViewModel? LabelField { get; private set; }
    public GeneratedStepFieldViewModel? ValueField { get; private set; }

    public void ConfigureNestedInputs(
        string keyPrefix,
        Func<string, StepValueKind, JsonNode?, GeneratedResultBindingEditorViewModel> resolver)
    {
        LabelField = CreateField($"{keyPrefix}.label", Label, resolver);
        ValueField = CreateField($"{keyPrefix}.value", Value, resolver);
        LabelField.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(GeneratedStepFieldViewModel.InputText)) Label = LabelField.InputText;
            else OnChange();
        };
        ValueField.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(GeneratedStepFieldViewModel.InputText)) Value = ValueField.InputText;
            else OnChange();
        };
        OnChange(nameof(LabelField));
        OnChange(nameof(ValueField));
    }

    public ICommand MoveUpCommand { get; }
    public ICommand MoveDownCommand { get; }
    public ICommand RemoveCommand { get; }

    public UserChoiceOption ToOption() => new() { Id = Id, Label = Label, Value = Value };

    private static GeneratedStepFieldViewModel CreateField(
        string key,
        string value,
        Func<string, StepValueKind, JsonNode?, GeneratedResultBindingEditorViewModel> resolver)
    {
        var node = JsonValue.Create(value);
        var descriptor = new StepFieldDescriptor(key, string.Empty, StepValueKind.Text,
            DefaultValue: node, EditorHint: StepEditorHints.EmojiText);
        return new GeneratedStepFieldViewModel(descriptor, node,
            inputReferenceEditor: resolver(key, StepValueKind.Text, node));
    }

    private void MoveUp()
    {
        var index = _owner.IndexOf(this);
        if (index > 0) _owner.Move(index, index - 1);
    }

    private void MoveDown()
    {
        var index = _owner.IndexOf(this);
        if (index >= 0 && index < _owner.Count - 1) _owner.Move(index, index + 1);
    }

    private void RefreshCommands()
    {
        OnChange(nameof(Number));
        (MoveUpCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (MoveDownCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (RemoveCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    private void OnChange([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
