using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using DesktopAutomationApp.Services.Preview;
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

        private readonly IJobExecutionContext _ctx;
        public AddJobStepDialogViewModel(IJobExecutionContext ctx)
        {
            _ctx = ctx;
            ConfirmCommand = new RelayCommand(Confirm, CanConfirm);
            CancelCommand = new RelayCommand(() => RequestClose?.Invoke(false));
            BrowseTemplatePathCommand = new RelayCommand(BrowseTemplatePath);
            BrowseScriptPathCommand = new RelayCommand(BrowseScriptPath);
        }

        // ----- Dialog-Interop -----
        public event Action<bool>? RequestClose; // true = OK, false = Cancel

        private StepDialogMode _mode = StepDialogMode.Add;
        public StepDialogMode Mode
        {
            get => _mode;
            set { _mode = value; OnChange(); OnChange(nameof(DialogTitle)); OnChange(nameof(ConfirmButtonText)); }
        }

        public string DialogTitle => Mode == StepDialogMode.Edit ? "Job-Step bearbeiten" : "Neuen Job-Step hinzufügen";
        public string ConfirmButtonText => Mode == StepDialogMode.Edit ? "Übernehmen" : "Hinzufügen";

        // ----- Commands -----
        public ICommand ConfirmCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand BrowseTemplatePathCommand { get; }
        public ICommand BrowseScriptPathCommand { get; }

        private void Confirm()
        {
            CreateStep();
            RequestClose?.Invoke(true);
        }

        private bool CanConfirm()
        {
            return SelectedType switch
            {
                "TemplateMatching" => !string.IsNullOrWhiteSpace(TemplatePath) && ConfidenceThreshold is >= 0 and <= 1,
                //"ProcessDuplication" => !string.IsNullOrWhiteSpace(ProcessName),
                "ShowImage" => !string.IsNullOrWhiteSpace(WindowName),
                "VideoCreation" => !string.IsNullOrWhiteSpace(FileName),
                "MakroExecution" => !string.IsNullOrWhiteSpace(SelectedMakroName),
                "DesktopDuplication" => true,
                "ScriptExecution" => !string.IsNullOrWhiteSpace(ScriptPath),
                _ => false
            };
        }

        // ----- Step-Auswahl -----
        public string[] StepTypes { get; } =
        {
            "TemplateMatching",
            "DesktopDuplication",
            //"ProcessDuplication",
            "ShowImage",
            "VideoCreation",
            "MakroExecution",
            "ScriptExecution"
        };

        private string _selectedType = "TemplateMatching";
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
                OnChange(nameof(ShowScriptExecution));
                (ConfirmCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public bool ShowTemplateMatching => SelectedType == "TemplateMatching";
        public bool ShowDesktopDuplication => SelectedType == "DesktopDuplication";
        //public bool ShowProcessDuplication => SelectedType == "ProcessDuplication";
        public bool ShowShowImage => SelectedType == "ShowImage";
        public bool ShowVideoCreation => SelectedType == "VideoCreation";
        public bool ShowMakroExecution => SelectedType == "MakroExecution";
        public bool ShowScriptExecution => SelectedType == "ScriptExecution";

        // ----- Ergebnis -----
        public JobStep? CreatedStep { get; private set; }

        // ===== TemplateMatching Felder =====
        private string _templatePath = string.Empty;
        public string TemplatePath { get => _templatePath; set { _templatePath = value; OnChange(); (ConfirmCommand as RelayCommand)?.RaiseCanExecuteChanged(); } }

        public TemplateMatchModes[] TemplateMatchModesAll { get; } =
            Enum.GetValues(typeof(TemplateMatchModes)).Cast<TemplateMatchModes>().ToArray();

        private TemplateMatchModes _templateMatchMode = TemplateMatchModes.CCoeffNormed;
        public TemplateMatchModes TemplateMatchMode { get => _templateMatchMode; set { _templateMatchMode = value; OnChange(); } }

        private bool _multiplePoints;
        public bool MultiplePoints { get => _multiplePoints; set { _multiplePoints = value; OnChange(); } }

        private double _confidenceThreshold = 0.90;
        public double ConfidenceThreshold
        {
            get => _confidenceThreshold;
            set { _confidenceThreshold = value; OnChange(); (ConfirmCommand as RelayCommand)?.RaiseCanExecuteChanged(); }
        }

        private bool _enableROI;
        public bool EnableROI { get => _enableROI; set { _enableROI = value; OnChange(); } }

        private int _roiX, _roiY, _roiW, _roiH;
        public int RoiX { get => _roiX; set { _roiX = value; OnChange(); } }
        public int RoiY { get => _roiY; set { _roiY = value; OnChange(); } }
        public int RoiW { get => _roiW; set { _roiW = value; OnChange(); } }
        public int RoiH { get => _roiH; set { _roiH = value; OnChange(); } }

        private bool _drawResults = true;
        public bool DrawResults { get => _drawResults; set { _drawResults = value; OnChange(); } }

        private string _scriptPath = string.Empty;
        private bool _fireAndForget = false;
        public string ScriptPath { get => _scriptPath; set { _scriptPath = value; OnChange(); } }
        public bool FireAndForget { get => _fireAndForget; set { _fireAndForget = value; OnChange(); } }

        // Datei-Auswahl (in VM gewünscht)
        private void BrowseTemplatePath()
        {
            var ofd = new OpenFileDialog
            {
                Title = "Template-Datei auswählen",
                Filter = "Bilder (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|Alle Dateien (*.*)|*.*",
                CheckFileExists = true,
                Multiselect = false
            };
            if (ofd.ShowDialog() == true)
            {
                TemplatePath = ofd.FileName;
            }
        }

        private void BrowseScriptPath()
        {
            var ofd = new OpenFileDialog
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
                ScriptPath = ofd.FileName;
            }
        }

        // ===== DesktopDuplication Felder =====
        private int _desktopIdx;
        public int DesktopIdx { get => _desktopIdx; set { _desktopIdx = value; OnChange(); } }

        // ===== ProcessDuplication Felder =====
        //private string _processName = string.Empty;
        //public string ProcessName { get => _processName; set { _processName = value; OnChange(); (ConfirmCommand as RelayCommand)?.RaiseCanExecuteChanged(); } }

        // ===== ShowImage Felder =====
        private string _windowName = "MyWindow";
        public string WindowName { get => _windowName; set { _windowName = value; OnChange(); (ConfirmCommand as RelayCommand)?.RaiseCanExecuteChanged(); } }

        private bool _showRawImage = true;
        public bool ShowRawImage { get => _showRawImage; set { _showRawImage = value; OnChange(); } }

        private bool _showProcessedImage = false;
        public bool ShowProcessedImage { get => _showProcessedImage; set { _showProcessedImage = value; OnChange(); } }

        // ===== VideoCreation Felder =====
        private string _savePath = string.Empty;
        public string SavePath { get => _savePath; set { _savePath = value; OnChange(); } }

        private string _fileName = "output.mp4";
        public string FileName { get => _fileName; set { _fileName = value; OnChange(); (ConfirmCommand as RelayCommand)?.RaiseCanExecuteChanged(); } }

        private bool _useRawImage = true;
        public bool UseRawImage { get => _useRawImage; set { _useRawImage = value; OnChange(); } }

        private bool _useProcessedImage = true;
        public bool UseProcessedImage { get => _useProcessedImage; set { _useProcessedImage = value; OnChange(); } }

        // ===== MakroExecution Felder =====
        public ObservableCollection<string> MakroNames => new ObservableCollection<string>(_ctx.AllMakros?.Keys?.OrderBy(n => n) ?? Enumerable.Empty<string>());
        private string? _selectedMakroName;
        public string? SelectedMakroName
        {
            get => _selectedMakroName;
            set { _selectedMakroName = value; OnChange(); (ConfirmCommand as RelayCommand)?.RaiseCanExecuteChanged(); }
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
                        TemplatePath = TemplatePath,
                        TemplateMatchMode = TemplateMatchMode,
                        MultiplePoints = MultiplePoints,
                        ConfidenceThreshold = ConfidenceThreshold,
                        EnableROI = EnableROI,
                        ROI = new Rect(RoiX, RoiY, RoiW, RoiH),
                        DrawResults = DrawResults
                    }
                },
                "DesktopDuplication" => new DesktopDuplicationStep
                {
                    Settings = new DesktopDuplicationSettings
                    {
                        DesktopIdx = DesktopIdx
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
                        WindowName = WindowName,
                        ShowRawImage = ShowRawImage,
                        ShowProcessedImage = ShowProcessedImage
                    }
                },
                "VideoCreation" => new VideoCreationStep
                {
                    Settings = new VideoCreationSettings
                    {
                        SavePath = SavePath,
                        FileName = FileName,
                        UseRawImage = UseRawImage,
                        UseProcessedImage = UseProcessedImage
                    }
                },
                "MakroExecution" => new MakroExecutionStep
                {
                    Settings = new MakroExecutionSettings
                    {
                        MakroName = SelectedMakroName ?? MakroNames.FirstOrDefault(string.Empty)
                    }
                },
                "ScriptExecution" => new ScriptExecutionStep
                {
                    Settings = new ScriptExecutionSettings
                    {
                        ScriptPath = string.Empty,
                        FireAndForget = false
                    }
                },
                _ => null
            };
        }
    }
}
