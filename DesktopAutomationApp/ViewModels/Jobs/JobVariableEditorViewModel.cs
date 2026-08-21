using System.Text.Json.Nodes;
using System.Runtime.CompilerServices;
using DesktopAutomationApp.Localization;
using DesktopAutomationApp.Services.Jobs;
using TaskAutomation.Jobs;

namespace DesktopAutomationApp.ViewModels;

public sealed class JobVariableEditorViewModel : ViewModelBase
{
    public static IReadOnlySet<ResultValueKind> SupportedKinds { get; } = new HashSet<ResultValueKind>
    {
        ResultValueKind.Text,
        ResultValueKind.Enum,
        ResultValueKind.Boolean,
        ResultValueKind.Integer,
        ResultValueKind.Number,
        ResultValueKind.DateTime,
        ResultValueKind.Point,
        ResultValueKind.Rectangle,
        ResultValueKind.Image
    };

    private readonly Action _changed;
    private bool _loading;

    public JobVariableEditorViewModel(JobVariable model, Action changed)
    {
        Model = model;
        _changed = changed;
        KindOptions =
        [
            new(ResultValueKind.Text, "Ui.Job.Variables.Type.Text"),
            new(ResultValueKind.Enum, "Ui.Job.Variables.Type.Enum"),
            new(ResultValueKind.Boolean, "Ui.Job.Variables.Type.Boolean"),
            new(ResultValueKind.Integer, "Ui.Job.Variables.Type.Integer"),
            new(ResultValueKind.Number, "Ui.Job.Variables.Type.Number"),
            new(ResultValueKind.DateTime, "Ui.Job.Variables.Type.DateTime"),
            new(ResultValueKind.Point, "Ui.Job.Variables.Type.Point"),
            new(ResultValueKind.Rectangle, "Ui.Job.Variables.Type.Rectangle"),
            new(ResultValueKind.Image, "Ui.Job.Variables.Type.Image")
        ];
        if (!SupportedKinds.Contains(Model.ValueKind))
            KindOptions = [.. KindOptions, LegacyKindOption(Model.ValueKind)];
        LoadValue();
    }

    public JobVariable Model { get; }
    public IReadOnlyList<JobVariableKindOption> KindOptions { get; }
    public Guid Id => Model.Id;
    public bool IsStepValue => Model.Scope == JobVariableScope.StepValue;
    public bool IsShared => Model.Scope == JobVariableScope.Shared;
    public string ScopeLabel => Loc.Get(IsStepValue
        ? "Ui.Job.Variables.Scope.StepValues"
        : "Ui.Job.Variables.Scope.Shared");
    public int UsageCount { get; private set; }
    public string UsageText => Loc.Format(
        UsageCount == 1 ? "Ui.Job.Variables.Usage.One" : "Ui.Job.Variables.Usage.Many",
        UsageCount);
    public string UsageSummary { get; private set; } = string.Empty;
    public IReadOnlyList<string> UsageSteps { get; private set; } = [];
    public bool HasMultipleUsages => UsageCount > 1;
    public bool IsUsed => UsageCount > 0;
    public string SearchValue => ValueReferenceDisplayFormatter.Instance.CompactValue(Model);

    public void SetUsage(int count, string summary, IReadOnlyList<string>? steps = null)
    {
        UsageCount = Math.Max(0, count);
        UsageSummary = summary;
        UsageSteps = steps ?? [];
        OnPropertyChanged(nameof(UsageCount));
        OnPropertyChanged(nameof(UsageText));
        OnPropertyChanged(nameof(UsageSummary));
        OnPropertyChanged(nameof(HasMultipleUsages));
        OnPropertyChanged(nameof(IsUsed));
        OnPropertyChanged(nameof(UsageSteps));
    }

    public void PromoteToShared()
    {
        if (Model.Scope == JobVariableScope.Shared) return;
        Model.Scope = JobVariableScope.Shared;
        OnPropertyChanged(nameof(IsStepValue));
        OnPropertyChanged(nameof(IsShared));
        OnPropertyChanged(nameof(ScopeLabel));
        Changed();
    }

    public string Name
    {
        get => Model.Name;
        set
        {
            if (Model.Name == value) return;
            Model.Name = value;
            Changed();
        }
    }

    public string Description
    {
        get => Model.Description;
        set
        {
            if (Model.Description == value) return;
            Model.Description = value;
            Changed();
        }
    }

    public JobVariableKindOption SelectedKind
    {
        get => KindOptions.First(option => option.Kind == Model.ValueKind);
        set
        {
            if (value is null || Model.ValueKind == value.Kind) return;
            Model.ValueKind = value.Kind;
            Model.Cardinality = ResultCardinality.Single;
            SetDefaultValue();
            LoadValue();
            OnPropertyChanged();
            NotifyKindVisibility();
            Changed();
        }
    }

    private string _textValue = string.Empty;
    public string TextValue { get => _textValue; set { if (UpdateProperty(ref _textValue, value)) StoreValue(JsonValue.Create(value)); } }
    private bool _booleanValue;
    public bool BooleanValue { get => _booleanValue; set { if (UpdateProperty(ref _booleanValue, value)) StoreValue(JsonValue.Create(value)); } }
    private int _integerValue;
    public int IntegerValue { get => _integerValue; set { if (UpdateProperty(ref _integerValue, value)) StoreValue(JsonValue.Create(value)); } }
    private double _numberValue;
    public double NumberValue { get => _numberValue; set { if (UpdateProperty(ref _numberValue, value)) StoreValue(JsonValue.Create(value)); } }
    private DateTime _dateTimeValue;
    public DateTime DateTimeValue { get => _dateTimeValue; set { if (UpdateProperty(ref _dateTimeValue, value)) StoreValue(JsonValue.Create(value)); } }
    private string _imagePath = string.Empty;
    public string ImagePath { get => _imagePath; set { if (UpdateProperty(ref _imagePath, value)) StoreValue(JsonValue.Create(value)); } }
    private string _jsonValue = string.Empty;
    public string JsonText
    {
        get => _jsonValue;
        set
        {
            if (!UpdateProperty(ref _jsonValue, value)) return;
            try { StoreValue(JsonNode.Parse(value)); }
            catch (System.Text.Json.JsonException) { }
        }
    }
    private int _x;
    public int X { get => _x; set { if (UpdateProperty(ref _x, value)) StoreGeometry(); } }
    private int _y;
    public int Y { get => _y; set { if (UpdateProperty(ref _y, value)) StoreGeometry(); } }
    private int _width;
    public int Width { get => _width; set { if (UpdateProperty(ref _width, Math.Max(0, value))) StoreGeometry(); } }
    private int _height;
    public int Height { get => _height; set { if (UpdateProperty(ref _height, Math.Max(0, value))) StoreGeometry(); } }

    public bool IsText => Model.ValueKind is ResultValueKind.Text or ResultValueKind.Enum;
    public bool IsBoolean => Model.ValueKind == ResultValueKind.Boolean;
    public bool IsInteger => Model.ValueKind == ResultValueKind.Integer;
    public bool IsNumber => Model.ValueKind == ResultValueKind.Number;
    public bool IsDateTime => Model.ValueKind == ResultValueKind.DateTime;
    public bool IsPoint => Model.ValueKind == ResultValueKind.Point;
    public bool IsRectangle => Model.ValueKind == ResultValueKind.Rectangle;
    public bool IsImage => Model.ValueKind == ResultValueKind.Image;
    public bool IsResultObject => Model.ValueKind is ResultValueKind.ResultObject
        or ResultValueKind.Detection or ResultValueKind.ProcessReference;

    private void LoadValue()
    {
        _loading = true;
        try
        {
            _textValue = IsText ? Model.Value?.GetValue<string>() ?? string.Empty : string.Empty;
            _booleanValue = IsBoolean && (Model.Value?.GetValue<bool>() ?? false);
            _integerValue = IsInteger ? Model.Value?.GetValue<int>() ?? 0 : 0;
            _numberValue = IsNumber ? Model.Value?.GetValue<double>() ?? 0 : 0;
            _dateTimeValue = IsDateTime ? Model.Value?.GetValue<DateTime>() ?? DateTime.Now : DateTime.Now;
            _imagePath = IsImage ? Model.Value?.GetValue<string>() ?? string.Empty : string.Empty;
            _jsonValue = IsResultObject ? Model.Value?.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }) ?? "null" : string.Empty;
            _x = IsPoint || IsRectangle ? Model.Value?["x"]?.GetValue<int>() ?? 0 : 0;
            _y = IsPoint || IsRectangle ? Model.Value?["y"]?.GetValue<int>() ?? 0 : 0;
            _width = IsRectangle ? Model.Value?["width"]?.GetValue<int>() ?? 0 : 0;
            _height = IsRectangle ? Model.Value?["height"]?.GetValue<int>() ?? 0 : 0;
        }
        catch (InvalidOperationException)
        {
            SetDefaultValue();
            LoadValue();
            return;
        }
        finally
        {
            _loading = false;
        }
        OnPropertyChanged(nameof(TextValue));
        OnPropertyChanged(nameof(BooleanValue));
        OnPropertyChanged(nameof(IntegerValue));
        OnPropertyChanged(nameof(NumberValue));
        OnPropertyChanged(nameof(DateTimeValue));
        OnPropertyChanged(nameof(ImagePath));
        OnPropertyChanged(nameof(JsonText));
        OnPropertyChanged(nameof(X));
        OnPropertyChanged(nameof(Y));
        OnPropertyChanged(nameof(Width));
        OnPropertyChanged(nameof(Height));
    }

    private void SetDefaultValue()
    {
        Model.Value = Model.ValueKind switch
        {
            ResultValueKind.Text or ResultValueKind.Enum => JsonValue.Create(string.Empty),
            ResultValueKind.Boolean => JsonValue.Create(false),
            ResultValueKind.Integer => JsonValue.Create(0),
            ResultValueKind.Number => JsonValue.Create(0d),
            ResultValueKind.DateTime => JsonValue.Create(DateTime.Now),
            ResultValueKind.Point => new JsonObject { ["x"] = 0, ["y"] = 0 },
            ResultValueKind.Rectangle => new JsonObject { ["x"] = 0, ["y"] = 0, ["width"] = 0, ["height"] = 0 },
            ResultValueKind.Image => JsonValue.Create(string.Empty),
            ResultValueKind.ResultObject => new JsonObject(),
            ResultValueKind.Detection or ResultValueKind.ProcessReference => new JsonObject(),
            _ => null
        };
    }

    private void StoreGeometry()
    {
        if (_loading) return;
        Model.Value = IsRectangle
            ? new JsonObject { ["x"] = X, ["y"] = Y, ["width"] = Width, ["height"] = Height }
            : new JsonObject { ["x"] = X, ["y"] = Y };
        Changed();
    }

    private bool UpdateProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        SetProperty(ref field, value, propertyName);
        return true;
    }

    private void StoreValue(JsonNode? value)
    {
        if (_loading) return;
        Model.Value = value;
        Changed();
    }

    private void Changed()
    {
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(SearchValue));
        _changed();
    }

    private void NotifyKindVisibility()
    {
        OnPropertyChanged(nameof(IsText));
        OnPropertyChanged(nameof(IsBoolean));
        OnPropertyChanged(nameof(IsInteger));
        OnPropertyChanged(nameof(IsNumber));
        OnPropertyChanged(nameof(IsDateTime));
        OnPropertyChanged(nameof(IsPoint));
        OnPropertyChanged(nameof(IsRectangle));
        OnPropertyChanged(nameof(IsImage));
        OnPropertyChanged(nameof(IsResultObject));
    }

    private static JobVariableKindOption LegacyKindOption(ResultValueKind kind) => kind switch
    {
        ResultValueKind.ResultObject => new(kind, "Ui.Job.Variables.Type.Object"),
        ResultValueKind.Detection => new(kind, "Ui.Job.Variables.Type.Detection"),
        ResultValueKind.ProcessReference => new(kind, "Ui.Job.Variables.Type.Process"),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };
}

public sealed class JobVariableKindOption(ResultValueKind kind, string labelKey)
{
    public ResultValueKind Kind { get; } = kind;
    public string DisplayName => Loc.Get(labelKey);
}
