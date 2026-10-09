using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TaskAutomation.Steps;
using System.ComponentModel;
using System.Linq;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using DesktopAutomationApp.Services.Preview;
using DesktopAutomationApp.Services;
using DesktopAutomationApp.Views;
using Microsoft.Win32;
using TaskAutomation.Jobs;
using TaskAutomation.Makros;
using DesktopAutomationApp.Localization;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Steps.Definitions;
using TaskAutomation.Security;
using DesktopAutomationApp.Services.Jobs;
using MahApps.Metro.IconPacks;

namespace DesktopAutomationApp.ViewModels
{
    public sealed class AddJobStepDialogViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private int _notificationDeferral;
        private bool _notificationPending;
        private bool _candidateIsValid;
        private bool _isPickerOnly;
        private bool _refreshingCandidateValidation;
        private DispatcherTimer? _candidateValidationTimer;
        private bool _candidateValidationPending;
        private IReadOnlyDictionary<string, int>? _variableUsageCounts;

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
        private readonly ValueReferenceSourceCatalog _valueReferenceSources;
        private readonly IReadOnlyList<JobVariable> _jobVariables;
        private readonly IReadOnlyList<LocalValue> _persistedLocalValues;
        private readonly List<LocalValue> _localValues;
        private readonly Dictionary<Guid, string> _localValueBaselines;
        private readonly List<ValueProviderSourceDescriptor> _providerSources;
        private readonly Action<JobVariable>? _jobVariableCreated;
        private readonly Action<LocalValue>? _localValueCreated;
        private readonly List<JobVariable> _draftStepVariables = [];
        private readonly Guid? _currentJobId;
        private readonly ICameraCaptureService _cameraCaptureService;
        private readonly IStepDefinitionCatalog _stepDefinitionCatalog;
        private readonly ISecretStore? _secretStore;
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
            IStepDefinitionCatalog? stepDefinitionCatalog = null,
            IReadOnlyList<JobVariable>? jobVariables = null,
            IReadOnlyList<ValueProviderSourceDescriptor>? providerSources = null,
            Action<JobVariable>? jobVariableCreated = null,
            ISecretStore? secretStore = null,
            IReadOnlyList<LocalValue>? localValues = null,
            Action<LocalValue>? localValueCreated = null)
        {
            _ctx = ctx;
            _precedingSteps = precedingSteps;
            _allJobSteps = allJobSteps ?? precedingSteps;
            _currentJobId = currentJobId;
            _cameraCaptureService = cameraCaptureService
                ?? throw new ArgumentNullException(nameof(cameraCaptureService));
            _stepDefinitionCatalog = stepDefinitionCatalog ?? BuiltInStepDefinitions.Instance;
            _jobVariables = jobVariables ?? [];
            _persistedLocalValues = localValues ?? [];
            _localValues = _persistedLocalValues.Select(CloneLocalValue).ToList();
            _localValueBaselines = _localValues.GroupBy(value => value.Id).ToDictionary(group => group.Key, group => JsonSerializer.Serialize(group.First()));
            _providerSources = (providerSources ?? []).ToList();
            _jobVariableCreated = jobVariableCreated;
            _localValueCreated = localValueCreated;
            _secretStore = secretStore;
            StepTypeItems = CreateStepTypeItems(_stepDefinitionCatalog);
            StepCategories = new[] { Loc.Get("Ui.Job.Steps.Picker.All") }
                .Concat(StepTypeItems.Cast<StepTypeItem>().Select(item => item.Category).Distinct()).ToArray();
            _selectedStepCategory = StepCategories[0];
            AvailableJobs = new ObservableCollection<Job>(
                (_ctx.AllJobs?.Values ?? Enumerable.Empty<Job>())
                .Where(job => job.Id != _currentJobId)
                .OrderBy(job => job.Name));
            AvailableMakros = new ObservableCollection<Makro>(
                (_ctx.AllMakros?.Values ?? Enumerable.Empty<Makro>())
                .OrderBy(makro => makro.Name));
            _conditionSourceSteps = preparedSources?.Conditions ?? BuildConditionSourceCatalog(precedingSteps);
            _valueReferenceSources = new ValueReferenceSourceCatalog(
                _conditionSourceSteps, CurrentVariables(), _providerSources);
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

        public bool IsPickerOnly
        {
            get => _isPickerOnly;
            set
            {
                if (_isPickerOnly == value) return;
                _isPickerOnly = value;
                if (value) GeneratedEditor = null;
                else SetGeneratedEditor(_selectedType);
                OnChange();
                RaiseConfirmCanExecuteChanged();
            }
        }

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
            IsPickerOnly ? Loc.Get("Step.Add") : IsTypeLocked
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
            if (IsPickerOnly)
            {
                if (!CanConfirm()) return;
                CreatedStep = _stepDefinitionCatalog.TryGetByName(SelectedType, out var definition)
                    ? definition.CreateDefault()
                    : null;
                if (CreatedStep is not null)
                    RequestClose?.Invoke(true);
                return;
            }

            RefreshCandidateValidation();
            if (!_candidateIsValid) return;
            if (CreatedStep is not null)
                CommitDraftValues(CreatedStep);
            RequestClose?.Invoke(true);
        }

        internal void CommitDraftValues(JobStep step)
        {
            var localInputs = ValueBindingTree.EnumerateReferences(step.Inputs)
                .Where(input => string.Equals(
                    input.Binding.ProviderId, ValueProviderIds.LocalValue, StringComparison.Ordinal))
                .GroupBy(input => input.Binding.SourceId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First().Path, StringComparer.OrdinalIgnoreCase);
            foreach (var usage in ValueReferenceUsageInspector.Find(new Job { Steps = [step], LocalValues = CurrentVariables().OfType<LocalValue>().ToList(), Variables = CurrentVariables().Where(value => value is not LocalValue).ToList() })
                         .Where(usage => string.Equals(
                             usage.Reference.ProviderId, ValueProviderIds.LocalValue, StringComparison.Ordinal)))
                localInputs.TryAdd(usage.Reference.SourceId, usage.Path);
            foreach (var edited in _localValues.Where(value => localInputs.ContainsKey(value.Id.ToString("D"))))
            {
                var persisted = _persistedLocalValues.FirstOrDefault(value => value.Id == edited.Id);
                var serialized = JsonSerializer.Serialize(edited);
                if (persisted is null || _localValueBaselines.GetValueOrDefault(edited.Id) == serialized) continue;
                persisted.Value = edited.Value?.DeepClone();
                persisted.EnumTypeName = edited.EnumTypeName;
                persisted.EnumValues = edited.EnumValues?.ToList();
                persisted.EnumDisplayNames = edited.EnumDisplayNames is null ? null
                    : new Dictionary<string, string>(edited.EnumDisplayNames, StringComparer.Ordinal);
                _localValueBaselines[edited.Id] = serialized;
            }
            foreach (var local in _draftStepVariables.OfType<LocalValue>()
                         .Where(value => localInputs.ContainsKey(value.Id.ToString("D"))).ToArray())
            {
                local.OwnerStepId = step.Id;
                local.InputPath = localInputs[local.Id.ToString("D")];
                CommitCreatedLocalValue(CloneLocalValue(local));
                _localValues.Add(local);
                _localValueBaselines[local.Id] = JsonSerializer.Serialize(local);
            }
            _draftStepVariables.Clear();
        }

        private bool CanConfirm()
        {
            return IsPickerOnly ? StepTypeItems.Cast<StepTypeItem>().Any(item => item.Name == SelectedType)
                : _candidateValidationPending || _candidateIsValid;
        }

        private void RefreshCandidateValidation()
        {
            if (_refreshingCandidateValidation) return;
            _candidateValidationTimer?.Stop();
            _candidateValidationPending = false;
            _refreshingCandidateValidation = true;
            try
            {
                CreateStep();
                if (GeneratedEditor?.ValidationError is { Length: > 0 } generatedError)
                {
                    _validationError = generatedError;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ValidationError)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasValidationError)));
                    _candidateIsValid = false;
                    RaiseConfirmCanExecuteChanged();
                    return;
                }
                var result = JobValidation.ValidateCandidate(
                    _precedingSteps, CreatedStep, _allJobSteps, CurrentVariables(), _providerSources);
                _validationError = JobValidationErrorLocalizer.Localize(result.Error, CreatedStep);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ValidationError)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasValidationError)));
                _candidateIsValid = result.IsValid;
                RaiseConfirmCanExecuteChanged();
            }
            finally
            {
                _refreshingCandidateValidation = false;
            }
        }

        private void ScheduleCandidateValidation()
        {
            _candidateValidationTimer ??= CreateCandidateValidationTimer();
            _candidateValidationTimer.Stop();
            _candidateValidationPending = true;
            _candidateValidationTimer.Start();
            RaiseConfirmCanExecuteChanged();
        }

        private DispatcherTimer CreateCandidateValidationTimer()
        {
            var timer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(120)
            };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                RefreshCandidateValidation();
            };
            return timer;
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
            public PackIconMaterialKind Icon { get; }
            public string CategoryKey => _category;
            public string Category => LocalizedOrFallback($"Step.Category.{_category}", _category);
            public string Description => LocalizedOrFallback(_descriptionKey, _description);
            public string DisplayLabel => LocalizedOrFallback(_displayNameKey, Name);

            public StepTypeItem(
                string name,
                string category,
                string description = "",
                string? displayNameKey = null,
                string? descriptionKey = null,
                Type? stepType = null)
            {
                Name = name;
                Icon = StepIconPresentation.ForType(stepType ?? typeof(JobStep));
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
                .Where(definition => definition.Descriptor.IsSelectable
                                     && definition.StepType != typeof(ElseIfStep)
                                     && definition.StepType != typeof(ElseStep)
                                     && definition.StepType != typeof(EndIfStep))
                .Select(definition => new StepTypeItem(
                    TrimStepSuffix(definition.StepType.Name),
                    definition.Descriptor.CategoryId,
                    displayNameKey: definition.Descriptor.DisplayNameKey,
                    descriptionKey: definition.Descriptor.DescriptionKey,
                    stepType: definition.StepType))
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

            return view;
        }

        public string InsertionDescription { get; set; } = string.Empty;
        public IReadOnlyList<string> StepCategories { get; }
        private string _selectedStepCategory = string.Empty;
        public string SelectedStepCategoryFilter
        {
            get => _selectedStepCategory;
            set
            {
                if (value is null || _selectedStepCategory == value) return;
                _selectedStepCategory = value;
                RefreshStepFilter();
                OnChange();
            }
        }
        public bool HasMatchingStepTypes => !StepTypeItems.IsEmpty;
        public StepTypeItem? SelectedStepTypeItem
        {
            get => StepTypeItems.Cast<StepTypeItem>().FirstOrDefault(item => item.Name == SelectedType);
            set { if (value is not null) SelectedType = value.Name; }
        }
        private string _stepTypeSearchText = string.Empty;
        public string StepTypeSearchText
        {
            get => _stepTypeSearchText;
            set
            {
                if (_stepTypeSearchText == value) return;
                _stepTypeSearchText = value;
                RefreshStepFilter();
                OnChange();
            }
        }
        private void RefreshStepFilter()
        {
            StepTypeItems.Filter = FilterStepType;
            StepTypeItems.Refresh();
            if (IsPickerOnly && !StepTypeItems.Cast<StepTypeItem>().Any(item => item.Name == SelectedType)
                && StepTypeItems.Cast<StepTypeItem>().FirstOrDefault() is { } first)
                SelectedType = first.Name;
            OnChange(nameof(SelectedType));
            OnChange(nameof(SelectedStepTypeItem));
            OnChange(nameof(HasMatchingStepTypes));
        }
        private bool FilterStepType(object item)
        {
            if (item is not StepTypeItem stepType) return false;
            if (_selectedStepCategory != StepCategories[0] && stepType.Category != _selectedStepCategory) return false;
            if (string.IsNullOrWhiteSpace(StepTypeSearchText)) return true;
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
                if (!IsPickerOnly) SetGeneratedEditor(value);
                OnChange(string.Empty);
            }
        }

        public bool TryLoadGeneratedStep(JobStep step)
        {
            _draftStepVariables.Clear();
            _valueReferenceSources.Reset(CurrentVariables(), _providerSources);
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
                GeneratedEditor = null;
                GeneratedEditor = CreateGeneratedEditor(terminateDefinition, migrated);
                OnChange(nameof(SelectedType));
                OnChange(string.Empty);
                return true;
            }
            if (!_stepDefinitionCatalog.TryGetByType(step.GetType(), out var definition))
                return false;
            _selectedType = TrimStepSuffix(step.GetType().Name);
            GeneratedEditor = null;
            GeneratedEditor = CreateGeneratedEditor(definition, step);
            RefreshCandidateValidation();
            OnChange(nameof(SelectedType));
            OnChange(string.Empty);
            return true;
        }

        private void SetGeneratedEditor(string selectedType)
        {
            _draftStepVariables.Clear();
            GeneratedEditor = null;
            _valueReferenceSources.Reset(CurrentVariables(), _providerSources);
            GeneratedEditor = _stepDefinitionCatalog.TryGetByName(selectedType, out var definition)
                ? CreateGeneratedEditor(definition)
                : null;
            RefreshCandidateValidation();
        }

        private GeneratedStepEditorViewModel CreateGeneratedEditor(
            IStepDefinition definition,
            JobStep? step = null) =>
            new(
                definition,
                step,
                ResolveGeneratedSuggestions,
                ResolveGeneratedChoices,
                (field, value) => ResolveGeneratedProcessTarget(definition, field, value, step?.Inputs),
                (field, value) => ResolveGeneratedResultBinding(definition, field, value),
                ResolveGeneratedCamera,
                (field, value) => ResolveGeneratedVisualOverlay(definition, field, value, step?.Inputs),
                (field, value) => ResolveGeneratedRoi(definition, field, value, step?.Inputs),
                (field, value) => ResolveGeneratedYolo(definition, field, value, step?.Inputs),
                (field, value) => ResolveGeneratedCondition(definition, field, value, step?.Inputs),
                ResolveGeneratedWindowsCapability,
                (field, value) => ResolveGeneratedScreenPoint(definition, field, value, step?.Inputs),
                (field, value) => ResolveGeneratedUserChoiceOptions(definition, field, value, step?.Inputs),
                (field, value) => ResolveGeneratedPointEntryList(definition, field, value, step?.Inputs),
                ResolveGeneratedAxisExpressionList,
                (field, binding) => ResolveGeneratedInputReference(definition, field, binding, step),
                (field, fallback) => ResolveStoredFieldValue(field.Id, fallback, step?.Inputs));

        private GeneratedResultBindingEditorViewModel? ResolveGeneratedInputReference(
            IStepDefinition definition,
            StepFieldDescriptor field,
            ResultBinding? binding,
            JobStep? step = null)
        {
            var contract = StepInputContractRegistry.Resolve(definition.StepType, field);
            if (binding?.IsConfigured == true
                && FindStoredVariable(binding) is { } storedVariable)
            {
                ApplyEnumMetadata(storedVariable, $"{definition.Descriptor.TypeId}.{field.Id}", field);
                _valueReferenceSources.AddVariable(storedVariable);
            }
            if (binding?.IsConfigured != true
                && (field.ValueKind != StepValueKind.ResultBinding || contract.AllowsDirectValue))
            {
                var variable = CreateDraftStepVariable(definition, field, contract, JobVariableInputMigration.GetLegacyDirectValue(step, field));
                binding = new ResultBinding
                {
                    ProviderId = ValueProviderIds.LocalValue,
                    SourceId = variable.Id.ToString("D")
                };
            }
            var picker = CreateValueReferencePicker(definition, field, contract, false);
            return new GeneratedResultBindingEditorViewModel(
                System.Text.Json.JsonSerializer.SerializeToNode(binding ?? new ResultBinding()),
                picker);
        }

        private JobVariable CreateDraftStepVariable(
            IStepDefinition definition,
            StepFieldDescriptor field,
            StepInputDescriptor? contract = null,
            JsonNode? initialValue = null)
        {
            var stem = $"{Loc.Get(definition.Descriptor.DisplayNameKey)} · {Loc.Get(field.LabelKey)}";
            var names = CurrentVariables().Select(variable => variable.Name)
                .ToHashSet(StringComparer.CurrentCultureIgnoreCase);
            var name = stem;
            for (var suffix = 2; names.Contains(name); suffix++) name = $"{stem} {suffix}";
            contract ??= StepInputContractRegistry.Resolve(definition.StepType, field);
            var shape = field.ValueKind == StepValueKind.ResultBinding
                ? contract.AcceptedShapes.FirstOrDefault()
                : null;
            var variable = new LocalValue
            {
                Name = name,
                Description = Loc.Format("Ui.Job.Variables.StepValue.Description",
                    Loc.Get(field.LabelKey), Loc.Get(definition.Descriptor.DisplayNameKey)),
                Scope = JobVariableScope.StepValue,
                ValueKind = shape?.ValueKind ?? JobVariableInputMigration.MapKind(field),
                Cardinality = shape?.Cardinalities.FirstOrDefault(ResultCardinality.Single)
                              ?? (field.ValueKind == StepValueKind.Collection
                                  ? ResultCardinality.Collection
                                  : ResultCardinality.Single),
                // Binding fields use their literal default; loaded local values are reused above.
                // Other fields initialize from the actual draft in the generated editor.
                Value = initialValue?.DeepClone() ?? (field.ValueKind == StepValueKind.ResultBinding ? field.DefaultValue?.DeepClone() : null)
            };
            ApplyEnumMetadata(variable, $"{definition.Descriptor.TypeId}.{field.Id}", field);
            _draftStepVariables.Add(variable);
            _valueReferenceSources.AddVariable(variable);
            _variableUsageCounts = null;
            return variable;
        }

        private static LocalValue CloneLocalValue(LocalValue value) =>
            JsonSerializer.Deserialize<LocalValue>(JsonSerializer.Serialize(value))!;

        private IReadOnlyList<JobVariable> CurrentVariables() =>
            _jobVariables.Cast<JobVariable>().Concat(_localValues).Concat(_draftStepVariables)
                .DistinctBy(variable => (JobValueSources.ProviderFor(variable), variable.Id)).ToArray();

        private JobVariable? FindStoredVariable(ResultBinding binding) =>
            JobValueSources.Find(CurrentVariables(), binding);

        private void CommitCreatedLocalValue(LocalValue variable)
        {
            if (_localValueCreated is not null) _localValueCreated(variable);
            else if (_jobVariableCreated is not null) _jobVariableCreated(variable);
            else if (_persistedLocalValues is ICollection<LocalValue> { IsReadOnly: false } values) values.Add(variable);
        }

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
            System.Text.Json.Nodes.JsonNode? value,
            IReadOnlyDictionary<string, ResultBinding>? inputs)
        {
            if (!string.Equals(field.EditorHint, StepEditorHints.ProcessTargetPicker, StringComparison.Ordinal)
                && !string.Equals(field.EditorHint, StepEditorHints.ExecutableProcessTargetPicker, StringComparison.Ordinal))
                return null;
            var contract = StepInputContractRegistry.Get(definition.StepType, "process")
                ?? throw new InvalidOperationException(
                    $"Eingabevertrag 'process' für {definition.StepType.Name} fehlt.");
            return new GeneratedProcessTargetEditorViewModel(
                value,
                CreateValueReferencePicker(definition, field, contract, false),
                _availableProcessNames,
                string.Equals(field.EditorHint, StepEditorHints.ExecutableProcessTargetPicker, StringComparison.Ordinal),
                field.Id,
                (key, kind, literal) => ResolveNestedInputReference(definition, field, key, kind, literal, inputs));
        }

        private GeneratedResultBindingEditorViewModel ResolveNestedInputReference(
            IStepDefinition definition,
            StepFieldDescriptor owner,
            string key,
            StepValueKind kind,
            JsonNode? literal,
            IReadOnlyDictionary<string, ResultBinding>? inputs,
            ResultPropertyDescriptor? enumProperty = null,
            ResultBinding? explicitBinding = null)
        {
            var enumOptions = kind == StepValueKind.Enum && enumProperty is not null
                ? (enumProperty.EnumValues ?? []).Select(value => new StepFieldOptionDescriptor(
                    value,
                    $"Enum.{EnumValueLocalization.ShortTypeName(enumProperty.EnumTypeName)}.{value}",
                    EnumValueLocalization.ForResultValue(enumProperty, value))).ToArray()
                : null;
            var descriptor = new StepFieldDescriptor(
                key,
                owner.LabelKey,
                kind,
                DefaultValue: literal?.DeepClone(),
                Constraints: enumOptions is { Length: > 0 }
                    ? new StepFieldConstraints(AllowedValues: enumOptions.Select(option => option.Value).ToArray())
                    : null,
                Options: enumOptions);
            var contract = StepInputContractRegistry.ForField(descriptor);
            if (enumProperty is not null)
                contract = ConditionRules.ComparisonInputContract(contract, enumProperty);
            if (string.Equals(owner.EditorHint, StepEditorHints.YoloPicker, StringComparison.Ordinal))
                contract = contract with { AllowedProviderIds = new HashSet<string>() };
            JobVariable CreateStepValue() => CreateDraftNestedVariable(
                definition, owner, key, kind, literal, enumProperty);
            var stepName = Loc.Get(definition.Descriptor.DisplayNameKey);
            var fieldName = NestedFieldName(key);
            var context = new ValueReferencePickerContext(
                stepName,
                fieldName,
                accepted => CreateJobVariable(accepted, stepName, fieldName),
                GetVariableUsageCount,
                variable => DetachStepValue(variable, stepName, fieldName),
                CreateStepValue,
                CreateSecret);
            var binding = explicitBinding ?? ValueBindingTree.Find(inputs, key);
            if (kind == StepValueKind.Enum
                && enumProperty is not null
                && binding?.IsConfigured == true
                && FindStoredVariable(binding) is { } storedVariable)
            {
                ApplyEnumMetadata(storedVariable, enumProperty);
                _valueReferenceSources.AddVariable(storedVariable);
            }
            if (binding?.IsConfigured != true)
            {
                var variable = CreateStepValue();
                binding = new ResultBinding
                {
                    ProviderId = ValueProviderIds.LocalValue,
                    SourceId = variable.Id.ToString("D")
                };
            }
            var picker = new ValueReferencePickerViewModel(
                _valueReferenceSources, contract, false, context);
            return new GeneratedResultBindingEditorViewModel(JsonSerializer.SerializeToNode(binding), picker);
        }

        private JobVariable CreateDraftNestedVariable(
            IStepDefinition definition,
            StepFieldDescriptor owner,
            string key,
            StepValueKind kind,
            JsonNode? literal,
            ResultPropertyDescriptor? enumProperty = null)
        {
            var nestedName = NestedFieldName(key);
            var variable = new LocalValue
            {
                Name = $"{Loc.Get(definition.Descriptor.DisplayNameKey)} · {nestedName}",
                Description = Loc.Format("Ui.Job.Variables.StepValue.Description", nestedName, Loc.Get(owner.LabelKey)),
                Scope = JobVariableScope.StepValue,
                ValueKind = JobVariableInputMigration.MapKind(kind),
                Cardinality = ResultCardinality.Single,
                Value = literal?.DeepClone()
            };
            if (kind == StepValueKind.Enum && enumProperty is not null)
            {
                variable.EnumTypeName = enumProperty.EnumTypeName;
                variable.EnumValues = enumProperty.EnumValues?.ToList();
                variable.EnumDisplayNames = enumProperty.EnumDisplayNames is null
                    ? null
                    : new Dictionary<string, string>(enumProperty.EnumDisplayNames, StringComparer.Ordinal);
            }
            _draftStepVariables.Add(variable);
            _valueReferenceSources.AddVariable(variable);
            _variableUsageCounts = null;
            return variable;
        }

        private static string NestedFieldName(string key) =>
            key[(key.LastIndexOf('.') + 1)..] switch
            {
                "font_size" => Loc.Get("Ui.Step.Settings.FontSizePt"),
                "font_color" => Loc.Get("Ui.Step.Settings.FontColor"),
                "opacity" => Loc.Get("Ui.Step.Settings.OpacityPercent"),
                "desktop_index" or "monitor_index" => Loc.Get("Ui.Step.Settings.DesktopIndex"),
                "offset_x" or "x" => Loc.Get("Ui.Common.XCoordinate"),
                "offset_y" or "y" => Loc.Get("Ui.Common.YCoordinate"),
                "duration_ms" => Loc.Get("Ui.Step.Settings.DisplayDurationMs"),
                "clear_on_job_end" => Loc.Get("Ui.Step.Settings.RemoveWhenJobEnds"),
                "comparison" => Loc.Get("Ui.Step.Settings.Value"),
                var name => name.Replace('_', ' ')
            };

        private GeneratedResultBindingEditorViewModel? ResolveGeneratedResultBinding(
            IStepDefinition definition,
            StepFieldDescriptor field,
            System.Text.Json.Nodes.JsonNode? value)
        {
            if (!string.Equals(field.EditorHint, StepEditorHints.ValueReferencePicker, StringComparison.Ordinal)
                && !string.Equals(field.EditorHint, StepEditorHints.ResultBindingPicker, StringComparison.Ordinal))
                return null;
            if (string.IsNullOrWhiteSpace(field.InputContractId))
                throw new InvalidOperationException($"Eingabevertrag für {definition.StepType.Name} fehlt im Descriptor.");
            var contract = StepInputContractRegistry.Get(definition.StepType, field.InputContractId)
                ?? throw new InvalidOperationException(
                    $"Eingabevertrag '{field.InputContractId}' für {definition.StepType.Name} fehlt.");
            return new GeneratedResultBindingEditorViewModel(
                value,
                CreateValueReferencePicker(definition, field, contract, field.Required));
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
            System.Text.Json.Nodes.JsonNode? value,
            IReadOnlyDictionary<string, ResultBinding>? inputs)
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
                ChooseMonitorForOverlayText,
                _jobVariables,
                _providerSources,
                CreateValueReferenceContext(definition, field),
                CreateValueReferenceContext(definition, field),
                field.Id,
                (key, kind, literal) => ResolveNestedInputReference(
                    definition, field, key, kind, literal, inputs),
                _valueReferenceSources);
        }

        private JsonNode? ResolveStoredFieldValue(
            string fieldId,
            JsonNode? fallback,
            IReadOnlyDictionary<string, ResultBinding>? inputs)
        {
            if (inputs?.GetValueOrDefault(fieldId) is not { } binding
                || binding.ProviderId != ValueProviderIds.LocalValue
                || !string.IsNullOrWhiteSpace(binding.ValuePath)
                || !Guid.TryParse(binding.SourceId, out var variableId))
                return fallback;

            return JobValueSources.Find(CurrentVariables(), binding)?.Value?.DeepClone()
                   ?? fallback;
        }

        private GeneratedRoiEditorViewModel? ResolveGeneratedRoi(
            IStepDefinition definition,
            StepFieldDescriptor field,
            System.Text.Json.Nodes.JsonNode? value,
            IReadOnlyDictionary<string, ResultBinding>? inputs)
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
                CreateValueReferencePicker(definition, field, contract, false),
                field.Id,
                (key, kind, literal) => ResolveNestedInputReference(definition, field, key, kind, literal, inputs));
        }

        private ValueReferencePickerViewModel CreateValueReferencePicker(
            IStepDefinition definition,
            StepFieldDescriptor field,
            StepInputDescriptor contract,
            bool selectDefault)
        {
            var sources = contract.Key == "dynamicRoi"
                ? _valueReferenceSources.WithAdditionalSources(
                    BuildConditionSourceCatalog(JobValidation.GetRoiFeedbackSources(_allJobSteps)))
                : _valueReferenceSources;
            return new ValueReferencePickerViewModel(
                sources,
                contract,
                selectDefault,
                CreateValueReferenceContext(definition, field));
        }

        private ValueReferencePickerContext CreateValueReferenceContext(
            IStepDefinition definition,
            StepFieldDescriptor field)
        {
            var stepName = Loc.Get(definition.Descriptor.DisplayNameKey);
            var fieldName = Loc.Get(field.LabelKey);
            return new ValueReferencePickerContext(
                stepName,
                fieldName,
                contract => CreateJobVariable(contract, stepName, fieldName),
                GetVariableUsageCount,
                variable => DetachStepValue(variable, stepName, fieldName),
                () => CreateDraftStepVariable(definition, field),
                CreateSecret);
        }

        private int GetVariableUsageCount(JobVariable variable)
        {
            if (_variableUsageCounts is null)
            {
                var steps = _allJobSteps.ToList();
                if (GeneratedEditor?.CreateUsageSnapshot() is { } draftStep)
                {
                    var existingIndex = steps.FindIndex(step => step.Id == draftStep.Id);
                    if (existingIndex >= 0) steps[existingIndex] = draftStep;
                    else steps.Add(draftStep);
                }
                _variableUsageCounts = ValueReferenceUsageInspector.CountLogicalByIdentity(
                    new Job { Steps = steps, LocalValues = CurrentVariables().OfType<LocalValue>().ToList(), Variables = CurrentVariables().Where(value => value is not LocalValue).ToList() });
            }
            var sourceId = JobValueSources.Key(JobValueSources.ProviderFor(variable), variable.Id.ToString("D"));
            return _variableUsageCounts.GetValueOrDefault(sourceId);
        }

        private JobVariable DetachStepValue(JobVariable source, string stepName, string fieldName)
        {
            var stem = $"{stepName} · {fieldName}";
            var names = CurrentVariables().Select(variable => variable.Name)
                .ToHashSet(StringComparer.CurrentCultureIgnoreCase);
            var name = stem;
            for (var suffix = 2; names.Contains(name); suffix++) name = $"{stem} {suffix}";
            var detached = new LocalValue
            {
                Name = name,
                Description = Loc.Format("Ui.Job.Variables.StepValue.Description", fieldName, stepName),
                Scope = JobVariableScope.StepValue,
                ValueKind = source.ValueKind,
                Cardinality = source.Cardinality,
                Value = source.Value?.DeepClone(),
                EnumTypeName = source.EnumTypeName,
                EnumValues = source.EnumValues?.ToList(),
                EnumDisplayNames = source.EnumDisplayNames is null
                    ? null
                    : new Dictionary<string, string>(source.EnumDisplayNames, StringComparer.Ordinal)
            };
            _draftStepVariables.Add(detached);
            _valueReferenceSources.AddVariable(detached);
            _variableUsageCounts = null;
            return detached;
        }

        private static void ApplyEnumMetadata(JobVariable variable, string enumTypeName, StepFieldDescriptor field) =>
            StepEnumRules.ApplyMetadata(variable, enumTypeName, field);
        private static void ApplyEnumMetadata(JobVariable variable, ResultPropertyDescriptor property)
        {
            if (variable.ValueKind != ResultValueKind.Enum) return;
            variable.EnumTypeName = property.EnumTypeName;
            variable.EnumValues = property.EnumValues?.ToList();
            variable.EnumDisplayNames = property.EnumDisplayNames is null
                ? null
                : new Dictionary<string, string>(property.EnumDisplayNames, StringComparer.Ordinal);
        }

        private JobVariable? CreateJobVariable(
            StepInputDescriptor contract,
            string stepName,
            string fieldName)
        {
            if (!contract.AcceptedShapes.Any(shape =>
                    JobVariableEditorViewModel.SupportedKinds.Contains(shape.ValueKind)))
                return null;
            var viewModel = new QuickCreateJobVariableViewModel(contract, stepName, fieldName, CurrentVariables());
            var dialog = new QuickCreateJobVariableDialog
            {
                Owner = System.Windows.Application.Current.MainWindow,
                DataContext = viewModel
            };
            if (dialog.ShowDialog() != true) return null;
            if (_jobVariableCreated is not null) _jobVariableCreated(viewModel.Variable);
            else if (_jobVariables is ICollection<JobVariable> variables) variables.Add(viewModel.Variable);
            return viewModel.Variable;
        }

        private ValueProviderSourceDescriptor? CreateSecret()
        {
            if (_secretStore is null) return null;
            var viewModel = new QuickCreateSecretViewModel(_secretStore);
            var dialog = new QuickCreateSecretDialog
            {
                Owner = System.Windows.Application.Current.MainWindow,
                DataContext = viewModel
            };
            if (dialog.ShowDialog() != true || viewModel.CreatedSecret is not { } secret) return null;
            var source = new ValueProviderSourceDescriptor(
                ValueProviderIds.Secret,
                secret.Id.ToString("D"),
                secret.Name,
                secret.Description,
                ResultValueKind.Text,
                ResultCardinality.Single,
                IsSensitive: true);
            _providerSources.RemoveAll(candidate =>
                string.Equals(candidate.ProviderId, source.ProviderId, StringComparison.Ordinal)
                && string.Equals(candidate.SourceId, source.SourceId, StringComparison.OrdinalIgnoreCase));
            _providerSources.Add(source);
            return source;
        }

        private GeneratedYoloEditorViewModel? ResolveGeneratedYolo(
            IStepDefinition definition,
            StepFieldDescriptor field,
            System.Text.Json.Nodes.JsonNode? value,
            IReadOnlyDictionary<string, ResultBinding>? inputs) =>
            string.Equals(field.EditorHint, StepEditorHints.YoloPicker, StringComparison.Ordinal)
                ? new GeneratedYoloEditorViewModel(
                    value,
                    () => _ctx.YoloManager?.GetAvailableModels() ?? [],
                    model => _ctx.YoloManager?.GetClassesForModel(model) ?? [],
                    model => _ctx.YoloManager?.GetRecommendedConfidenceThreshold(model),
                    field.Id,
                    (key, kind, literal) => ResolveNestedInputReference(definition, field, key, kind, literal, inputs))
                : null;

        private GeneratedConditionEditorViewModel? ResolveGeneratedCondition(
            IStepDefinition definition,
            StepFieldDescriptor field,
            System.Text.Json.Nodes.JsonNode? value,
            IReadOnlyDictionary<string, ResultBinding>? inputs) =>
            string.Equals(field.EditorHint, StepEditorHints.ConditionEditor, StringComparison.Ordinal)
                ? new GeneratedConditionEditorViewModel(
                    value,
                    _conditionSourceSteps,
                    CurrentVariables(),
                    _providerSources.Where(source => !source.IsSensitive).ToArray(),
                    field.Id,
                    (key, kind, literal, enumProperty, binding) => ResolveNestedInputReference(
                        definition, field, key, kind, literal, null, enumProperty, binding),
                    _valueReferenceSources)
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
            IStepDefinition definition,
            StepFieldDescriptor field,
            System.Text.Json.Nodes.JsonNode? value,
            IReadOnlyDictionary<string, ResultBinding>? inputs)
        {
            if (field.EditorHint != StepEditorHints.ScreenPointPicker) return null;
            var contractId = field.ScreenPointPickerOptions?.WholeValueInputContractId ?? "origin";
            var contract = StepInputContractRegistry.Get(definition.StepType, contractId)
                ?? throw new InvalidOperationException(
                    $"Eingabevertrag '{contractId}' für {definition.StepType.Name} fehlt.");
            return new GeneratedScreenPointEditorViewModel(
                value,
                CreateValueReferencePicker(definition, field, contract, false),
                NormalizeScreenPoint,
                SelectMonitorForGeneratedEditor,
                CaptureGeneratedScreenPointAsync,
                field.Id,
                (key, kind, literal) => ResolveNestedInputReference(definition, field, key, kind, literal, inputs));
        }

        private GeneratedUserChoiceOptionsEditorViewModel? ResolveGeneratedUserChoiceOptions(
            IStepDefinition definition,
            StepFieldDescriptor field,
            System.Text.Json.Nodes.JsonNode? value,
            IReadOnlyDictionary<string, ResultBinding>? inputs) =>
            field.EditorHint == StepEditorHints.UserChoiceOptions
                ? new GeneratedUserChoiceOptionsEditorViewModel(
                    value,
                    field.Id,
                    (key, kind, literal) => ResolveNestedInputReference(definition, field, key, kind, literal, inputs))
                : null;

        private GeneratedPointEntryListEditorViewModel? ResolveGeneratedPointEntryList(
            IStepDefinition definition,
            StepFieldDescriptor field,
            System.Text.Json.Nodes.JsonNode? value,
            IReadOnlyDictionary<string, ResultBinding>? inputs) =>
            field.EditorHint == StepEditorHints.PointEntryList
                ? new GeneratedPointEntryListEditorViewModel(
                    value, _conditionSourceSteps, _jobVariables, _providerSources,
                    new ValueReferencePickerContext(
                        Loc.Get("Step.Type.PointComparison"),
                        Loc.Get(field.LabelKey),
                        contract => CreateJobVariable(
                            contract,
                            Loc.Get("Step.Type.PointComparison"),
                            Loc.Get(field.LabelKey))),
                    field.Id,
                    (key, kind, literal) => ResolveNestedInputReference(definition, field, key, kind, literal, inputs),
                    _valueReferenceSources)
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

        private void OnGeneratedEditorChanged()
        {
            _variableUsageCounts = null;
            ScheduleCandidateValidation();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GeneratedEditor)));
        }

        private static string TrimStepSuffix(string name) =>
            name.EndsWith("Step", StringComparison.Ordinal) ? name[..^4] : name;

        // Beschreibung kommt direkt aus dem StepTypeItem – kein separates switch mehr nötig.
        public string StepTypeDescription => _stepDefinitionCatalog.TryGetByName(SelectedType, out var definition)
            ? Loc.Get(definition.Descriptor.DescriptionKey) : string.Empty;
        public string SelectedStepDisplayName => _stepDefinitionCatalog.TryGetByName(SelectedType, out var definition)
            ? Loc.Get(definition.Descriptor.DisplayNameKey) : SelectedType;
        public string SelectedStepCategory => _stepDefinitionCatalog.TryGetByName(SelectedType, out var definition)
            ? Loc.Get($"Step.Category.{definition.Descriptor.CategoryId}") : string.Empty;
        public PackIconMaterialKind SelectedStepIcon => _stepDefinitionCatalog.TryGetByName(SelectedType, out var definition)
            ? StepIconPresentation.ForType(definition.StepType) : PackIconMaterialKind.ShapeOutline;
        public string SelectedStepCategoryKey => _stepDefinitionCatalog.TryGetByName(SelectedType, out var definition)
            ? definition.Descriptor.CategoryId : "Unknown";

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
                var selectedMonitor = ShowMonitorSelection();
                if (selectedMonitor.Index >= 0)
                {
                    if (GeneratedEditor != null)
                        GeneratedEditor.ApplyMonitorSelection(field, selectedMonitor.Index, selectedMonitor.DeviceName);
                    else field.IntegerValue = selectedMonitor.Index;
                }
            }
            catch (Exception ex)
            {
                AppDialog.Show(Loc.Format("Error.MonitorSelection", ex.Message), Loc.Get("Error.Title"),
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }


        private int ShowMonitorSelectionOverlay() => ShowMonitorSelection().Index;

        private (int Index, string DeviceName) ShowMonitorSelection()
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

                return (selectedIndex, selectedIndex >= 0 ? screens[selectedIndex].DeviceName : string.Empty);
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

