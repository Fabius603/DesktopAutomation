using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TaskAutomation.Steps;
using System.ComponentModel;
using System.Linq;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using DesktopAutomationApp.Services.Preview;
using DesktopAutomationApp.Services;
using DesktopAutomationApp.Views;
using Microsoft.Win32;
using TaskAutomation.Jobs;
using TaskAutomation.Makros;
using DesktopAutomationApp.Localization;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Steps.Definitions;

namespace DesktopAutomationApp.ViewModels
{
    public sealed class AddJobStepDialogViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private int _notificationDeferral;
        private bool _notificationPending;

        private void OnChange([CallerMemberName] string? p = null)
        {
            if (_notificationDeferral > 0)
            {
                _notificationPending = true;
                return;
            }
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
            RaiseConfirmCanExecuteChanged();
        }

        public IDisposable DeferNotifications()
        {
            _notificationDeferral++;
            return new NotificationScope(this);
        }

        private void EndNotificationDeferral()
        {
            if (_notificationDeferral == 0 || --_notificationDeferral > 0) return;
            if (!_notificationPending) return;
            _notificationPending = false;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
            RaiseConfirmCanExecuteChanged();
        }

        private sealed class NotificationScope(AddJobStepDialogViewModel owner) : IDisposable
        {
            private AddJobStepDialogViewModel? _owner = owner;
            public void Dispose()
            {
                _owner?.EndNotificationDeferral();
                _owner = null;
            }
        }

        private void RaiseConfirmCanExecuteChanged()
        {
            if (_notificationDeferral > 0)
            {
                _notificationPending = true;
                return;
            }
            (ConfirmCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private readonly IJobExecutor _ctx;
        private readonly IReadOnlyList<JobStep> _precedingSteps;
        private readonly IReadOnlyList<JobStep> _allJobSteps;
        private readonly IReadOnlyList<SourceStepItem> _conditionSourceSteps;
        private readonly Guid? _currentJobId;
        private readonly ICameraCaptureService _cameraCaptureService;
        private readonly IStepDefinitionCatalog _stepDefinitionCatalog;
        public ObservableCollection<Job> AvailableJobs { get; }
        public ObservableCollection<Makro> AvailableMakros { get; }

        public sealed record PreparedSources(IReadOnlyList<SourceStepItem> Conditions);

        public static Task<PreparedSources> PrepareSourcesAsync(
            IReadOnlyList<JobStep> precedingSteps,
            CancellationToken cancellationToken = default)
            => Task.Run(() => new PreparedSources(
                BuildConditionSourceCatalog(precedingSteps)), cancellationToken);

        public AddJobStepDialogViewModel(
            IJobExecutor ctx,
            IReadOnlyList<JobStep> precedingSteps,
            Guid? currentJobId = null,
            IReadOnlyList<JobStep>? allJobSteps = null,
            PreparedSources? preparedSources = null,
            ICameraCaptureService? cameraCaptureService = null,
            IStepDefinitionCatalog? stepDefinitionCatalog = null)
        {
            _ctx = ctx;
            _precedingSteps = precedingSteps;
            _allJobSteps = allJobSteps ?? precedingSteps;
            _currentJobId = currentJobId;
            _cameraCaptureService = cameraCaptureService
                ?? throw new ArgumentNullException(nameof(cameraCaptureService));
            _stepDefinitionCatalog = stepDefinitionCatalog ?? BuiltInStepDefinitions.Instance;
            StepTypeItems = CreateStepTypeItems(_stepDefinitionCatalog);
            AvailableJobs = new ObservableCollection<Job>(
                (_ctx.AllJobs?.Values ?? Enumerable.Empty<Job>())
                .Where(job => job.Id != _currentJobId)
                .OrderBy(job => job.Name));
            AvailableMakros = new ObservableCollection<Makro>(
                (_ctx.AllMakros?.Values ?? Enumerable.Empty<Makro>())
                .OrderBy(makro => makro.Name));
            _conditionSourceSteps = preparedSources?.Conditions ?? BuildConditionSourceCatalog(precedingSteps);
            ConfirmCommand = new RelayCommand(Confirm, CanConfirm);
            CancelCommand = new RelayCommand(() => RequestClose?.Invoke(false));
            BrowseGeneratedFileCommand = new RelayCommand<GeneratedStepFieldViewModel?>(BrowseGeneratedFile);
            BrowseGeneratedDirectoryCommand = new RelayCommand<GeneratedStepFieldViewModel?>(BrowseGeneratedDirectory);
            BrowseGeneratedFileOrFolderCommand = new RelayCommand<GeneratedStepFieldViewModel?>(BrowseGeneratedFileOrFolder);
            BrowseGeneratedProcessTargetFileCommand = new RelayCommand<GeneratedProcessTargetEditorViewModel?>(BrowseGeneratedProcessTargetFile);
            CaptureGeneratedRoiCommand = new RelayCommand<GeneratedRoiEditorViewModel?>(CaptureGeneratedRoi);
            ChooseMonitorCommand = new RelayCommand<GeneratedStepFieldViewModel?>(ChooseMonitor);
            SetGeneratedEditor(_selectedType);
            _ = LoadInstalledProgramsAsync();
        }

        private async Task LoadInstalledProgramsAsync()
        {
            try
            {
                var programs = await InstalledProgramDiscovery.DiscoverAsync();
                _availableStartPrograms.ReplaceRange(programs);
                _availableExecutablePrograms.ReplaceRange(
                    programs.Where(program => program.IsDirectExecutable
                                              && !string.IsNullOrWhiteSpace(program.ProcessName)));
                _availableProcessNames.ReplaceRange(
                    programs.Select(program => program.ProcessName)
                        .Where(name => !string.IsNullOrWhiteSpace(name))
                        .Distinct(StringComparer.OrdinalIgnoreCase));
                GeneratedEditor?.RefreshSuggestions(ResolveGeneratedSuggestions);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            catch (ArgumentException) { }
        }

        // ----- Dialog-Interop -----
        public event Action<bool>? RequestClose; // true = OK, false = Cancel

        /// <summary>Lädt optionale, dateisystembasierte Daten erst nach dem Anzeigen des Dialogs.</summary>
        public async Task InitializeAsync()
        {
            var initializations = GeneratedEditor?.Fields
                .SelectMany(field => new[] { field.CameraEditor?.Initialization, field.YoloEditor?.Initialization })
                .OfType<Task>()
                .ToArray() ?? [];
            await Task.WhenAll(initializations);
        }

        private StepDialogMode _mode = StepDialogMode.Add;
        public StepDialogMode Mode
        {
            get => _mode;
            set { _mode = value; OnChange(); OnChange(nameof(DialogTitle)); OnChange(nameof(ConfirmButtonText)); }
        }

        // ----- Type-Lock (für intern erzeugten ElseIf-Dialog) -----
        private bool _isTypeLocked;
        public bool IsTypeLocked
        {
            get => _isTypeLocked;
            set { _isTypeLocked = value; OnChange(); OnChange(nameof(ShowTypeSelector)); OnChange(nameof(DialogTitle)); }
        }
        public bool ShowTypeSelector => !IsTypeLocked;

        public string DialogTitle =>
            IsTypeLocked
                ? (SelectedType == "ElseIf"
                    ? Loc.Get(Mode == StepDialogMode.Edit ? "Step.ElseIf.Edit" : "Step.ElseIf.Add")
                    : Loc.Get(Mode == StepDialogMode.Edit ? "Step.Edit" : "Step.Add"))
                : Loc.Get(Mode == StepDialogMode.Edit ? "JobStep.Edit" : "JobStep.Add");
        public string ConfirmButtonText => Loc.Get(Mode == StepDialogMode.Edit ? "Common.Apply" : "Common.Add");

        // ----- Commands -----
        public ICommand ConfirmCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand BrowseGeneratedFileCommand { get; }
        public ICommand BrowseGeneratedDirectoryCommand { get; }
        public ICommand BrowseGeneratedFileOrFolderCommand { get; }
        public ICommand BrowseGeneratedProcessTargetFileCommand { get; }
        public ICommand CaptureGeneratedRoiCommand { get; }
        public ICommand ChooseMonitorCommand { get; }

        private void Confirm()
        {
            CreateStep();
            RequestClose?.Invoke(true);
        }

        private bool CanConfirm()
        {
            CreateStep();
            if (GeneratedEditor?.ValidationError is { Length: > 0 } generatedError)
            {
                _validationError = generatedError;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ValidationError)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasValidationError)));
                return false;
            }
            var result = JobValidation.ValidateCandidate(_precedingSteps, CreatedStep, _allJobSteps);
            _validationError = result.Error;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ValidationError)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasValidationError)));
            return result.IsValid;
        }

        private string? _validationError;
        public string? ValidationError => _validationError;
        public bool HasValidationError => !string.IsNullOrWhiteSpace(_validationError);

        // ----- Step-Auswahl -----
        /// <param name="Description">Text, der im Dialog unterhalb des Typ-Selektors angezeigt wird.</param>
        public sealed class StepTypeItem
        {
            private readonly string _category;
            private readonly string _description;
            private readonly string _displayNameKey;
            private readonly string _descriptionKey;
            public string Name { get; }
            public string CategoryKey => _category;
            public string Category => LocalizedOrFallback($"Step.Category.{_category}", _category);
            public string Description => LocalizedOrFallback(_descriptionKey, _description);
            public string DisplayLabel => LocalizedOrFallback(_displayNameKey, Name);

            public StepTypeItem(
                string name,
                string category,
                string description = "",
                string? displayNameKey = null,
                string? descriptionKey = null)
            {
                Name = name;
                _category = category;
                _description = description;
                _displayNameKey = displayNameKey ?? $"Step.Type.{Name}";
                _descriptionKey = descriptionKey ?? $"Step.Description.{Name}";
            }

            private static string LocalizedOrFallback(string key, string fallback)
            {
                var value = LocalizationService.Instance[key];
                return value == $"[{key}]" ? fallback : value;
            }
        }

        public ListCollectionView StepTypeItems { get; }

        internal static ListCollectionView CreateStepTypeItems(IStepDefinitionCatalog stepDefinitionCatalog)
        {
            var items = stepDefinitionCatalog.Definitions
                .Where(definition => definition.StepType != typeof(ElseIfStep)
                                     && definition.StepType != typeof(ElseStep)
                                     && definition.StepType != typeof(EndIfStep))
                .Select(definition => new StepTypeItem(
                    TrimStepSuffix(definition.StepType.Name),
                    definition.Descriptor.CategoryId,
                    displayNameKey: definition.Descriptor.DisplayNameKey,
                    descriptionKey: definition.Descriptor.DescriptionKey))
                .ToList();
            string[] categoryOrder =
            [
                "BildAufnehmen",
                "BildAuswerten",
                "MausTastatur",
                "ProgrammeFenster",
                "DateienOrdner",
                "WindowsSystem",
                "AnzeigenSpeichern",
                "AblaufSteuern"
            ];
            items = items
                .OrderBy(item => Array.IndexOf(categoryOrder, item.CategoryKey))
                .ToList();

            var view = new ListCollectionView(items);
            view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(StepTypeItem.Category)));
            return view;
        }

        private string _stepTypeSearchText = string.Empty;
        public string StepTypeSearchText
        {
            get => _stepTypeSearchText;
            set
            {
                if (_stepTypeSearchText == value) return;
                _stepTypeSearchText = value;
                StepTypeItems.Filter = FilterStepType;
                StepTypeItems.Refresh();
                OnChange();
            }
        }

        private bool FilterStepType(object item)
        {
            if (item is not StepTypeItem stepType || string.IsNullOrWhiteSpace(StepTypeSearchText))
                return true;
            var search = StepTypeSearchText.Trim();
            return stepType.DisplayLabel.Contains(search, StringComparison.CurrentCultureIgnoreCase)
                   || stepType.Category.Contains(search, StringComparison.CurrentCultureIgnoreCase)
                   || stepType.Description.Contains(search, StringComparison.CurrentCultureIgnoreCase);
        }

        private string _selectedType = "DesktopDuplication";
        private GeneratedStepEditorViewModel? _generatedEditor;

        public GeneratedStepEditorViewModel? GeneratedEditor
        {
            get => _generatedEditor;
            private set
            {
                if (ReferenceEquals(_generatedEditor, value)) return;
                if (_generatedEditor is not null)
                    _generatedEditor.Changed -= OnGeneratedEditorChanged;
                _generatedEditor = value;
                if (_generatedEditor is not null)
                    _generatedEditor.Changed += OnGeneratedEditorChanged;
                OnChange();
            }
        }

        public string SelectedType
        {
            get => _selectedType;
            set
            {
                // A filtered ListBox temporarily clears SelectedValue when no item matches.
                // Keep the last valid step type so clearing or changing the search can restore it.
                if (string.IsNullOrWhiteSpace(value) || _selectedType == value) return;
                _selectedType = value;
                SetGeneratedEditor(value);
                OnChange(string.Empty);
            }
        }

        public bool TryLoadGeneratedStep(JobStep step)
        {
            if (step is StartProcessStep { Settings.Action: StartProcessAction.Terminate } legacyTerminate
                && _stepDefinitionCatalog.TryGetByType(typeof(TerminateProcessStep), out var terminateDefinition))
            {
                var migrated = new TerminateProcessStep
                {
                    Id = legacyTerminate.Id,
                    IsEnabled = legacyTerminate.IsEnabled,
                    IsBreakpoint = legacyTerminate.IsBreakpoint,
                    Settings = new TerminateProcessSettings { Target = legacyTerminate.Settings.Target }
                };
                _selectedType = "TerminateProcess";
                GeneratedEditor = CreateGeneratedEditor(terminateDefinition, migrated);
                OnChange(nameof(SelectedType));
                OnChange(string.Empty);
                return true;
            }
            if (!_stepDefinitionCatalog.TryGetByType(step.GetType(), out var definition))
                return false;
            _selectedType = TrimStepSuffix(step.GetType().Name);
            GeneratedEditor = CreateGeneratedEditor(definition, step);
            OnChange(nameof(SelectedType));
            OnChange(string.Empty);
            return true;
        }

        private void SetGeneratedEditor(string selectedType)
        {
            GeneratedEditor = _stepDefinitionCatalog.TryGetByName(selectedType, out var definition)
                ? CreateGeneratedEditor(definition)
                : null;
        }

        private GeneratedStepEditorViewModel CreateGeneratedEditor(
            IStepDefinition definition,
            JobStep? step = null) =>
            new(
                definition,
                step,
                ResolveGeneratedSuggestions,
                ResolveGeneratedChoices,
                (field, value) => ResolveGeneratedProcessTarget(definition, field, value),
                (field, value) => ResolveGeneratedResultBinding(definition, field, value),
                ResolveGeneratedCamera,
                (field, value) => ResolveGeneratedVisualOverlay(definition, field, value),
                (field, value) => ResolveGeneratedRoi(definition, field, value),
                ResolveGeneratedYolo,
                ResolveGeneratedCondition,
                ResolveGeneratedWindowsCapability,
                ResolveGeneratedScreenPoint,
                ResolveGeneratedUserChoiceOptions,
                ResolveGeneratedPointEntryList,
                ResolveGeneratedAxisExpressionList);

        private IEnumerable<string>? ResolveGeneratedSuggestions(StepFieldDescriptor field) =>
            field.EditorHint switch
            {
                StepEditorHints.ProcessNameSuggestions => _availableProcessNames,
                StepEditorHints.ExecutablePathSuggestions =>
                    _availableExecutablePrograms.Select(program => program.Command),
                StepEditorHints.StartProgramPicker =>
                    _availableStartPrograms.Select(program => program.Command),
                _ => null
            };

        private IEnumerable<GeneratedStepChoiceOptionViewModel>? ResolveGeneratedChoices(
            StepFieldDescriptor field) => field.EditorHint switch
            {
                StepEditorHints.MacroPicker => AvailableMakros.Select(macro =>
                    new GeneratedStepChoiceOptionViewModel(new StepReferenceValue(
                        macro.Id.ToString("D"), macro.Name))),
                StepEditorHints.JobPicker => AvailableJobs.Select(job =>
                    new GeneratedStepChoiceOptionViewModel(new StepReferenceValue(
                        job.Id.ToString("D"), job.Name))),
                _ => null
            };

        private GeneratedProcessTargetEditorViewModel? ResolveGeneratedProcessTarget(
            IStepDefinition definition,
            StepFieldDescriptor field,
            System.Text.Json.Nodes.JsonNode? value)
        {
            if (!string.Equals(field.EditorHint, StepEditorHints.ProcessTargetPicker, StringComparison.Ordinal)
                && !string.Equals(field.EditorHint, StepEditorHints.ExecutableProcessTargetPicker, StringComparison.Ordinal))
                return null;
            var contract = StepInputContractRegistry.Get(definition.StepType, "process")
                ?? throw new InvalidOperationException(
                    $"Eingabevertrag 'process' für {definition.StepType.Name} fehlt.");
            return new GeneratedProcessTargetEditorViewModel(
                value,
                new ResultBindingPickerViewModel(_conditionSourceSteps, contract, false),
                _availableProcessNames,
                string.Equals(field.EditorHint, StepEditorHints.ExecutableProcessTargetPicker, StringComparison.Ordinal));
        }

        private GeneratedResultBindingEditorViewModel? ResolveGeneratedResultBinding(
            IStepDefinition definition,
            StepFieldDescriptor field,
            System.Text.Json.Nodes.JsonNode? value)
        {
            if (!string.Equals(field.EditorHint, StepEditorHints.ResultBindingPicker, StringComparison.Ordinal))
                return null;
            if (string.IsNullOrWhiteSpace(field.InputContractId))
                throw new InvalidOperationException($"Eingabevertrag für {definition.StepType.Name} fehlt im Descriptor.");
            var contract = StepInputContractRegistry.Get(definition.StepType, field.InputContractId)
                ?? throw new InvalidOperationException(
                    $"Eingabevertrag '{field.InputContractId}' für {definition.StepType.Name} fehlt.");
            return new GeneratedResultBindingEditorViewModel(
                value,
                new ResultBindingPickerViewModel(_conditionSourceSteps, contract, field.Required));
        }

        private GeneratedCameraEditorViewModel? ResolveGeneratedCamera(
            StepFieldDescriptor field,
            System.Text.Json.Nodes.JsonNode? value) =>
            string.Equals(field.EditorHint, StepEditorHints.CameraPicker, StringComparison.Ordinal)
                ? new GeneratedCameraEditorViewModel(value, _cameraCaptureService)
                : null;

        private GeneratedVisualOverlayEditorViewModel? ResolveGeneratedVisualOverlay(
            IStepDefinition definition,
            StepFieldDescriptor field,
            System.Text.Json.Nodes.JsonNode? value)
        {
            if (!string.Equals(field.EditorHint, StepEditorHints.VisualOverlay, StringComparison.Ordinal))
                return null;
            var options = field.VisualOverlayOptions
                ?? throw new InvalidOperationException(
                    $"Visual-overlay options for {definition.StepType.Name}.{field.Id} are missing.");
            var detectionContract = StepInputContractRegistry.Get(
                definition.StepType, options.DetectionInputContractId)
                ?? throw new InvalidOperationException(
                    $"Input contract '{options.DetectionInputContractId}' for {definition.StepType.Name} is missing.");
            var textContract = StepInputContractRegistry.Get(
                definition.StepType, options.TextInputContractId)
                ?? throw new InvalidOperationException(
                    $"Input contract '{options.TextInputContractId}' for {definition.StepType.Name} is missing.");
            return new GeneratedVisualOverlayEditorViewModel(
                value,
                _conditionSourceSteps,
                detectionContract,
                textContract,
                options.SupportsDesktopPlacement,
                ChooseMonitorForOverlayText);
        }

        private GeneratedRoiEditorViewModel? ResolveGeneratedRoi(
            IStepDefinition definition,
            StepFieldDescriptor field,
            System.Text.Json.Nodes.JsonNode? value)
        {
            if (!string.Equals(field.EditorHint, StepEditorHints.RoiPicker, StringComparison.Ordinal))
                return null;
            var options = field.RoiPickerOptions
                ?? throw new InvalidOperationException(
                    $"ROI-picker options for {definition.StepType.Name}.{field.Id} are missing.");
            var contract = StepInputContractRegistry.Get(definition.StepType, options.DynamicInputContractId)
                ?? throw new InvalidOperationException(
                    $"Input contract '{options.DynamicInputContractId}' for {definition.StepType.Name} is missing.");
            return new GeneratedRoiEditorViewModel(
                value,
                new ResultBindingPickerViewModel(_conditionSourceSteps, contract, false));
        }

        private GeneratedYoloEditorViewModel? ResolveGeneratedYolo(
            StepFieldDescriptor field,
            System.Text.Json.Nodes.JsonNode? value) =>
            string.Equals(field.EditorHint, StepEditorHints.YoloPicker, StringComparison.Ordinal)
                ? new GeneratedYoloEditorViewModel(
                    value,
                    () => _ctx.YoloManager?.GetAvailableModels() ?? [],
                    model => _ctx.YoloManager?.GetClassesForModel(model) ?? [],
                    model => _ctx.YoloManager?.GetRecommendedConfidenceThreshold(model))
                : null;

        private GeneratedConditionEditorViewModel? ResolveGeneratedCondition(
            StepFieldDescriptor field,
            System.Text.Json.Nodes.JsonNode? value) =>
            string.Equals(field.EditorHint, StepEditorHints.ConditionEditor, StringComparison.Ordinal)
                ? new GeneratedConditionEditorViewModel(value, _conditionSourceSteps)
                : null;

        private static GeneratedWindowsCapabilityEditorViewModel? ResolveGeneratedWindowsCapability(
            StepFieldDescriptor field,
            System.Text.Json.Nodes.JsonNode? value) =>
            string.Equals(field.EditorHint, StepEditorHints.WindowsCapabilityPicker, StringComparison.Ordinal)
                ? new GeneratedWindowsCapabilityEditorViewModel(
                    value,
                    field.WindowsCapabilityPickerOptions?.Mode
                    ?? throw new InvalidOperationException(
                        $"Windows capability options for field '{field.Id}' are missing."))
                : null;

        private GeneratedScreenPointEditorViewModel? ResolveGeneratedScreenPoint(
            StepFieldDescriptor field,
            System.Text.Json.Nodes.JsonNode? value) =>
            field.EditorHint == StepEditorHints.ScreenPointPicker
                ? new GeneratedScreenPointEditorViewModel(
                    value,
                    NormalizeScreenPoint,
                    SelectMonitorForGeneratedEditor,
                    CaptureGeneratedScreenPointAsync)
                : null;

        private static GeneratedUserChoiceOptionsEditorViewModel? ResolveGeneratedUserChoiceOptions(
            StepFieldDescriptor field,
            System.Text.Json.Nodes.JsonNode? value) =>
            field.EditorHint == StepEditorHints.UserChoiceOptions
                ? new GeneratedUserChoiceOptionsEditorViewModel(value)
                : null;

        private GeneratedPointEntryListEditorViewModel? ResolveGeneratedPointEntryList(
            StepFieldDescriptor field,
            System.Text.Json.Nodes.JsonNode? value) =>
            field.EditorHint == StepEditorHints.PointEntryList
                ? new GeneratedPointEntryListEditorViewModel(value, _conditionSourceSteps)
                : null;

        private static GeneratedAxisExpressionListEditorViewModel? ResolveGeneratedAxisExpressionList(
            StepFieldDescriptor field,
            System.Text.Json.Nodes.JsonNode? value) =>
            field.EditorHint == StepEditorHints.AxisExpressionList
                ? new GeneratedAxisExpressionListEditorViewModel(value)
                : null;

        private int? SelectMonitorForGeneratedEditor()
        {
            try
            {
                var selected = ShowMonitorSelectionOverlay();
                return selected >= 0 ? selected : null;
            }
            catch (Exception ex)
            {
                AppDialog.Show(Loc.Format("Error.MonitorSelection", ex.Message), Loc.Get("Error.Title"),
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return null;
            }
        }

        private static StepScreenPointSelectionValue NormalizeScreenPoint(StepScreenPointSelectionValue value)
        {
            if (value.CoordinateSpace.Equals(KlickOnPoint3DSettings.MonitorLocalCoordinates, StringComparison.OrdinalIgnoreCase))
                return value;
            return ToMonitorLocalPoint(new System.Drawing.Point(value.X, value.Y));
        }

        private static StepScreenPointSelectionValue ToMonitorLocalPoint(System.Drawing.Point point)
        {
            var screens = ImageHelperMethods.ScreenHelper.GetScreens();
            var monitorIndex = Array.FindIndex(screens, screen => screen.Bounds.Contains(point));
            if (monitorIndex < 0) monitorIndex = GetPrimaryMonitorIndex();
            var bounds = screens.Length > monitorIndex && monitorIndex >= 0
                ? screens[monitorIndex].Bounds
                : System.Drawing.Rectangle.Empty;
            return new(Math.Max(0, monitorIndex), point.X - bounds.Left, point.Y - bounds.Top,
                KlickOnPoint3DSettings.MonitorLocalCoordinates);
        }

        private static async Task<StepScreenPointSelectionValue?> CaptureGeneratedScreenPointAsync()
        {
            try
            {
                var overlay = new DesktopOverlay.RoiCaptureOverlay();
                return ToMonitorLocalPoint(await overlay.CapturePointAsync());
            }
            catch (OperationCanceledException) { return null; }
            catch (Exception ex)
            {
                AppDialog.Show(Loc.Format("Error.CapturePoint", ex.Message), Loc.Get("Error.Title"),
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return null;
            }
        }

        private void OnGeneratedEditorChanged() => OnChange(nameof(GeneratedEditor));

        private static string TrimStepSuffix(string name) =>
            name.EndsWith("Step", StringComparison.Ordinal) ? name[..^4] : name;

        // Beschreibung kommt direkt aus dem StepTypeItem – kein separates switch mehr nötig.
        public string StepTypeDescription =>
            StepTypeItems.Cast<StepTypeItem>().FirstOrDefault(i => i.Name == SelectedType)?.Description ?? string.Empty;
        public string SelectedStepDisplayName =>
            StepTypeItems.Cast<StepTypeItem>().FirstOrDefault(i => i.Name == SelectedType)?.DisplayLabel ?? SelectedType;
        public string SelectedStepCategory =>
            StepTypeItems.Cast<StepTypeItem>().FirstOrDefault(i => i.Name == SelectedType)?.Category ?? string.Empty;

        /// <summary>Voraussetzung eines Steps mit Information ob sie durch vorherige Steps erfüllt ist.</summary>
        // ----- Quell-Step-Helfer -----

        /// <summary>
        /// Builds a list of all preceding steps that produce a result of the given type name.
        /// </summary>
        private static IReadOnlyList<SourceStepItem> BuildConditionSourceCatalog(
            IReadOnlyList<JobStep> precedingSteps)
            => StepResultMetadata.GetConditionSources(precedingSteps, precedingSteps.Count)
                .Select(source => new SourceStepItem(
                    source.Step.Id,
                    StepLocalization.ResultStepName(source.Step, precedingSteps),
                    StepLocalization.ResultType(source.ResultType)))
                .ToArray();

        // ----- Ergebnis -----
        public JobStep? CreatedStep { get; private set; }

        // ===== TemplateMatching Felder =====
        // ===== KlickOnPoint3D Felder =====
        private static int GetPrimaryMonitorIndex()
        {
            var screens = ImageHelperMethods.ScreenHelper.GetScreens();
            var primaryDeviceName = Screen.PrimaryScreen?.DeviceName;
            var index = Array.FindIndex(screens, screen => screen.DeviceName == primaryDeviceName);
            return Math.Max(0, index);
        }

        private void BrowseGeneratedFile(GeneratedStepFieldViewModel? field)
        {
            if (field is null) return;
            var ofd = new Microsoft.Win32.OpenFileDialog
            {
                Title = Loc.Get(field.Descriptor.FilePickerOptions?.Kind == StepFilePickerKind.Image
                    ? "FilePicker.Template"
                    : field.UsesSuggestionFilePicker ? "FilePicker.Executable" : "FilePicker.Script"),
                Filter = field.Descriptor.FilePickerOptions?.Kind == StepFilePickerKind.Image
                    ? "Bilder (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|Alle Dateien (*.*)|*.*"
                    : "Skripte (*.ps1;*.bat;*.cmd;*.sh;*.py;*.js;*.vbs;*.wsf;*.exe)|*.ps1;*.bat;*.cmd;*.sh;*.py;*.js;*.vbs;*.wsf;*.exe|Alle Dateien (*.*)|*.*",
                CheckFileExists = true,
                Multiselect = false
            };

            if (ofd.ShowDialog() == true)
                field.InputText = ofd.FileName;
        }

        private void BrowseGeneratedFileOrFolder(GeneratedStepFieldViewModel? field)
        {
            if (field is not null && TryBrowseFileOrFolder(field.InputText, out var selected))
                field.InputText = selected;
        }

        private static void BrowseGeneratedDirectory(GeneratedStepFieldViewModel? field)
        {
            if (field is null || !field.UsesDirectoryPicker) return;
            using var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = field.Label,
                UseDescriptionForTitle = true,
                ShowNewFolderButton = true,
                SelectedPath = Directory.Exists(field.InputText) ? field.InputText : string.Empty
            };
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                field.InputText = dialog.SelectedPath;
        }

        private void BrowseGeneratedProcessTargetFile(GeneratedProcessTargetEditorViewModel? editor)
        {
            if (editor is null) return;
            var ofd = new Microsoft.Win32.OpenFileDialog
            {
                Title = Loc.Get("FilePicker.Executable"),
                Filter = "Programme (*.exe;*.bat;*.cmd;*.ps1)|*.exe;*.bat;*.cmd;*.ps1|Alle Dateien (*.*)|*.*",
                CheckFileExists = true,
                Multiselect = false
            };

            if (ofd.ShowDialog() == true)
                editor.ExecutablePath = ofd.FileName;
        }



        private void ChooseMonitor(GeneratedStepFieldViewModel? field)
        {
            if (field is null || !field.UsesMonitorPicker) return;
            try
            {
                int selectedMonitorIndex = ShowMonitorSelectionOverlay();
                if (selectedMonitorIndex >= 0)
                    field.IntegerValue = selectedMonitorIndex;
            }
            catch (Exception ex)
            {
                AppDialog.Show(Loc.Format("Error.MonitorSelection", ex.Message), Loc.Get("Error.Title"),
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }


        private int ShowMonitorSelectionOverlay()
        {
            var screens = ImageHelperMethods.ScreenHelper.GetScreens();
            var overlays = new List<System.Windows.Window>();
            int selectedIndex = -1;
            bool selectionMade = false;

            try
            {
                // Create overlay windows for each monitor
                for (int i = 0; i < screens.Length; i++)
                {
                    var screen = screens[i];
                    int monitorIndex = i; // Capture loop variable

                    var overlay = new System.Windows.Window
                    {
                        WindowStyle = System.Windows.WindowStyle.None,
                        AllowsTransparency = true,
                        Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(128, 0, 100, 200)),
                        Topmost = true,
                        Left = screen.Bounds.Left,
                        Top = screen.Bounds.Top,
                        Width = screen.Bounds.Width,
                        Height = screen.Bounds.Height,
                        Cursor = System.Windows.Input.Cursors.Hand
                    };

                    // Add text to show monitor index
                    var textBlock = new System.Windows.Controls.TextBlock
                    {
                        Text = $"Monitor {i}",
                        FontSize = 48,
                        Foreground = System.Windows.Media.Brushes.White,
                        HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                        VerticalAlignment = System.Windows.VerticalAlignment.Center,
                        FontWeight = System.Windows.FontWeights.Bold
                    };
                    overlay.Content = textBlock;

                    // Handle click
                    overlay.MouseLeftButtonDown += (s, e) =>
                    {
                        if (!selectionMade)
                        {
                            selectedIndex = monitorIndex;
                            selectionMade = true;
                            
                            // Close all overlays
                            foreach (var o in overlays)
                            {
                                o.Close();
                            }
                        }
                    };

                    // Handle Escape key to cancel
                    overlay.KeyDown += (s, e) =>
                    {
                        if (e.Key == System.Windows.Input.Key.Escape && !selectionMade)
                        {
                            selectionMade = true;
                            foreach (var o in overlays)
                            {
                                o.Close();
                            }
                        }
                    };

                    overlays.Add(overlay);
                    overlay.Show();
                    overlay.Focus();
                }

                // Wait for selection or timeout
                var timeout = DateTime.Now.AddSeconds(30);
                while (!selectionMade && DateTime.Now < timeout)
                {
                    System.Windows.Forms.Application.DoEvents();
                    System.Threading.Thread.Sleep(50);
                }

                return selectedIndex;
            }
            finally
            {
                // Ensure all overlays are closed
                foreach (var overlay in overlays)
                {
                    if (overlay.IsVisible)
                    {
                        overlay.Close();
                    }
                }
            }
        }

        private void ChooseMonitorForOverlayText(TextOverlayRowViewModel row)
        {
            try
            {
                int selectedMonitorIndex = ShowMonitorSelectionOverlay();
                if (selectedMonitorIndex >= 0)
                    row.DesktopIndex = selectedMonitorIndex;
            }
            catch (Exception ex)
            {
                AppDialog.Show(Loc.Format("Error.MonitorSelection", ex.Message), Loc.Get("Error.Title"),
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private static bool TryBrowseFileOrFolder(string currentPath, out string selectedPath)
        {
            const string folderPlaceholder = "Ordner auswählen";
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = Loc.Get("Ui.Step.FileSystem.BrowseTitle"),
                Filter = Loc.Get("Ui.Step.FileSystem.AllFilesFilter"),
                CheckFileExists = false,
                ValidateNames = false,
                Multiselect = false,
                FileName = folderPlaceholder
            };
            try
            {
                var initial = string.IsNullOrWhiteSpace(currentPath)
                    ? null
                    : Directory.Exists(currentPath) ? currentPath : Path.GetDirectoryName(currentPath);
                if (!string.IsNullOrWhiteSpace(initial) && Directory.Exists(initial))
                    dialog.InitialDirectory = initial;
            }
            catch (ArgumentException) { }

            if (dialog.ShowDialog() != true)
            {
                selectedPath = string.Empty;
                return false;
            }

            selectedPath = File.Exists(dialog.FileName)
                ? dialog.FileName
                : Path.GetDirectoryName(dialog.FileName) ?? dialog.FileName;
            return true;
        }

        private readonly ObservableRangeCollection<InstalledProgramSuggestion> _availableStartPrograms = new();
        private readonly ObservableRangeCollection<InstalledProgramSuggestion> _availableExecutablePrograms = new();
        private readonly ObservableRangeCollection<string> _availableProcessNames = new();
        public ObservableCollection<InstalledProgramSuggestion> AvailableStartPrograms => _availableStartPrograms;
        public ObservableCollection<InstalledProgramSuggestion> AvailableExecutablePrograms => _availableExecutablePrograms;
        public ObservableCollection<string> AvailableProcessNames => _availableProcessNames;

        // ===== UserChoice Felder =====
        // ===== PointComparison Felder =====

        // ── Comparison mode ──
        // ── Match requirement ──
        // ── Offset settings: reference point ──
        // ── Expression settings ──
        // ── Points list ──
        // ===== If / ElseIf Felder =====

        // ── MatchMode ──
        // ===== Fabrik =====
        public void CreateStep()
        {
            CreatedStep = CreateGeneratedStep();
        }

        private async void CaptureGeneratedRoi(GeneratedRoiEditorViewModel? editor)
        {
            if (editor is null) return;
            try
            {
                var roiOverlay = new DesktopOverlay.RoiCaptureOverlay();
                var rect = await roiOverlay.CaptureRoiAsync();
                editor.X = rect.X;
                editor.Y = rect.Y;
                editor.RoiWidth = rect.Width;
                editor.RoiHeight = rect.Height;
                editor.IsRoiEnabled = true;
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                AppDialog.Show(Loc.Format("Error.CaptureRoi", ex.Message), Loc.Get("Error.Title"),
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private JobStep? CreateGeneratedStep()
        {
            if (GeneratedEditor is null) return null;
            return GeneratedEditor.TryCreateStep(out var step) ? step : null;
        }
    }
}

