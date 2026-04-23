using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TaskAutomation.Steps;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using DesktopAutomationApp.Services.Preview;
using DesktopAutomationApp.Views;
using Microsoft.Win32;
using OpenCvSharp;
using TaskAutomation.Jobs;
using TaskAutomation.Makros;

namespace DesktopAutomationApp.ViewModels
{
    public sealed class AddJobStepDialogViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnChange([CallerMemberName] string? p = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));

        private readonly IJobExecutor _ctx;
        private readonly IReadOnlyList<JobStep> _precedingSteps;
        private readonly Guid? _currentJobId;

        public AddJobStepDialogViewModel(
            IJobExecutor ctx,
            IReadOnlyList<JobStep> precedingSteps,
            Guid? currentJobId = null)
        {
            _ctx = ctx;
            _precedingSteps = precedingSteps;
            _currentJobId = currentJobId;
            ConfirmCommand = new RelayCommand(Confirm, CanConfirm);
            CancelCommand = new RelayCommand(() => RequestClose?.Invoke(false));
            BrowseTemplatePathCommand = new RelayCommand(BrowseTemplatePath);
            BrowseScriptPathCommand = new RelayCommand(BrowseScriptPath);
            BrowseVideoSavePathCommand = new RelayCommand(BrowseVideoSavePath);
            ChooseMonitorCommand = new RelayCommand(ChooseMonitor);
            CaptureTemplateMatchingRoiCommand = new RelayCommand(CaptureTemplateMatchingRoi);
            CaptureYoloDetectionRoiCommand = new RelayCommand(CaptureYoloDetectionRoi);
            IfStep_AddConditionCommand    = new RelayCommand(() =>
                IfStep_Conditions.Add(new ConditionRowViewModel(IfStep_Conditions, GetConditionSourceSteps())));
            ElseIfStep_AddConditionCommand = new RelayCommand(() =>
                ElseIfStep_Conditions.Add(new ConditionRowViewModel(ElseIfStep_Conditions, GetConditionSourceSteps())));

            InitDefaults();
        }

        private void InitDefaults()
        {
            // Video Creation: Standard-Videoordner + DesktopAutomation
            _videoCreationStep_SavePath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
                "DesktopAutomation");

            // Makro: erstes verfügbares Makro vorauswählen
            _makroExecutionStep_SelectedMakro = _ctx.AllMakros?.Values
                ?.OrderBy(m => m.Name).FirstOrDefault();
            if (_makroExecutionStep_SelectedMakro != null)
            {
                _makroExecutionStep_SelectedMakroId   = _makroExecutionStep_SelectedMakro.Id;
                _makroExecutionStep_SelectedMakroName = _makroExecutionStep_SelectedMakro.Name;
            }

            // Job: ersten verfügbaren Job vorauswählen
            _jobExecutionStep_SelectedJob = _ctx.AllJobs?.Values
                ?.Where(j => j?.Id != _currentJobId)
                .OrderBy(j => j.Name).FirstOrDefault();
            if (_jobExecutionStep_SelectedJob != null)
            {
                _jobExecutionStep_SelectedJobId   = _jobExecutionStep_SelectedJob.Id;
                _jobExecutionStep_SelectedJobName = _jobExecutionStep_SelectedJob.Name;
            }

            // YOLO: erstes verfügbares Modell + erste Klasse vorauswählen
            try
            {
                var firstModel = _ctx.YoloManager?.GetAvailableModels()?.FirstOrDefault();
                if (firstModel != null)
                {
                    _yoloDetectionStep_Model = firstModel;
                    var firstClass = _ctx.YoloManager?.GetClassesForModel(firstModel)?.FirstOrDefault();
                    if (firstClass != null)
                        _yoloDetectionStep_ClassName = firstClass;
                }
            }
            catch { }

            // If / ElseIf: start with one empty condition row
            IfStep_Conditions.Add(new ConditionRowViewModel(IfStep_Conditions, GetConditionSourceSteps()));
            ElseIfStep_Conditions.Add(new ConditionRowViewModel(ElseIfStep_Conditions, GetConditionSourceSteps()));

            // Source step pre-selection (first available of the right type)
            TemplateMatchingStep_SourceCaptureStep  = AvailableCaptureSteps.FirstOrDefault();
            YoloDetectionStep_SourceCaptureStep     = AvailableCaptureSteps.FirstOrDefault();
            KlickOnPointStep_SourceDetectionStep    = AvailableDetectionSteps.FirstOrDefault();
            KlickOnPoint3DStep_SourceDetectionStep  = AvailableDetectionSteps.FirstOrDefault();
            KlickOnPoint3DStep_SourceCaptureStep    = AvailableCaptureSteps.FirstOrDefault();
            ShowImageStep_SourceCaptureStep         = AvailableCaptureSteps.FirstOrDefault();
            ShowImageStep_SourceDetectionStep       = AvailableDetectionSteps.FirstOrDefault();
            VideoCreationStep_SourceCaptureStep     = AvailableCaptureSteps.FirstOrDefault();
            VideoCreationStep_SourceDetectionStep   = AvailableDetectionSteps.FirstOrDefault();
        }

        // ----- Dialog-Interop -----
        public event Action<bool>? RequestClose; // true = OK, false = Cancel

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
                    ? (Mode == StepDialogMode.Edit ? "Else-If bearbeiten" : "Else-If hinzufügen")
                    : (Mode == StepDialogMode.Edit ? "Step bearbeiten" : "Step hinzufügen"))
                : (Mode == StepDialogMode.Edit ? "Job-Step bearbeiten" : "Neuen Job-Step hinzufügen");
        public string ConfirmButtonText => Mode == StepDialogMode.Edit ? "Übernehmen" : "Hinzufügen";

        // ----- Commands -----
        public ICommand ConfirmCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand BrowseTemplatePathCommand { get; }
        public ICommand BrowseScriptPathCommand { get; }
        public ICommand BrowseVideoSavePathCommand { get; }
        public ICommand ChooseMonitorCommand { get; }
        public ICommand CaptureTemplateMatchingRoiCommand { get; }
        public ICommand CaptureYoloDetectionRoiCommand { get; }
        public ICommand IfStep_AddConditionCommand { get; }
        public ICommand ElseIfStep_AddConditionCommand { get; }

        private void Confirm()
        {
            CreateStep();
            RequestClose?.Invoke(true);
        }

        private bool CanConfirm()
        {
            return SelectedType switch
            {
                "TemplateMatching" =>
                    !string.IsNullOrWhiteSpace(TemplateMatchingStep_TemplatePath)
                    && TemplateMatchingStep_ConfidenceThreshold is >= 0 and <= 1
                    && (AvailableCaptureSteps.Count == 0 || TemplateMatchingStep_SourceCaptureStep != null),
                "ShowImage" =>
                    !string.IsNullOrWhiteSpace(ShowImageStep_WindowName),
                "VideoCreation" =>
                    !string.IsNullOrWhiteSpace(VideoCreationStep_SavePath)
                    && !string.IsNullOrWhiteSpace(VideoCreationStep_FileName),
                "MakroExecution" => MakroExecutionStep_SelectedMakro != null,
                "JobExecution"   => JobExecutionStep_SelectedJob != null,
                "DesktopDuplication" => true,
                "ScriptExecution" => !string.IsNullOrWhiteSpace(ScriptExecutionStep_ScriptPath),
                "KlickOnPoint" =>
                    !string.IsNullOrWhiteSpace(KlickOnPointStep_ClickType)
                    && KlickOnPointStep_TimeoutMs >= 0
                    && (AvailableDetectionSteps.Count == 0 || KlickOnPointStep_SourceDetectionStep != null),
                "KlickOnPoint3D" =>
                    !string.IsNullOrWhiteSpace(KlickOnPoint3DStep_ClickType)
                    && KlickOnPoint3DStep_Timeout >= 0
                    && KlickOnPoint3DStep_FOV > 0
                    && (AvailableDetectionSteps.Count == 0 || KlickOnPoint3DStep_SourceDetectionStep != null)
                    && (AvailableCaptureSteps.Count == 0   || KlickOnPoint3DStep_SourceCaptureStep != null),
                "YoloDetection" =>
                    !string.IsNullOrWhiteSpace(YoloDetectionStep_Model)
                    && !string.IsNullOrWhiteSpace(YoloDetectionStep_ClassName)
                    && YoloDetectionStep_ConfidenceThreshold is >= 0 and <= 1
                    && (AvailableCaptureSteps.Count == 0 || YoloDetectionStep_SourceCaptureStep != null),
                "Timeout" => TimeoutStep_DelayMs >= 0,
                "If"      => true,
                "ElseIf"  => true,
                "Else"    => true,
                "EndIf"   => true,
                _ => false
            };
        }

        // ----- Step-Auswahl -----
        public record StepTypeItem(string Name, string Category, string? Label = null)
        {
            public string DisplayLabel => Label ?? Name;
        }

        public ListCollectionView StepTypeItems { get; } = CreateStepTypeItems();

        private static ListCollectionView CreateStepTypeItems()
        {
            var items = new List<StepTypeItem>
            {
                new("DesktopDuplication", "Erfassung"),
                new("TemplateMatching",   "Erkennung"),
                new("YoloDetection",      "Erkennung"),
                new("KlickOnPoint",       "Interaktion"),
                new("KlickOnPoint3D",     "Interaktion"),
                new("ShowImage",          "Ausgabe"),
                new("VideoCreation",      "Ausgabe"),
                new("MakroExecution",     "Automatisierung"),
                new("JobExecution",       "Automatisierung"),
                new("ScriptExecution",    "Automatisierung"),
                new("Timeout",            "Automatisierung"),
                new("If",                 "Ablaufsteuerung", "If-Abfrage"),
            };
            var view = new ListCollectionView(items);
            view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(StepTypeItem.Category)));
            return view;
        }

        // kept for CanConfirm switch — still string-based
        public string[] StepTypes { get; } =
        {
            "TemplateMatching",
            "DesktopDuplication",
            //"ProcessDuplication",
            "ShowImage",
            "VideoCreation",
            "MakroExecution",
            "JobExecution",
            "ScriptExecution",
            "KlickOnPoint",
            "KlickOnPoint3D",
            "YoloDetection",
            "Timeout"
        };

        private static string GetFirstStepTypeName() =>
            CreateStepTypeItems().Cast<StepTypeItem>().First().Name;

        private string _selectedType = GetFirstStepTypeName();
        public string SelectedType
        {
            get => _selectedType;
            set
            {
                if (_selectedType == value) return;
                _selectedType = value;
                OnChange();
                OnChange(nameof(ShowTemplateMatching));
                OnChange(nameof(ShowDesktopDuplication));
                //OnChange(nameof(ShowProcessDuplication));
                OnChange(nameof(ShowShowImage));
                OnChange(nameof(ShowVideoCreation));
                OnChange(nameof(ShowMakroExecution));
                OnChange(nameof(ShowJobExecution));
                OnChange(nameof(ShowScriptExecution));
                OnChange(nameof(ShowKlickOnPoint));
                OnChange(nameof(ShowKlickOnPoint3D));
                OnChange(nameof(ShowYoloDetection));
                OnChange(nameof(ShowTimeout));
                OnChange(nameof(ShowIf));
                OnChange(nameof(ShowElseIf));
                OnChange(nameof(ShowElse));
                OnChange(nameof(ShowEndIf));
                OnChange(nameof(StepTypeDescription));
                OnChange(nameof(StepPrerequisites));
                OnChange(nameof(StepOutput));
                (ConfirmCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public bool ShowTemplateMatching => SelectedType == "TemplateMatching";
        public bool ShowDesktopDuplication => SelectedType == "DesktopDuplication";
        //public bool ShowProcessDuplication => SelectedType == "ProcessDuplication";
        public bool ShowShowImage => SelectedType == "ShowImage";
        public bool ShowVideoCreation => SelectedType == "VideoCreation";
        public bool ShowMakroExecution => SelectedType == "MakroExecution";
        public bool ShowJobExecution => SelectedType == "JobExecution";
        public bool ShowScriptExecution => SelectedType == "ScriptExecution";
        public bool ShowKlickOnPoint => SelectedType == "KlickOnPoint";
        public bool ShowKlickOnPoint3D => SelectedType == "KlickOnPoint3D";
        public bool ShowYoloDetection => SelectedType == "YoloDetection";
        public bool ShowTimeout => SelectedType == "Timeout";
        public bool ShowIf     => SelectedType == "If";
        public bool ShowElseIf => SelectedType == "ElseIf";
        public bool ShowElse   => SelectedType == "Else";
        public bool ShowEndIf  => SelectedType == "EndIf";

        public string StepTypeDescription => SelectedType switch
        {
            "TemplateMatching"   => "Vergleicht ein Bild-Template mit der Bildquelle aus einem Erfassungs-Step. Das Ergebnis kann von einem Click on Point Step verwendet werden.",
            "DesktopDuplication" => "Nimmt einen Screenshot des gewählten Monitors auf und stellt ihn als Bildquelle für nachfolgende Steps bereit.",
            "ShowImage"          => "Zeigt das aktuelle Bild (roh oder verarbeitet) in einem separaten Vorschaufenster an.",
            "VideoCreation"      => "Speichert den aktuellen Bildstrom kontinuierlich als Video-Datei auf der Festplatte.",
            "MakroExecution"     => "Führt ein zuvor aufgezeichnetes Makro (Maus- und Tastatureingaben) aus.",
            "JobExecution"       => "Startet einen anderen Job und wartet optional auf dessen Abschluss, bevor der aktuelle Job fortgesetzt wird.",
            "ScriptExecution"    => "Führt ein externes Skript aus (PowerShell, Python, Batch, …). Mit \"Fire and Forget\" wird nicht auf die Beendigung gewartet.",
            "KlickOnPoint"       => "Klickt auf den zuletzt erkannten Bildpunkt (z. B. Ergebnis eines TemplateMatching- oder YOLO-Steps). Wartet bis zum angegebenen Timeout auf einen Fund.",
            "KlickOnPoint3D"     => "Wie KlickOnPoint, aber für 3D-Umgebungen: Die Maus wird per FOV-Berechnung auf das Zielobjekt bewegt, bevor geklickt wird.",
            "YoloDetection"      => "Erkennt Objekte im Bild mithilfe eines YOLO-KI-Modells und speichert die Fundstelle für nachfolgende Steps (z. B. KlickOnPoint).",
            "Timeout"            => "Wartet eine konfigurierbare Zeit in Millisekunden, bevor der nächste Step ausgeführt wird.",
            "If"                 => "Beginnt einen bedingten Block. Die enthaltenen Steps werden nur ausgeführt, wenn die Bedingung erfüllt ist.",
            "ElseIf"             => "Alternatives Kriterium innerhalb eines If-Blocks. Wird geprüft, wenn die vorherige Bedingung nicht zutraf.",
            "Else"               => "Markiert den Fallback-Block eines If-Blocks. Wird ausgeführt, wenn keine vorherige Bedingung zutraf.",
            "EndIf"              => "Beendet einen If/ElseIf/Else-Block.",
            _                    => string.Empty
        };

        /// <summary>Voraussetzung eines Steps mit Information ob sie durch vorherige Steps erfüllt ist.</summary>
        public sealed record PrerequisiteItem(string Name, bool IsSatisfied);

        private HashSet<string> GetAvailableOutputs()
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            foreach (var step in _precedingSteps)
            {
                var info = StepPipelineRegistry.Get(step.GetType());
                if (info != null) set.Add(info.Output);
            }
            return set;
        }

        public IReadOnlyList<PrerequisiteItem> StepPrerequisites
        {
            get
            {
                var prereqs   = StepPipelineRegistry.GetByName(SelectedType)?.Prerequisites ?? Array.Empty<string>();
                var available = GetAvailableOutputs();
                return prereqs.Select(p => new PrerequisiteItem(p, available.Contains(p))).ToList();
            }
        }

        public string StepOutput
            => StepPipelineRegistry.GetByName(SelectedType)?.Output ?? "–";

        // ----- Quell-Step-Helfer -----

        /// <summary>
        /// Builds a list of all preceding steps that produce a result of the given type name.
        /// </summary>
        private IReadOnlyList<SourceStepItem> BuildStepItems(string resultTypeName)
        {
            var items = new List<SourceStepItem>();
            for (int i = 0; i < _precedingSteps.Count; i++)
            {
                var step = _precedingSteps[i];
                var info = TaskAutomation.Steps.StepPipelineRegistry.Get(step.GetType());
                if (info?.Output != resultTypeName) continue;
                var descriptor = TaskAutomation.Steps.StepResultMetadata.ResultTypes
                    .FirstOrDefault(r => r.TypeName == resultTypeName);
                if (descriptor is null) continue;
                var name = $"{TaskAutomation.Steps.StepResultMetadata.GetFriendlyName(step.GetType().Name)} (Step {i + 1})";
                items.Add(new SourceStepItem(step.Id, name, descriptor));
            }
            return items;
        }

        /// <summary>All preceding steps that produce any evaluable result, for use in condition rows.</summary>
        private IReadOnlyList<SourceStepItem> GetConditionSourceSteps()
        {
            var items = new List<SourceStepItem>();
            for (int i = 0; i < _precedingSteps.Count; i++)
            {
                var step = _precedingSteps[i];
                var info = TaskAutomation.Steps.StepPipelineRegistry.Get(step.GetType());
                if (info is null || info.Output == "–") continue;
                var descriptor = TaskAutomation.Steps.StepResultMetadata.ResultTypes
                    .FirstOrDefault(r => r.TypeName == info.Output);
                if (descriptor is null) continue;
                var name = $"{TaskAutomation.Steps.StepResultMetadata.GetFriendlyName(step.GetType().Name)} (Step {i + 1})";
                items.Add(new SourceStepItem(step.Id, name, descriptor));
            }
            return items;
        }

        public IReadOnlyList<SourceStepItem> AvailableCaptureSteps   => BuildStepItems("CaptureResult");
        public IReadOnlyList<SourceStepItem> AvailableDetectionSteps => BuildStepItems("DetectionResult");

        // ----- Ergebnis -----
        public JobStep? CreatedStep { get; private set; }

        // ===== TemplateMatching Felder =====
        private string _templateMatchingStep_TemplatePath = string.Empty;
        public string TemplateMatchingStep_TemplatePath { get => _templateMatchingStep_TemplatePath; set { _templateMatchingStep_TemplatePath = value; OnChange(); (ConfirmCommand as RelayCommand)?.RaiseCanExecuteChanged(); } }

        private SourceStepItem? _templateMatchingStep_SourceCaptureStep;
        public SourceStepItem? TemplateMatchingStep_SourceCaptureStep
        {
            get => _templateMatchingStep_SourceCaptureStep;
            set { _templateMatchingStep_SourceCaptureStep = value; OnChange(); }
        }

        public TemplateMatchModes[] TemplateMatchModesAll { get; } =
            Enum.GetValues(typeof(TemplateMatchModes)).Cast<TemplateMatchModes>().ToArray();

        private TemplateMatchModes _templateMatchingStep_TemplateMatchMode = TemplateMatchModes.CCoeffNormed;
        public TemplateMatchModes TemplateMatchingStep_TemplateMatchMode { get => _templateMatchingStep_TemplateMatchMode; set { _templateMatchingStep_TemplateMatchMode = value; OnChange(); } }

        private double _templateMatchingStep_ConfidenceThreshold = 0.90;
        public double TemplateMatchingStep_ConfidenceThreshold
        {
            get => _templateMatchingStep_ConfidenceThreshold;
            set { _templateMatchingStep_ConfidenceThreshold = value; OnChange(); (ConfirmCommand as RelayCommand)?.RaiseCanExecuteChanged(); }
        }

        private bool _templateMatchingStep_EnableROI;
        public bool TemplateMatchingStep_EnableROI { get => _templateMatchingStep_EnableROI; set { _templateMatchingStep_EnableROI = value; OnChange(); } }

        private int _templateMatchingStep_RoiX, _templateMatchingStep_RoiY, _templateMatchingStep_RoiW, _templateMatchingStep_RoiH;
        public int TemplateMatchingStep_RoiX { get => _templateMatchingStep_RoiX; set { _templateMatchingStep_RoiX = value; OnChange(); } }
        public int TemplateMatchingStep_RoiY { get => _templateMatchingStep_RoiY; set { _templateMatchingStep_RoiY = value; OnChange(); } }
        public int TemplateMatchingStep_RoiW { get => _templateMatchingStep_RoiW; set { _templateMatchingStep_RoiW = value; OnChange(); } }
        public int TemplateMatchingStep_RoiH { get => _templateMatchingStep_RoiH; set { _templateMatchingStep_RoiH = value; OnChange(); } }

        private bool _templateMatchingStep_DrawResults = true;
        public bool TemplateMatchingStep_DrawResults { get => _templateMatchingStep_DrawResults; set { _templateMatchingStep_DrawResults = value; OnChange(); } }

        private string _scriptExecutionStep_ScriptPath = string.Empty;
        private bool _scriptExecutionStep_FireAndForget = false;
        public string ScriptExecutionStep_ScriptPath
        {
            get => _scriptExecutionStep_ScriptPath;
            set
            {
                if (_scriptExecutionStep_ScriptPath == value) return;
                _scriptExecutionStep_ScriptPath = value;
                OnChange();
                (ConfirmCommand as RelayCommand)?.RaiseCanExecuteChanged(); // << wichtig
            }
        }
        public bool ScriptExecutionStep_FireAndForget { get => _scriptExecutionStep_FireAndForget; set { _scriptExecutionStep_FireAndForget = value; OnChange(); } }

        // ===== KlickOnPointExecution Felder =====
        private bool _klickOnPointStep_DoubleClick = false;
        public bool KlickOnPointStep_DoubleClick { get => _klickOnPointStep_DoubleClick; set { _klickOnPointStep_DoubleClick = value; OnChange(); } }

        private string _klickOnPointStep_ClickType = "left";
        public string KlickOnPointStep_ClickType { get => _klickOnPointStep_ClickType; set { _klickOnPointStep_ClickType = value; OnChange(); } }

        private int _klickOnPointStep_TimeoutMs = 0;
        public int KlickOnPointStep_TimeoutMs { get => _klickOnPointStep_TimeoutMs; set { _klickOnPointStep_TimeoutMs = value; OnChange(); } }

        private SourceStepItem? _klickOnPointStep_SourceDetectionStep;
        public SourceStepItem? KlickOnPointStep_SourceDetectionStep
        {
            get => _klickOnPointStep_SourceDetectionStep;
            set { _klickOnPointStep_SourceDetectionStep = value; OnChange(); }
        }

        // ===== KlickOnPoint3D Felder =====
        private float _klickOnPoint3DStep_FOV = 90.0f;
        public float KlickOnPoint3DStep_FOV { get => _klickOnPoint3DStep_FOV; set { _klickOnPoint3DStep_FOV = value; OnChange(); } }

        private float _klickOnPoint3DStep_MausSensitivityX = 1.0f;
        public float KlickOnPoint3DStep_MausSensitivityX { get => _klickOnPoint3DStep_MausSensitivityX; set { _klickOnPoint3DStep_MausSensitivityX = value; OnChange(); } }

        private float _klickOnPoint3DStep_MausSensitivityY = 1.0f;
        public float KlickOnPoint3DStep_MausSensitivityY { get => _klickOnPoint3DStep_MausSensitivityY; set { _klickOnPoint3DStep_MausSensitivityY = value; OnChange(); } }

        private bool _klickOnPoint3DStep_DoubleClick = false;
        public bool KlickOnPoint3DStep_DoubleClick { get => _klickOnPoint3DStep_DoubleClick; set { _klickOnPoint3DStep_DoubleClick = value; OnChange(); } }

        private string _klickOnPoint3DStep_ClickType = "left";
        public string KlickOnPoint3DStep_ClickType { get => _klickOnPoint3DStep_ClickType; set { _klickOnPoint3DStep_ClickType = value; OnChange(); } }

        private int _klickOnPoint3DStep_Timeout = 0;
        public int KlickOnPoint3DStep_Timeout { get => _klickOnPoint3DStep_Timeout; set { _klickOnPoint3DStep_Timeout = value; OnChange(); } }
        private bool _klickOnPoint3DStep_InvertMouseMovementY = false;
        public bool KlickOnPoint3DStep_InvertMouseMovementY { get => _klickOnPoint3DStep_InvertMouseMovementY; set { _klickOnPoint3DStep_InvertMouseMovementY = value; OnChange(); } }
        private bool _klickOnPoint3DStep_InvertMouseMovementX = false;
        public bool KlickOnPoint3DStep_InvertMouseMovementX { get => _klickOnPoint3DStep_InvertMouseMovementX; set { _klickOnPoint3DStep_InvertMouseMovementX = value; OnChange(); } }

        private SourceStepItem? _klickOnPoint3DStep_SourceDetectionStep;
        public SourceStepItem? KlickOnPoint3DStep_SourceDetectionStep
        {
            get => _klickOnPoint3DStep_SourceDetectionStep;
            set { _klickOnPoint3DStep_SourceDetectionStep = value; OnChange(); }
        }

        private SourceStepItem? _klickOnPoint3DStep_SourceCaptureStep;
        public SourceStepItem? KlickOnPoint3DStep_SourceCaptureStep
        {
            get => _klickOnPoint3DStep_SourceCaptureStep;
            set { _klickOnPoint3DStep_SourceCaptureStep = value; OnChange(); }
        }

        // ===== YOLODetectionStep Felder =====
        private string _yoloDetectionStep_Model = string.Empty;
        public string YoloDetectionStep_Model 
        { 
            get => _yoloDetectionStep_Model; 
            set 
            { 
                _yoloDetectionStep_Model = value; 
                OnChange(); 
                OnChange(nameof(YoloDetectionStep_AvailableClasses));
                (ConfirmCommand as RelayCommand)?.RaiseCanExecuteChanged(); 
            } 
        }

        private SourceStepItem? _yoloDetectionStep_SourceCaptureStep;
        public SourceStepItem? YoloDetectionStep_SourceCaptureStep
        {
            get => _yoloDetectionStep_SourceCaptureStep;
            set { _yoloDetectionStep_SourceCaptureStep = value; OnChange(); }
        }
        
        private float _yoloDetectionStep_ConfidenceThreshold = 0.5f;
        public float YoloDetectionStep_ConfidenceThreshold { get => _yoloDetectionStep_ConfidenceThreshold; set { _yoloDetectionStep_ConfidenceThreshold = value; OnChange(); (ConfirmCommand as RelayCommand)?.RaiseCanExecuteChanged(); } }
        
        private string _yoloDetectionStep_ClassName = string.Empty;
        public string YoloDetectionStep_ClassName { get => _yoloDetectionStep_ClassName; set { _yoloDetectionStep_ClassName = value; OnChange(); (ConfirmCommand as RelayCommand)?.RaiseCanExecuteChanged(); } }
        
        private bool _yoloDetectionStep_DrawResults = true;
        public bool YoloDetectionStep_DrawResults { get => _yoloDetectionStep_DrawResults; set { _yoloDetectionStep_DrawResults = value; OnChange(); } }
        
        private bool _yoloDetectionStep_EnableROI = false;
        public bool YoloDetectionStep_EnableROI { get => _yoloDetectionStep_EnableROI; set { _yoloDetectionStep_EnableROI = value; OnChange(); } }
        
        private int _yoloDetectionStep_RoiX, _yoloDetectionStep_RoiY, _yoloDetectionStep_RoiW, _yoloDetectionStep_RoiH;
        public int YoloDetectionStep_RoiX { get => _yoloDetectionStep_RoiX; set { _yoloDetectionStep_RoiX = value; OnChange(); } }
        public int YoloDetectionStep_RoiY { get => _yoloDetectionStep_RoiY; set { _yoloDetectionStep_RoiY = value; OnChange(); } }
        public int YoloDetectionStep_RoiW { get => _yoloDetectionStep_RoiW; set { _yoloDetectionStep_RoiW = value; OnChange(); } }
        public int YoloDetectionStep_RoiH { get => _yoloDetectionStep_RoiH; set { _yoloDetectionStep_RoiH = value; OnChange(); } }

        // YOLO Listen Properties
        public ObservableCollection<string> YoloDetectionStep_AvailableModels
        {
            get
            {
                try
                {
                    var yoloManager = _ctx.YoloManager;
                    if (yoloManager != null)
                    {
                        var models = yoloManager.GetAvailableModels();
                        return new ObservableCollection<string>(models);
                    }
                }
                catch (Exception ex)
                {
                    // Fehler beim Laden der Modelle - Log könnte hier hilfreich sein
                    System.Diagnostics.Debug.WriteLine($"Fehler beim Laden der YOLO-Modelle: {ex.Message}");
                }
                return new ObservableCollection<string>();
            }
        }

        public ObservableCollection<string> YoloDetectionStep_AvailableClasses
        {
            get
            {
                if (string.IsNullOrWhiteSpace(YoloDetectionStep_Model))
                    return new ObservableCollection<string>();

                try
                {
                    var yoloManager = _ctx.YoloManager;
                    if (yoloManager != null)
                    {
                        var classes = yoloManager.GetClassesForModel(YoloDetectionStep_Model);
                        return new ObservableCollection<string>(classes);
                    }
                }
                catch (Exception ex)
                {
                    // Fehler beim Laden der Klassen - Log könnte hier hilfreich sein
                    System.Diagnostics.Debug.WriteLine($"Fehler beim Laden der YOLO-Klassen für Model '{YoloDetectionStep_Model}': {ex.Message}");
                }
                return new ObservableCollection<string>();
            }
        }

        // Datei-Auswahl (in VM gewünscht)
        private void BrowseTemplatePath()
        {
            var ofd = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Template-Datei auswählen",
                Filter = "Bilder (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|Alle Dateien (*.*)|*.*",
                CheckFileExists = true,
                Multiselect = false
            };
            if (ofd.ShowDialog() == true)
            {
                TemplateMatchingStep_TemplatePath = ofd.FileName;
            }
        }

        private void BrowseScriptPath()
        {
            var ofd = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Script-Datei auswählen",
                Filter =
                    "Skripte (*.ps1;*.bat;*.cmd;*.sh;*.py;*.js;*.vbs;*.wsf;*.exe)|*.ps1;*.bat;*.cmd;*.sh;*.py;*.js;*.vbs;*.wsf;*.exe|" +
                    "Alle Dateien (*.*)|*.*",
                CheckFileExists = true,
                Multiselect = false
            };

            if (ofd.ShowDialog() == true)
            {
                ScriptExecutionStep_ScriptPath = ofd.FileName;
            }
        }

        private void BrowseVideoSavePath()
        {
            var folderDialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Ordner für Video-Speicherung auswählen",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = true
            };

            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                VideoCreationStep_SavePath = folderDialog.SelectedPath;
            }
        }

        private void ChooseMonitor()
        {
            try
            {
                int selectedMonitorIndex = ShowMonitorSelectionOverlay();
                if (selectedMonitorIndex >= 0)
                {
                    DesktopDuplicationStep_DesktopIdx = selectedMonitorIndex;
                }
            }
            catch (Exception ex)
            {
                AppDialog.Show($"Fehler bei der Monitor-Auswahl: {ex.Message}", "Fehler",
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

        private async void CaptureTemplateMatchingRoi()
        {
            try
            {
                var roiOverlay = new DesktopOverlay.RoiCaptureOverlay();
                var rect = await roiOverlay.CaptureRoiAsync();
                
                TemplateMatchingStep_RoiX = rect.X;
                TemplateMatchingStep_RoiY = rect.Y;
                TemplateMatchingStep_RoiW = rect.Width;
                TemplateMatchingStep_RoiH = rect.Height;
            }
            catch (OperationCanceledException)
            {
                // User aborted with ESC - this is not an error, just ignore
                return;
            }
            catch (Exception ex)
            {
                // Handle actual errors
                AppDialog.Show($"Error capturing ROI: {ex.Message}", "Fehler",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private async void CaptureYoloDetectionRoi()
        {
            try
            {
                var roiOverlay = new DesktopOverlay.RoiCaptureOverlay();
                var rect = await roiOverlay.CaptureRoiAsync();

                YoloDetectionStep_RoiX = rect.X;
                YoloDetectionStep_RoiY = rect.Y;
                YoloDetectionStep_RoiW = rect.Width;
                YoloDetectionStep_RoiH = rect.Height;
            }
            catch (OperationCanceledException)
            {
                // User aborted with ESC - this is not an error, just ignore
                return;
            }
            catch (Exception ex)
            {
                // Handle actual errors
                AppDialog.Show($"Error capturing ROI: {ex.Message}", "Fehler",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        // ===== DesktopDuplication Felder =====
        private int _desktopDuplicationStep_DesktopIdx;
        public int DesktopDuplicationStep_DesktopIdx { get => _desktopDuplicationStep_DesktopIdx; set { _desktopDuplicationStep_DesktopIdx = value; OnChange(); } }

        // ===== ProcessDuplication Felder =====
        //private string _processName = string.Empty;
        //public string ProcessName { get => _processName; set { _processName = value; OnChange(); (ConfirmCommand as RelayCommand)?.RaiseCanExecuteChanged(); } }

        // ===== ShowImage Felder =====
        private string _showImageStep_WindowName = "MyWindow";
        public string ShowImageStep_WindowName { get => _showImageStep_WindowName; set { _showImageStep_WindowName = value; OnChange(); (ConfirmCommand as RelayCommand)?.RaiseCanExecuteChanged(); } }

        private SourceStepItem? _showImageStep_SourceCaptureStep;
        public SourceStepItem? ShowImageStep_SourceCaptureStep
        {
            get => _showImageStep_SourceCaptureStep;
            set { _showImageStep_SourceCaptureStep = value; OnChange(); }
        }

        private SourceStepItem? _showImageStep_SourceDetectionStep;
        public SourceStepItem? ShowImageStep_SourceDetectionStep
        {
            get => _showImageStep_SourceDetectionStep;
            set { _showImageStep_SourceDetectionStep = value; OnChange(); }
        }

        private bool _showImageStep_ShowRawImage = true;
        public bool ShowImageStep_ShowRawImage 
        { 
            get => _showImageStep_ShowRawImage; 
            set 
            { 
                _showImageStep_ShowRawImage = value; 
                OnChange(); 
                OnChange(nameof(ShowImageStep_ShowProcessedImage));
                OnChange(nameof(ShowImageStep_ImageMode));
            } 
        }

        private bool _showImageStep_ShowProcessedImage = false;
        public bool ShowImageStep_ShowProcessedImage 
        { 
            get => _showImageStep_ShowProcessedImage; 
            set 
            { 
                _showImageStep_ShowProcessedImage = value; 
                OnChange(); 
                OnChange(nameof(ShowImageStep_ShowRawImage));
                OnChange(nameof(ShowImageStep_ImageMode));
            } 
        }

        public string[] ImageModeOptions { get; } = { "Rohbild", "Verarbeitetes Bild", "Beides" };

        public string ShowImageStep_ImageMode
        {
            get => (_showImageStep_ShowRawImage, _showImageStep_ShowProcessedImage) switch
            {
                (true,  false) => "Rohbild",
                (false, true)  => "Verarbeitetes Bild",
                _              => "Beides"
            };
            set
            {
                _showImageStep_ShowRawImage     = value != "Verarbeitetes Bild";
                _showImageStep_ShowProcessedImage = value != "Rohbild";
                OnChange();
                OnChange(nameof(ShowImageStep_ShowRawImage));
                OnChange(nameof(ShowImageStep_ShowProcessedImage));
            }
        }

        // ===== VideoCreation Felder =====
        private string _videoCreationStep_SavePath = string.Empty;
        public string VideoCreationStep_SavePath { get => _videoCreationStep_SavePath; set { _videoCreationStep_SavePath = value; OnChange(); } }

        private string _videoCreationStep_FileName = "output.mp4";
        public string VideoCreationStep_FileName { get => _videoCreationStep_FileName; set { _videoCreationStep_FileName = value; OnChange(); (ConfirmCommand as RelayCommand)?.RaiseCanExecuteChanged(); } }

        private SourceStepItem? _videoCreationStep_SourceCaptureStep;
        public SourceStepItem? VideoCreationStep_SourceCaptureStep
        {
            get => _videoCreationStep_SourceCaptureStep;
            set { _videoCreationStep_SourceCaptureStep = value; OnChange(); }
        }

        private SourceStepItem? _videoCreationStep_SourceDetectionStep;
        public SourceStepItem? VideoCreationStep_SourceDetectionStep
        {
            get => _videoCreationStep_SourceDetectionStep;
            set { _videoCreationStep_SourceDetectionStep = value; OnChange(); }
        }

        private bool _videoCreationStep_UseRawImage = true;
        public bool VideoCreationStep_UseRawImage 
        { 
            get => _videoCreationStep_UseRawImage; 
            set 
            { 
                _videoCreationStep_UseRawImage = value; 
                OnChange(); 
                OnChange(nameof(VideoCreationStep_UseProcessedImage));
                OnChange(nameof(VideoCreationStep_ImageMode));
            } 
        }

        private bool _videoCreationStep_UseProcessedImage = false;
        public bool VideoCreationStep_UseProcessedImage 
        { 
            get => _videoCreationStep_UseProcessedImage; 
            set 
            { 
                _videoCreationStep_UseProcessedImage = value; 
                OnChange(); 
                OnChange(nameof(VideoCreationStep_UseRawImage));
                OnChange(nameof(VideoCreationStep_ImageMode));
            } 
        }

        public string VideoCreationStep_ImageMode
        {
            get => (_videoCreationStep_UseRawImage, _videoCreationStep_UseProcessedImage) switch
            {
                (true,  false) => "Rohbild",
                (false, true)  => "Verarbeitetes Bild",
                _              => "Beides"
            };
            set
            {
                _videoCreationStep_UseRawImage     = value != "Verarbeitetes Bild";
                _videoCreationStep_UseProcessedImage = value != "Rohbild";
                OnChange();
                OnChange(nameof(VideoCreationStep_UseRawImage));
                OnChange(nameof(VideoCreationStep_UseProcessedImage));
            }
        }

        // ===== JobExecution Felder =====
        public ObservableCollection<Job> AvailableJobs
        {
            get
            {
                var allJobs = _ctx.AllJobs?.Values?.Where(j => j != null) ?? Enumerable.Empty<Job>();
                var availableJobs = allJobs
                    .Where(j => j.Id != _currentJobId)
                    .OrderBy(j => j.Name);
                return new ObservableCollection<Job>(availableJobs);
            }
        }

        private Job? _jobExecutionStep_SelectedJob;
        public Job? JobExecutionStep_SelectedJob
        {
            get => _jobExecutionStep_SelectedJob;
            set
            {
                _jobExecutionStep_SelectedJob = value;
                _jobExecutionStep_SelectedJobId = value?.Id;
                _jobExecutionStep_SelectedJobName = value?.Name;
                OnChange();
                (ConfirmCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        private string? _jobExecutionStep_SelectedJobName;
        public string? JobExecutionStep_SelectedJobName
        {
            get => _jobExecutionStep_SelectedJobName;
            set { _jobExecutionStep_SelectedJobName = value; OnChange(); (ConfirmCommand as RelayCommand)?.RaiseCanExecuteChanged(); }
        }

        private Guid? _jobExecutionStep_SelectedJobId;
        public Guid? JobExecutionStep_SelectedJobId
        {
            get => _jobExecutionStep_SelectedJobId;
            set { _jobExecutionStep_SelectedJobId = value; OnChange(); (ConfirmCommand as RelayCommand)?.RaiseCanExecuteChanged(); }
        }

        private bool _jobExecutionStep_WaitForCompletion = true;
        public bool JobExecutionStep_WaitForCompletion { get => _jobExecutionStep_WaitForCompletion; set { _jobExecutionStep_WaitForCompletion = value; OnChange(); } }

        // ===== MakroExecution Felder =====
        public ObservableCollection<Makro> AvailableMakros => new ObservableCollection<Makro>(
            _ctx.AllMakros?.Values?.OrderBy(m => m.Name) ?? Enumerable.Empty<Makro>());

        private Makro? _makroExecutionStep_SelectedMakro;
        public Makro? MakroExecutionStep_SelectedMakro
        {
            get => _makroExecutionStep_SelectedMakro;
            set
            {
                _makroExecutionStep_SelectedMakro = value;
                _makroExecutionStep_SelectedMakroId = value?.Id;
                _makroExecutionStep_SelectedMakroName = value?.Name;
                OnChange();
                (ConfirmCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        private string? _makroExecutionStep_SelectedMakroName;
        public string? MakroExecutionStep_SelectedMakroName
        {
            get => _makroExecutionStep_SelectedMakroName;
            set { _makroExecutionStep_SelectedMakroName = value; OnChange(); (ConfirmCommand as RelayCommand)?.RaiseCanExecuteChanged(); }
        }

        private Guid? _makroExecutionStep_SelectedMakroId;
        public Guid? MakroExecutionStep_SelectedMakroId
        {
            get => _makroExecutionStep_SelectedMakroId;
            set { _makroExecutionStep_SelectedMakroId = value; OnChange(); (ConfirmCommand as RelayCommand)?.RaiseCanExecuteChanged(); }
        }

        // ===== Timeout Felder =====
        private int _timeoutStep_DelayMs = 1000;
        public int TimeoutStep_DelayMs { get => _timeoutStep_DelayMs; set { _timeoutStep_DelayMs = value; OnChange(); (ConfirmCommand as RelayCommand)?.RaiseCanExecuteChanged(); } }

        // ===== If / ElseIf Felder =====

        // ── MatchMode ──
        private TaskAutomation.Jobs.ConditionMatchMode _ifStep_MatchMode = TaskAutomation.Jobs.ConditionMatchMode.All;
        public TaskAutomation.Jobs.ConditionMatchMode IfStep_MatchMode
        {
            get => _ifStep_MatchMode;
            set { _ifStep_MatchMode = value; OnChange(); OnChange(nameof(IfStep_MatchModeIsAll)); OnChange(nameof(IfStep_MatchModeIsAny)); }
        }
        public bool IfStep_MatchModeIsAll
        {
            get => _ifStep_MatchMode == TaskAutomation.Jobs.ConditionMatchMode.All;
            set { if (value) IfStep_MatchMode = TaskAutomation.Jobs.ConditionMatchMode.All; }
        }
        public bool IfStep_MatchModeIsAny
        {
            get => _ifStep_MatchMode == TaskAutomation.Jobs.ConditionMatchMode.Any;
            set { if (value) IfStep_MatchMode = TaskAutomation.Jobs.ConditionMatchMode.Any; }
        }
        public System.Collections.ObjectModel.ObservableCollection<ConditionRowViewModel> IfStep_Conditions { get; } = new();

        private TaskAutomation.Jobs.ConditionMatchMode _elseIfStep_MatchMode = TaskAutomation.Jobs.ConditionMatchMode.All;
        public TaskAutomation.Jobs.ConditionMatchMode ElseIfStep_MatchMode
        {
            get => _elseIfStep_MatchMode;
            set { _elseIfStep_MatchMode = value; OnChange(); OnChange(nameof(ElseIfStep_MatchModeIsAll)); OnChange(nameof(ElseIfStep_MatchModeIsAny)); }
        }
        public bool ElseIfStep_MatchModeIsAll
        {
            get => _elseIfStep_MatchMode == TaskAutomation.Jobs.ConditionMatchMode.All;
            set { if (value) ElseIfStep_MatchMode = TaskAutomation.Jobs.ConditionMatchMode.All; }
        }
        public bool ElseIfStep_MatchModeIsAny
        {
            get => _elseIfStep_MatchMode == TaskAutomation.Jobs.ConditionMatchMode.Any;
            set { if (value) ElseIfStep_MatchMode = TaskAutomation.Jobs.ConditionMatchMode.Any; }
        }
        public System.Collections.ObjectModel.ObservableCollection<ConditionRowViewModel> ElseIfStep_Conditions { get; } = new();

        // ── Load from existing settings (called from Prefill) ──
        public void LoadIfStepConditions(TaskAutomation.Jobs.IfConditionSettings settings)
        {
            IfStep_MatchMode = settings.MatchMode;
            IfStep_Conditions.Clear();
            foreach (var c in settings.Conditions)
            {
                var row = new ConditionRowViewModel(IfStep_Conditions, GetConditionSourceSteps());
                row.LoadFrom(c);
                IfStep_Conditions.Add(row);
            }
            if (IfStep_Conditions.Count == 0)
                IfStep_Conditions.Add(new ConditionRowViewModel(IfStep_Conditions, GetConditionSourceSteps()));
        }

        public void LoadElseIfStepConditions(TaskAutomation.Jobs.IfConditionSettings settings)
        {
            ElseIfStep_MatchMode = settings.MatchMode;
            ElseIfStep_Conditions.Clear();
            foreach (var c in settings.Conditions)
            {
                var row = new ConditionRowViewModel(ElseIfStep_Conditions, GetConditionSourceSteps());
                row.LoadFrom(c);
                ElseIfStep_Conditions.Add(row);
            }
            if (ElseIfStep_Conditions.Count == 0)
                ElseIfStep_Conditions.Add(new ConditionRowViewModel(ElseIfStep_Conditions, GetConditionSourceSteps()));
        }

        // ===== Fabrik =====
        public void CreateStep()
        {
            CreatedStep = SelectedType switch
            {
                "TemplateMatching" => new TemplateMatchingStep
                {
                    Settings = new TemplateMatchingSettings
                    {
                        TemplatePath = TemplateMatchingStep_TemplatePath,
                        TemplateMatchMode = TemplateMatchingStep_TemplateMatchMode,
                        ConfidenceThreshold = TemplateMatchingStep_ConfidenceThreshold,
                        EnableROI = TemplateMatchingStep_EnableROI,
                        ROI = new Rect(TemplateMatchingStep_RoiX, TemplateMatchingStep_RoiY, TemplateMatchingStep_RoiW, TemplateMatchingStep_RoiH),
                        DrawResults = TemplateMatchingStep_DrawResults,
                        SourceCaptureStepId = TemplateMatchingStep_SourceCaptureStep?.StepId ?? ""
                    }
                },
                "DesktopDuplication" => new DesktopDuplicationStep
                {
                    Settings = new DesktopDuplicationSettings
                    {
                        DesktopIdx = DesktopDuplicationStep_DesktopIdx
                    }
                },
                //"ProcessDuplication" => new ProcessDuplicationStep
                //{
                //    Settings = new ProcessDuplicationSettings
                //    {
                //        ProcessName = ProcessName
                //    }
                //},
                "ShowImage" => new ShowImageStep
                {
                    Settings = new ShowImageSettings
                    {
                        WindowName = ShowImageStep_WindowName,
                        ShowRawImage = ShowImageStep_ShowRawImage,
                        ShowProcessedImage = ShowImageStep_ShowProcessedImage,
                        SourceCaptureStepId   = ShowImageStep_SourceCaptureStep?.StepId ?? "",
                        SourceDetectionStepId = ShowImageStep_SourceDetectionStep?.StepId ?? ""
                    }
                },
                "VideoCreation" => new VideoCreationStep
                {
                    Settings = new VideoCreationSettings
                    {
                        SavePath = VideoCreationStep_SavePath,
                        FileName = VideoCreationStep_FileName,
                        UseRawImage = VideoCreationStep_UseRawImage,
                        UseProcessedImage = VideoCreationStep_UseProcessedImage,
                        SourceCaptureStepId   = VideoCreationStep_SourceCaptureStep?.StepId ?? "",
                        SourceDetectionStepId = VideoCreationStep_SourceDetectionStep?.StepId ?? ""
                    }
                },
                "MakroExecution" => new MakroExecutionStep
                {
                    Settings = new MakroExecutionSettings
                    {
                        MakroName = MakroExecutionStep_SelectedMakro?.Name ?? string.Empty,
                        MakroId = MakroExecutionStep_SelectedMakro?.Id
                    }
                },
                "JobExecution" => new JobExecutionStep
                {
                    Settings = new JobExecutionStepSettings
                    {
                        JobName = JobExecutionStep_SelectedJob?.Name ?? string.Empty,
                        JobId = JobExecutionStep_SelectedJob?.Id,
                        WaitForCompletion = JobExecutionStep_WaitForCompletion
                    }
                },
                "ScriptExecution" => new ScriptExecutionStep
                {
                    Settings = new ScriptExecutionSettings
                    {
                        ScriptPath = ScriptExecutionStep_ScriptPath,
                        FireAndForget = ScriptExecutionStep_FireAndForget
                    }
                },
                "KlickOnPoint" => new KlickOnPointStep
                {
                    Settings = new KlickOnPointSettings
                    {
                        DoubleClick = KlickOnPointStep_DoubleClick,
                        ClickType = KlickOnPointStep_ClickType,
                        TimeoutMs = KlickOnPointStep_TimeoutMs,
                        SourceDetectionStepId = KlickOnPointStep_SourceDetectionStep?.StepId ?? ""
                    }
                },
                "KlickOnPoint3D" => new KlickOnPoint3DStep
                {
                    Settings = new KlickOnPoint3DSettings
                    {
                        FOV = KlickOnPoint3DStep_FOV,
                        MausSensitivityX = KlickOnPoint3DStep_MausSensitivityX,
                        MausSensitivityY = KlickOnPoint3DStep_MausSensitivityY,
                        DoubleClick = KlickOnPoint3DStep_DoubleClick,
                        ClickType = KlickOnPoint3DStep_ClickType,
                        TimeoutMs = KlickOnPoint3DStep_Timeout,
                        InvertMouseMovementY = KlickOnPoint3DStep_InvertMouseMovementY,
                        InvertMouseMovementX = KlickOnPoint3DStep_InvertMouseMovementX,
                        SourceDetectionStepId = KlickOnPoint3DStep_SourceDetectionStep?.StepId ?? "",
                        SourceCaptureStepId   = KlickOnPoint3DStep_SourceCaptureStep?.StepId ?? ""
                    }
                },
                "YoloDetection" => new YOLODetectionStep
                {
                    Settings = new YOLODetectionStepSettings
                    {
                        Model = YoloDetectionStep_Model,
                        ConfidenceThreshold = YoloDetectionStep_ConfidenceThreshold,
                        ClassName = YoloDetectionStep_ClassName,
                        DrawResults = YoloDetectionStep_DrawResults,
                        EnableROI = YoloDetectionStep_EnableROI,
                        ROI = new Rect(YoloDetectionStep_RoiX, YoloDetectionStep_RoiY, YoloDetectionStep_RoiW, YoloDetectionStep_RoiH),
                        SourceCaptureStepId = YoloDetectionStep_SourceCaptureStep?.StepId ?? ""
                    }
                },
                "Timeout" => new TimeoutStep
                {
                    Settings = new TimeoutSettings
                    {
                        DelayMs = TimeoutStep_DelayMs
                    }
                },
                "If" => new TaskAutomation.Jobs.IfStep
                {
                    Settings = new TaskAutomation.Jobs.IfConditionSettings
                    {
                        MatchMode  = IfStep_MatchMode,
                        Conditions = IfStep_Conditions.Select(c => c.ToCondition()).ToList()
                    }
                },
                "ElseIf" => new TaskAutomation.Jobs.ElseIfStep
                {
                    Settings = new TaskAutomation.Jobs.IfConditionSettings
                    {
                        MatchMode  = ElseIfStep_MatchMode,
                        Conditions = ElseIfStep_Conditions.Select(c => c.ToCondition()).ToList()
                    }
                },
                "Else"  => new TaskAutomation.Jobs.ElseStep(),
                "EndIf" => new TaskAutomation.Jobs.EndIfStep(),
                _ => null
            };
        }
    }
}

