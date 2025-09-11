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

        private readonly IJobExecutor _ctx;
        private readonly Job? _currentJobBeingEdited;

        public AddJobStepDialogViewModel(IJobExecutor ctx, Job? currentJobBeingEdited = null)
        {
            _ctx = ctx;
            _currentJobBeingEdited = currentJobBeingEdited;
            ConfirmCommand = new RelayCommand(Confirm, CanConfirm);
            CancelCommand = new RelayCommand(() => RequestClose?.Invoke(false));
            BrowseTemplatePathCommand = new RelayCommand(BrowseTemplatePath);
            BrowseScriptPathCommand = new RelayCommand(BrowseScriptPath);
            BrowseVideoSavePathCommand = new RelayCommand(BrowseVideoSavePath);
            ChooseMonitorCommand = new RelayCommand(ChooseMonitor);
            CaptureTemplateMatchingRoiCommand = new RelayCommand(CaptureTemplateMatchingRoi);
            CaptureYoloDetectionRoiCommand = new RelayCommand(CaptureYoloDetectionRoi);
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
        public ICommand BrowseVideoSavePathCommand { get; }
        public ICommand ChooseMonitorCommand { get; }
        public ICommand CaptureTemplateMatchingRoiCommand { get; }
        public ICommand CaptureYoloDetectionRoiCommand { get; }

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
                "JobExecution" => !string.IsNullOrWhiteSpace(JobExecutionStep_SelectedJobName),
                "DesktopDuplication" => true,
                "ScriptExecution" => !string.IsNullOrWhiteSpace(ScriptExecutionStep_ScriptPath),
                "KlickOnPoint" => !string.IsNullOrWhiteSpace(KlickOnPointStep_ClickType) && KlickOnPointStep_TimeoutMs >= 0,
                "KlickOnPoint3D" => !string.IsNullOrWhiteSpace(KlickOnPoint3DStep_ClickType) && KlickOnPoint3DStep_Timeout >= 0 && KlickOnPoint3DStep_FOV > 0,
                "YoloDetection" => !string.IsNullOrWhiteSpace(YoloDetectionStep_Model) && !string.IsNullOrWhiteSpace(YoloDetectionStep_ClassName) && YoloDetectionStep_ConfidenceThreshold is >= 0 and <= 1,
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
            "JobExecution",
            "ScriptExecution",
            "KlickOnPoint",
            "KlickOnPoint3D",
            "YoloDetection"
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
                OnChange(nameof(ShowJobExecution));
                OnChange(nameof(ShowScriptExecution));
                OnChange(nameof(ShowKlickOnPoint));
                OnChange(nameof(ShowKlickOnPoint3D));
                OnChange(nameof(ShowYoloDetection));
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

        private int _klickOnPoint3DStep_Timeout = 5000;
        public int KlickOnPoint3DStep_Timeout { get => _klickOnPoint3DStep_Timeout; set { _klickOnPoint3DStep_Timeout = value; OnChange(); } }
        private bool _klickOnPoint3DStep_InvertMouseMovementY = false;
        public bool KlickOnPoint3DStep_InvertMouseMovementY { get => _klickOnPoint3DStep_InvertMouseMovementY; set { _klickOnPoint3DStep_InvertMouseMovementY = value; OnChange(); } }
        private bool _klickOnPoint3DStep_InvertMouseMovementX = false;
        public bool KlickOnPoint3DStep_InvertMouseMovementX { get => _klickOnPoint3DStep_InvertMouseMovementX; set { _klickOnPoint3DStep_InvertMouseMovementX = value; OnChange(); } }

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
                System.Windows.MessageBox.Show($"Fehler bei der Monitor-Auswahl: {ex.Message}", "Fehler", 
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
                System.Windows.MessageBox.Show($"Error capturing ROI: {ex.Message}", "Error", 
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
                System.Windows.MessageBox.Show($"Error capturing ROI: {ex.Message}", "Error", 
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

        private bool _showImageStep_ShowRawImage = true;
        public bool ShowImageStep_ShowRawImage 
        { 
            get => _showImageStep_ShowRawImage; 
            set 
            { 
                _showImageStep_ShowRawImage = value; 
                if (value) _showImageStep_ShowProcessedImage = false;
                OnChange(); 
                OnChange(nameof(ShowImageStep_ShowProcessedImage));
            } 
        }

        private bool _showImageStep_ShowProcessedImage = false;
        public bool ShowImageStep_ShowProcessedImage 
        { 
            get => _showImageStep_ShowProcessedImage; 
            set 
            { 
                _showImageStep_ShowProcessedImage = value; 
                if (value) _showImageStep_ShowRawImage = false;
                OnChange(); 
                OnChange(nameof(ShowImageStep_ShowRawImage));
            } 
        }

        // ===== VideoCreation Felder =====
        private string _videoCreationStep_SavePath = string.Empty;
        public string VideoCreationStep_SavePath { get => _videoCreationStep_SavePath; set { _videoCreationStep_SavePath = value; OnChange(); } }

        private string _videoCreationStep_FileName = "output.mp4";
        public string VideoCreationStep_FileName { get => _videoCreationStep_FileName; set { _videoCreationStep_FileName = value; OnChange(); (ConfirmCommand as RelayCommand)?.RaiseCanExecuteChanged(); } }

        private bool _videoCreationStep_UseRawImage = true;
        public bool VideoCreationStep_UseRawImage 
        { 
            get => _videoCreationStep_UseRawImage; 
            set 
            { 
                _videoCreationStep_UseRawImage = value; 
                if (value) _videoCreationStep_UseProcessedImage = false;
                OnChange(); 
                OnChange(nameof(VideoCreationStep_UseProcessedImage));
            } 
        }

        private bool _videoCreationStep_UseProcessedImage = false;
        public bool VideoCreationStep_UseProcessedImage 
        { 
            get => _videoCreationStep_UseProcessedImage; 
            set 
            { 
                _videoCreationStep_UseProcessedImage = value; 
                if (value) _videoCreationStep_UseRawImage = false;
                OnChange(); 
                OnChange(nameof(VideoCreationStep_UseRawImage));
            } 
        }

        // ===== JobExecution Felder =====
        public ObservableCollection<string> AvailableJobNames
        {
            get
            {
                var allJobs = _ctx.AllJobs?.Values?.Where(j => j != null) ?? Enumerable.Empty<Job>();
                var availableJobs = allJobs
                    .Where(j => !j.Repeating && 
                               j.Name != _currentJobBeingEdited?.Name)
                    .Select(j => j.Name)
                    .OrderBy(n => n);
                return new ObservableCollection<string>(availableJobs);
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
        public ObservableCollection<string> MakroNames => new ObservableCollection<string>(_ctx.AllMakros?.Keys?.OrderBy(n => n) ?? Enumerable.Empty<string>());
        
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
                        MakroName = MakroExecutionStep_SelectedMakroName ?? MakroNames.FirstOrDefault(string.Empty),
                        MakroId = MakroExecutionStep_SelectedMakroId ?? 
                                  (_ctx.AllMakros?.Values?.FirstOrDefault(m => m.Name == MakroExecutionStep_SelectedMakroName)?.Id)
                    }
                },
                "JobExecution" => new JobExecutionStep
                {
                    Settings = new JobExecutionStepSettings
                    {
                        JobName = JobExecutionStep_SelectedJobName ?? AvailableJobNames.FirstOrDefault(string.Empty),
                        JobId = JobExecutionStep_SelectedJobId ??
                                (_ctx.AllJobs?.Values?.FirstOrDefault(j => j.Name == JobExecutionStep_SelectedJobName)?.Id),
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
                        TimeoutMs = KlickOnPointStep_TimeoutMs
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
                        InvertMouseMovementX = KlickOnPoint3DStep_InvertMouseMovementX
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
                        ROI = new Rect(YoloDetectionStep_RoiX, YoloDetectionStep_RoiY, YoloDetectionStep_RoiW, YoloDetectionStep_RoiH)
                    }
                },
                _ => null
            };
        }
    }
}

