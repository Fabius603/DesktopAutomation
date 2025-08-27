using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Forms;
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
                "TemplateMatching" => !string.IsNullOrWhiteSpace(TemplateMatchingStep_TemplatePath) && TemplateMatchingStep_ConfidenceThreshold is >= 0 and <= 1,
                //"ProcessDuplication" => !string.IsNullOrWhiteSpace(ProcessName),
                "ShowImage" => !string.IsNullOrWhiteSpace(ShowImageStep_WindowName),
                "VideoCreation" => !string.IsNullOrWhiteSpace(VideoCreationStep_FileName),
                "MakroExecution" => !string.IsNullOrWhiteSpace(MakroExecutionStep_SelectedMakroName),
                "DesktopDuplication" => true,
                "ScriptExecution" => !string.IsNullOrWhiteSpace(ScriptExecutionStep_ScriptPath),
                "KlickOnPoint" => !string.IsNullOrWhiteSpace(KlickOnPointStep_ClickType) && KlickOnPointStep_TimeoutMs >= 0,
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
            "ScriptExecution",
            "KlickOnPoint"
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
                OnChange(nameof(ShowKlickOnPoint));
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
        public bool ShowKlickOnPoint => SelectedType == "KlickOnPoint";

        // ----- Ergebnis -----
        public JobStep? CreatedStep { get; private set; }

        // ===== TemplateMatching Felder =====
        private string _templateMatchingStep_TemplatePath = string.Empty;
        public string TemplateMatchingStep_TemplatePath { get => _templateMatchingStep_TemplatePath; set { _templateMatchingStep_TemplatePath = value; OnChange(); (ConfirmCommand as RelayCommand)?.RaiseCanExecuteChanged(); } }

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

        private int _klickOnPointStep_TimeoutMs = 5000;
        public int KlickOnPointStep_TimeoutMs { get => _klickOnPointStep_TimeoutMs; set { _klickOnPointStep_TimeoutMs = value; OnChange(); } }

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

        // ===== DesktopDuplication Felder =====
        private int _desktopDuplicationStep_DesktopIdx;
        public int DesktopDuplicationStep_DesktopIdx { get => _desktopDuplicationStep_DesktopIdx; set { _desktopDuplicationStep_DesktopIdx = value; OnChange(); } }

        // ===== ProcessDuplication Felder =====
        //private string _processName = string.Empty;
        //public string ProcessName { get => _processName; set { _processName = value; OnChange(); (ConfirmCommand as RelayCommand)?.RaiseCanExecuteChanged(); } }

        // ===== ShowImage Felder =====
        private string _showImageStep_WindowName = "MyWindow";
        public string ShowImageStep_WindowName { get => _showImageStep_WindowName; set { _showImageStep_WindowName = value; OnChange(); (ConfirmCommand as RelayCommand)?.RaiseCanExecuteChanged(); } }

        private bool _showImageStep_ShowRawImage = true;
        public bool ShowImageStep_ShowRawImage { get => _showImageStep_ShowRawImage; set { _showImageStep_ShowRawImage = value; OnChange(); } }

        private bool _showImageStep_ShowProcessedImage = false;
        public bool ShowImageStep_ShowProcessedImage { get => _showImageStep_ShowProcessedImage; set { _showImageStep_ShowProcessedImage = value; OnChange(); } }

        // ===== VideoCreation Felder =====
        private string _videoCreationStep_SavePath = string.Empty;
        public string VideoCreationStep_SavePath { get => _videoCreationStep_SavePath; set { _videoCreationStep_SavePath = value; OnChange(); } }

        private string _videoCreationStep_FileName = "output.mp4";
        public string VideoCreationStep_FileName { get => _videoCreationStep_FileName; set { _videoCreationStep_FileName = value; OnChange(); (ConfirmCommand as RelayCommand)?.RaiseCanExecuteChanged(); } }

        private bool _videoCreationStep_UseRawImage = true;
        public bool VideoCreationStep_UseRawImage { get => _videoCreationStep_UseRawImage; set { _videoCreationStep_UseRawImage = value; OnChange(); } }

        private bool _videoCreationStep_UseProcessedImage = true;
        public bool VideoCreationStep_UseProcessedImage { get => _videoCreationStep_UseProcessedImage; set { _videoCreationStep_UseProcessedImage = value; OnChange(); } }

        // ===== MakroExecution Felder =====
        public ObservableCollection<string> MakroNames => new ObservableCollection<string>(_ctx.AllMakros?.Keys?.OrderBy(n => n) ?? Enumerable.Empty<string>());
        private string? _makroExecutionStep_SelectedMakroName;
        public string? MakroExecutionStep_SelectedMakroName
        {
            get => _makroExecutionStep_SelectedMakroName;
            set { _makroExecutionStep_SelectedMakroName = value; OnChange(); (ConfirmCommand as RelayCommand)?.RaiseCanExecuteChanged(); }
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
                        DrawResults = TemplateMatchingStep_DrawResults
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
                        ShowProcessedImage = ShowImageStep_ShowProcessedImage
                    }
                },
                "VideoCreation" => new VideoCreationStep
                {
                    Settings = new VideoCreationSettings
                    {
                        SavePath = VideoCreationStep_SavePath,
                        FileName = VideoCreationStep_FileName,
                        UseRawImage = VideoCreationStep_UseRawImage,
                        UseProcessedImage = VideoCreationStep_UseProcessedImage
                    }
                },
                "MakroExecution" => new MakroExecutionStep
                {
                    Settings = new MakroExecutionSettings
                    {
                        MakroName = MakroExecutionStep_SelectedMakroName ?? MakroNames.FirstOrDefault(string.Empty)
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
                        TimeoutMs = KlickOnPointStep_TimeoutMs
                    }
                },
                _ => null
            };
        }
    }
}
