using System.Text.Json.Nodes;
using System.Runtime.CompilerServices;
using System.IO;
using System.Windows.Input;
using System.Windows.Media;
using DesktopAutomationApp.Localization;
using DesktopAutomationApp.Services.Jobs;
using Microsoft.Win32;
using TaskAutomation.Jobs;

namespace DesktopAutomationApp.ViewModels;

public sealed class JobVariableEditorViewModel : ViewModelBase
{
    public static IReadOnlySet<ResultValueKind> SupportedKinds { get; } = new HashSet<ResultValueKind>
    {
        ResultValueKind.Text,
        ResultValueKind.Boolean,
        ResultValueKind.Integer,
        ResultValueKind.Number,
        ResultValueKind.DateTime,
        ResultValueKind.Point,
        ResultValueKind.Rectangle,
        ResultValueKind.Color,
        ResultValueKind.FilePath
    };

    private readonly Action<string?> _changed;
    private bool _loading;

    public JobVariableEditorViewModel(JobVariable model, Action<string?> changed)
    {
        Model = model;
        _changed = changed;
        KindOptions =
        [
            new(ResultValueKind.Text, "Ui.Job.Variables.Type.Text"),
            new(ResultValueKind.Boolean, "Ui.Job.Variables.Type.Boolean"),
            new(ResultValueKind.Integer, "Ui.Job.Variables.Type.Integer"),
            new(ResultValueKind.Number, "Ui.Job.Variables.Type.Number"),
            new(ResultValueKind.DateTime, "Ui.Job.Variables.Type.DateTime"),
            new(ResultValueKind.Point, "Ui.Job.Variables.Type.Point"),
            new(ResultValueKind.Rectangle, "Ui.Job.Variables.Type.Rectangle"),
            new(ResultValueKind.Color, "Ui.Job.Variables.Type.Color"),
            new(ResultValueKind.FilePath, "Ui.Job.Variables.Type.FilePath")
        ];
        if (!SupportedKinds.Contains(Model.ValueKind))
            KindOptions = [.. KindOptions, LegacyKindOption(Model.ValueKind)];
        BrowseImageCommand = new RelayCommand(BrowseImage);
        BrowseFileCommand = new RelayCommand(BrowseFile);
        LoadValue();
    }

    public JobVariable Model { get; }
    public IReadOnlyList<JobVariableKindOption> KindOptions { get; }
    public ICommand BrowseImageCommand { get; }
    public ICommand BrowseFileCommand { get; }
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
    public IReadOnlyList<JobVariableUsageViewModel> UsageItems { get; private set; } = [];
    public bool HasMultipleUsages => UsageCount > 1;
    public bool IsUsed => UsageCount > 0;
    public string SearchValue => ValueReferenceDisplayFormatter.Instance.CompactValue(Model);
    public string CompactValue => SearchValue;
    public string TypeDisplayName => SelectedKind.DisplayName;

    public void SetUsage(int count, string summary, IReadOnlyList<JobVariableUsageViewModel>? usages = null)
    {
        UsageCount = Math.Max(0, count);
        UsageSummary = summary;
        UsageItems = usages ?? [];
        UsageSteps = UsageItems.Select(usage => usage.StepName).Distinct(StringComparer.CurrentCultureIgnoreCase).ToArray();
        OnPropertyChanged(nameof(UsageCount));
        OnPropertyChanged(nameof(UsageText));
        OnPropertyChanged(nameof(UsageSummary));
        OnPropertyChanged(nameof(HasMultipleUsages));
        OnPropertyChanged(nameof(IsUsed));
        OnPropertyChanged(nameof(UsageSteps));
        OnPropertyChanged(nameof(UsageItems));
    }

    public void PromoteToShared()
    {
        if (Model.Scope == JobVariableScope.Shared) return;
        Model.Scope = JobVariableScope.Shared;
        OnPropertyChanged(nameof(IsStepValue));
        OnPropertyChanged(nameof(IsShared));
        OnPropertyChanged(nameof(ScopeLabel));
        Changed(nameof(IsShared));
    }

    public string Name
    {
        get => Model.Name;
        set
        {
            if (Model.Name == value) return;
            Model.Name = value;
            Changed(nameof(Name));
        }
    }

    public string Description
    {
        get => Model.Description;
        set
        {
            if (Model.Description == value) return;
            Model.Description = value;
            Changed(nameof(Description));
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
            OnPropertyChanged(nameof(SelectedKindValue));
            OnPropertyChanged(nameof(TypeDisplayName));
            NotifyKindVisibility();
            Changed(nameof(SelectedKind));
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
    private Color _colorValue = Colors.White;
    public Color ColorValue
    {
        get => _colorValue;
        set
        {
            if (!UpdateProperty(ref _colorValue, value)) return;
            StoreValue(JsonValue.Create($"#{value.R:X2}{value.G:X2}{value.B:X2}"));
        }
    }

    public ResultValueKind SelectedKindValue
    {
        get => Model.ValueKind;
        set
        {
            var option = KindOptions.FirstOrDefault(candidate => candidate.Kind == value);
            if (option is not null) SelectedKind = option;
        }
    }
    private string _filePath = string.Empty;
    public string FilePath
    {
        get => _filePath;
        set { if (UpdateProperty(ref _filePath, value)) StoreValue(JsonValue.Create(value)); }
    }
    private string _imagePath = string.Empty;
    public string ImagePath
    {
        get => _imagePath;
        set
        {
            if (!UpdateProperty(ref _imagePath, value)) return;
            StoreValue(JsonValue.Create(value));
            RefreshImagePreview();
        }
    }
    public ImageSource? ImagePreview { get; private set; }
    public bool HasImagePreview => ImagePreview is not null;
    public bool HasImagePath => !string.IsNullOrWhiteSpace(ImagePath);
    public bool IsImagePathMissing => HasImagePath && !File.Exists(ImagePath);
    public string ImageStatus => !HasImagePath
        ? Loc.Get("Ui.Job.Variables.Image.Empty")
        : HasImagePreview
            ? Loc.Get("Ui.Job.Variables.Image.Available")
            : Loc.Get("Ui.Job.Variables.Image.Missing");
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
    public bool IsColor => Model.ValueKind == ResultValueKind.Color;
    public bool IsFilePath => Model.ValueKind == ResultValueKind.FilePath;
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
            _colorValue = IsColor && WpfColorParser.TryParse(Model.Value?.GetValue<string>(), out var color)
                ? color
                : Colors.White;
            _filePath = IsFilePath ? Model.Value?.GetValue<string>() ?? string.Empty : string.Empty;
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
        OnPropertyChanged(nameof(ColorValue));
        OnPropertyChanged(nameof(FilePath));
        OnPropertyChanged(nameof(ImagePath));
        OnPropertyChanged(nameof(JsonText));
        OnPropertyChanged(nameof(X));
        OnPropertyChanged(nameof(Y));
        OnPropertyChanged(nameof(Width));
        OnPropertyChanged(nameof(Height));
        RefreshImagePreview();
    }

    private void SetDefaultValue()
    {
        Model.Value = Model.ValueKind switch
        {
            ResultValueKind.Text or ResultValueKind.Enum or ResultValueKind.FilePath => JsonValue.Create(string.Empty),
            ResultValueKind.Boolean => JsonValue.Create(false),
            ResultValueKind.Integer => JsonValue.Create(0),
            ResultValueKind.Number => JsonValue.Create(0d),
            ResultValueKind.DateTime => JsonValue.Create(DateTime.Now),
            ResultValueKind.Color => JsonValue.Create("#FFFFFF"),
            ResultValueKind.Point => new JsonObject { ["x"] = 0, ["y"] = 0 },
            ResultValueKind.Rectangle => new JsonObject { ["x"] = 0, ["y"] = 0, ["width"] = 0, ["height"] = 0 },
            ResultValueKind.Image => JsonValue.Create(string.Empty),
            ResultValueKind.ResultObject => new JsonObject(),
            ResultValueKind.Detection or ResultValueKind.ProcessReference => new JsonObject(),
            _ => null
        };
    }

    private void StoreGeometry([CallerMemberName] string? propertyName = null)
    {
        if (_loading) return;
        Model.Value = IsRectangle
            ? new JsonObject { ["x"] = X, ["y"] = Y, ["width"] = Width, ["height"] = Height }
            : new JsonObject { ["x"] = X, ["y"] = Y };
        Changed(propertyName);
    }

    private bool UpdateProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        SetProperty(ref field, value, propertyName);
        return true;
    }

    private void StoreValue(JsonNode? value, [CallerMemberName] string? propertyName = null)
    {
        if (_loading) return;
        Model.Value = value;
        Changed(propertyName);
    }

    private void Changed(string? propertyName)
    {
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(SearchValue));
        OnPropertyChanged(nameof(CompactValue));
        _changed(propertyName);
    }

    private void BrowseImage()
    {
        var dialog = new OpenFileDialog
        {
            Title = Loc.Get("Ui.Job.Variables.Image.Select"),
            Filter = Loc.Get("Ui.Job.Variables.Image.Filter"),
            CheckFileExists = true,
            Multiselect = false
        };
        if (HasImagePath && Directory.Exists(Path.GetDirectoryName(ImagePath)))
            dialog.InitialDirectory = Path.GetDirectoryName(ImagePath);
        if (dialog.ShowDialog() == true) ImagePath = dialog.FileName;
    }

    private void BrowseFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = Loc.Get("Ui.Job.Variables.FilePath.Select"),
            Filter = Loc.Get("Ui.Job.Variables.FilePath.Filter"),
            CheckFileExists = true,
            Multiselect = false
        };
        if (!string.IsNullOrWhiteSpace(FilePath) && Directory.Exists(Path.GetDirectoryName(FilePath)))
            dialog.InitialDirectory = Path.GetDirectoryName(FilePath);
        if (dialog.ShowDialog() == true) FilePath = dialog.FileName;
    }

    private void RefreshImagePreview()
    {
        ImagePreview = IsImage ? WpfImagePreviewLoader.TryLoad(ImagePath) : null;
        OnPropertyChanged(nameof(ImagePreview));
        OnPropertyChanged(nameof(HasImagePreview));
        OnPropertyChanged(nameof(HasImagePath));
        OnPropertyChanged(nameof(IsImagePathMissing));
        OnPropertyChanged(nameof(ImageStatus));
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
        OnPropertyChanged(nameof(IsColor));
        OnPropertyChanged(nameof(IsFilePath));
        OnPropertyChanged(nameof(IsImage));
        OnPropertyChanged(nameof(IsResultObject));
    }

    private static JobVariableKindOption LegacyKindOption(ResultValueKind kind) => kind switch
    {
        ResultValueKind.ResultObject => new(kind, "Ui.Job.Variables.Type.Object"),
        ResultValueKind.Detection => new(kind, "Ui.Job.Variables.Type.Detection"),
        ResultValueKind.ProcessReference => new(kind, "Ui.Job.Variables.Type.Process"),
        ResultValueKind.Enum => new(kind, "Ui.Job.Variables.Type.Enum"),
        ResultValueKind.Image => new(kind, "Ui.Job.Variables.Type.Image"),
        ResultValueKind.JobReference => new(kind, "Ui.Job.Variables.Type.JobReference"),
        ResultValueKind.MacroReference => new(kind, "Ui.Job.Variables.Type.MacroReference"),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };
}

public sealed record JobVariableUsageViewModel(
    JobStep Step,
    string StepName,
    string InputName,
    string SearchText);

public sealed class JobVariableKindOption(ResultValueKind kind, string labelKey)
{
    public ResultValueKind Kind { get; } = kind;
    public string DisplayName => Loc.Get(labelKey);
}

public sealed class JobVariableTypeFilterOption(ResultValueKind? kind, string labelKey)
{
    public ResultValueKind? Kind { get; } = kind;
    public string DisplayName => Loc.Get(labelKey);
}
