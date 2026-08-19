using System.Text.Json.Nodes;
using DesktopAutomationApp.Localization;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;

namespace DesktopAutomationApp.ViewModels;

public sealed class QuickCreateJobVariableViewModel : ViewModelBase
{
    private readonly IReadOnlyList<JobVariableKindOption> _typeOptions;
    private readonly IReadOnlySet<string> _existingNames;

    public QuickCreateJobVariableViewModel(
        StepInputDescriptor contract,
        string stepName,
        string fieldName,
        IReadOnlyList<JobVariable> existing)
    {
        _existingNames = existing.Select(variable => variable.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var supported = contract.AcceptedShapes.Select(shape => shape.ValueKind)
            .Where(JobVariableEditorViewModel.SupportedKinds.Contains)
            .Distinct().ToArray();
        if (supported.Length == 0)
            throw new InvalidOperationException("The input contract does not support creatable job variables.");
        Variable = new JobVariable
        {
            Name = string.Empty,
            Description = Loc.Format("Ui.ValueReference.CreateVariable.Description", fieldName, stepName),
            Scope = JobVariableScope.Shared,
            ValueKind = supported[0],
            Cardinality = contract.AcceptedShapes[0].Cardinalities.FirstOrDefault(ResultCardinality.Single),
            Value = DefaultValue(supported[0])
        };
        Editor = new JobVariableEditorViewModel(Variable, () => { });
        _typeOptions = Editor.KindOptions.Where(option => supported.Contains(option.Kind)).ToArray();
    }

    public JobVariable Variable { get; }
    public JobVariableEditorViewModel Editor { get; }
    public IReadOnlyList<JobVariableKindOption> TypeOptions => _typeOptions;
    public string Name
    {
        get => Editor.Name;
        set
        {
            if (Editor.Name == value) return;
            Editor.Name = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanCreate));
            OnPropertyChanged(nameof(NameValidationMessage));
        }
    }

    public bool CanCreate => !string.IsNullOrWhiteSpace(Name)
                             && !_existingNames.Contains(Name.Trim());

    public string NameValidationMessage => string.IsNullOrWhiteSpace(Name)
        ? Loc.Get("Ui.ValueReference.CreateVariable.NameRequired")
        : _existingNames.Contains(Name.Trim())
            ? Loc.Get("Ui.ValueReference.CreateVariable.NameDuplicate")
            : string.Empty;

    public JobVariableKindOption SelectedKind
    {
        get => Editor.SelectedKind;
        set
        {
            Editor.SelectedKind = value;
            OnPropertyChanged();
        }
    }

    private static JsonNode? DefaultValue(ResultValueKind kind) => kind switch
    {
        ResultValueKind.Text or ResultValueKind.Enum => JsonValue.Create(string.Empty),
        ResultValueKind.Boolean => JsonValue.Create(false),
        ResultValueKind.Integer => JsonValue.Create(0),
        ResultValueKind.Number => JsonValue.Create(0d),
        ResultValueKind.DateTime => JsonValue.Create(DateTime.Now),
        ResultValueKind.Point => new JsonObject { ["x"] = 0, ["y"] = 0 },
        ResultValueKind.Rectangle => new JsonObject
            { ["x"] = 0, ["y"] = 0, ["width"] = 0, ["height"] = 0 },
        ResultValueKind.Image => JsonValue.Create(string.Empty),
        ResultValueKind.ResultObject => new JsonObject(),
        _ => null
    };
}
