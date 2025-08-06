using System;
using System.IO;
using System.Collections.Generic;
using OpenCvSharp;
using ImageCapture.ProcessDuplication;
using ImageCapture.DesktopDuplication;
using ImageCapture.Video;
using System.Drawing;
using ImageDetection.Algorithms.TemplateMatching;
using OpenCvSharp.Extensions;
using ImageDetection;
using SharpDX;
using System.Text;
using Point = OpenCvSharp.Point;
using ImageHelperMethods;
using TaskAutomation.Makros;
using System.Linq;
using TaskAutomation.Steps;
using Microsoft.Extensions.Logging;
using Common.Logging;

namespace TaskAutomation.Jobs
{
    public class JobExecutor : IDisposable
    {
        private readonly ILogger<JobExecutor> _logger;

        private ProcessDuplicatorResult _processDuplicationResult;
        private DesktopFrame _currentDesktopFrame;
        private Bitmap _currentImage;
        private Mat _currentImageWithResult;
        private bool _disposed = false;
        private StreamVideoRecorder _videoRecorder;
        private ProcessDuplicator _processDuplicator;
        private DesktopDuplicator _desktopDuplicator;
        private TemplateMatching _templateMatcher;
        private TemplateMatchingResult _templateMatchingResult;
        private Mat _imageToProcess;
        private Point _currentOffset = new Point(0, 0);
        private int _currentDesktop = 0;
        private int _currentAdapter = 0;
        private Dictionary<string, Makro> _makros;
        private DxgiResources _dxgiResources { get; } = DxgiResources.Instance;
        private MakroExecutor _makroExecutor;
        private string _makroFolderPath = Path.Combine(AppContext.BaseDirectory, "MakroFiles");
        private string _jobFolderPath = Path.Combine(AppContext.BaseDirectory, "JobFiles");
        private Dictionary<string, Job> _allJobs = new Dictionary<string, Job>();
        private Dictionary<string, Makro> _allMakros = new Dictionary<string, Makro>();


        private readonly Dictionary<Type, IJobStepHandler> _stepHandlers = new()
        {
            { typeof(ProcessDuplicationStep), new ProcessDuplicationStepHandler() },
            { typeof(DesktopDuplicationStep), new DesktopDuplicationStepHandler() },
            { typeof(TemplateMatchingStep), new TemplateMatchingStepHandler() },
            { typeof(ShowImageStep), new ShowImageStepHandler() },
            { typeof(VideoCreationStep), new VideoCreationStepHandler() },
            { typeof(MakroExecutionStep), new MakroExecutionStepHandler() }
        };

        // Öffentliche Properties für den Zugriff von außen (z.B. Handler)
        public ProcessDuplicatorResult ProcessDuplicationResult
        {
            get => _processDuplicationResult;
            set => _processDuplicationResult = value;
        }

        public DesktopFrame CurrentDesktopFrame
        {
            get => _currentDesktopFrame;
            set => _currentDesktopFrame = value;
        }

        public Bitmap CurrentImage
        {
            get => _currentImage;
            set => _currentImage = value;
        }

        public Mat CurrentImageWithResult
        {
            get => _currentImageWithResult;
            set => _currentImageWithResult = value;
        }

        public StreamVideoRecorder VideoRecorder
        {
            get => _videoRecorder;
            set => _videoRecorder = value;
        }

        public ProcessDuplicator ProcessDuplicator
        {
            get => _processDuplicator;
            set => _processDuplicator = value;
        }

        public DesktopDuplicator DesktopDuplicator
        {
            get => _desktopDuplicator;
            set => _desktopDuplicator = value;
        }

        public TemplateMatching TemplateMatcher
        {
            get => _templateMatcher;
            set => _templateMatcher = value;
        }

        public TemplateMatchingResult TemplateMatchingResult
        {
            get => _templateMatchingResult;
            set => _templateMatchingResult = value;
        }

        public Mat ImageToProcess
        {
            get => _imageToProcess;
            set => _imageToProcess = value;
        }

        public Point CurrentOffset
        {
            get => _currentOffset;
            set => _currentOffset = value;
        }

        public int CurrentDesktop
        {
            get => _currentDesktop;
            set => _currentDesktop = value;
        }

        public int CurrentAdapter
        {
            get => _currentAdapter;
            set => _currentAdapter = value;
        }

        public DxgiResources DxgiResources => _dxgiResources;

        public MakroExecutor MakroExecutor
        {
            get => _makroExecutor;
            set => _makroExecutor = value;
        }

        public string MakroFolderPath
        {
            get => _makroFolderPath;
        }

        public string JobFolderPath
        {
            get => _jobFolderPath;
        }

        public Dictionary<string, Job> AllJobs
        {
            get => _allJobs;
        }

        public Dictionary<string, Makro> AllMakros
        {
            get => _allMakros;
        }

        public JobExecutor()
        {
            _logger = Log.Create<JobExecutor>();
            SetAllJobs();
            SetAllMakros();
            _logger.LogInformation("JobExecutor initialisiert. Verfügbare Jobs: {JobCount}, Verfügbare Makros: {MakroCount}", _allJobs.Count, _allMakros.Count);
        }

        private void SetAllJobs()
        {
            if (!Directory.Exists(JobFolderPath))
            {
                _logger.LogWarning("Das Job-Verzeichnis '{JobFolderPath}' existiert nicht.", JobFolderPath);
            }

            string[] files = Directory.GetFiles(JobFolderPath);
            if (files.Length == 0)
            {
                _logger.LogWarning("Keine Job-Dateien im Verzeichnis '{JobFolderPath}' gefunden.", JobFolderPath);
            }

            _allJobs = new Dictionary<string, Job>();

            foreach (string file in files)
            {
                try
                {
                    Job job = JobReader.ReadSteps(file);
                    if (_allJobs.ContainsKey(job.Name))
                    {
                        _logger.LogWarning("Ein Job mit dem Namen '{JobName}' existiert bereits. Der Job wird überschrieben.", job.Name);
                    }
                    _allJobs[job.Name] = job;
                }
                catch
                {
                    _logger.LogError("Fehler beim Laden der Job-Datei: {File}. Bitte überprüfen Sie die Datei auf Korrektheit.", file);
                }
            }
        }

        private void SetAllMakros()
        {
            if (!Directory.Exists(MakroFolderPath))
            {
                _logger.LogWarning("Das Makro-Verzeichnis '{MakroFolderPath}' existiert nicht.", MakroFolderPath);
            }

            string[] files = Directory.GetFiles(MakroFolderPath);
            if (files.Length == 0)
            {
                _logger.LogInformation("Keine Makro-Dateien im Verzeichnis '{MakroFolderPath}' gefunden.", MakroFolderPath);
            }

            foreach (string file in files)
            {
                try
                {
                    Makro makro = MakroReader.LadeMakroDatei(file);
                    _allMakros[makro.Name] = makro;
                }
                catch
                {
                    _logger.LogError("Fehler beim Laden der Makro-Datei: {File}. Bitte überprüfen Sie die Datei auf Korrektheit.", file);
                }
            }
        }

        public async void ExecuteJob(Job job)
        {
            if (job == null)
            {
                _logger.LogError("Job ist null. Bitte stellen Sie sicher, dass ein gültiger Job übergeben wird.");
                return;
            }

            _logger.LogInformation("Starte Job: {JobName}", job.Name);

            var videoStep = job.Steps.OfType<VideoCreationStep>().FirstOrDefault();
            if (videoStep != null)
            {
                _videoRecorder = new StreamVideoRecorder(1920, 1080, 60);
                _videoRecorder.OutputDirectory = videoStep.SavePath;
                _videoRecorder.FileName = videoStep.FileName;
                await _videoRecorder.StartAsync();
                _logger.LogInformation("VideoRecorder gestartet mit Pfad: {OutputDirectory}, Dateiname: {FileName}", _videoRecorder.OutputDirectory, _videoRecorder.FileName);
            }

            if (job.Steps.OfType<MakroExecutionStep>().Any())
            {
                _makroExecutor = new MakroExecutor();

                _makros = new Dictionary<string, Makro>();
                foreach (var step in job.Steps.OfType<MakroExecutionStep>())
                {
                    string makroFilePath = Path.Combine(_makroFolderPath, step.MakroName + ".json");
                    _makros[step.MakroName] = MakroReader.LadeMakroDatei(makroFilePath);
                }
            }

            bool continueJob = true;
            do
            {
                _processDuplicationResult?.Dispose(); // Aufräumen vom vorherigen Durchlauf (falls repeating)
                _processDuplicationResult = null;

                foreach (var step_object in job.Steps)
                {
                    try
                    {
                        if (!ExecuteStep(step_object, job)) // ExecuteStep gibt false zurück, falls der Job abgebrochen werden soll
                        {
                            continueJob = false;
                            _logger.LogWarning("Job '{JobName}' wurde durch Step '{StepType}' vorzeitig beendet.", job.Name, step_object.GetType().Name);
                            break; // Inneren Loop (Steps) verlassen
                        }
                    }
                    catch (NotImplementedException nie)
                    {
                        _logger.LogWarning("Schritt '{StepType}' ist nicht vollständig implementiert: {Message}", step_object.GetType().Name, nie.Message);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "FEHLER beim Ausführen von Step '{StepType}': {Message}", step_object.GetType().Name, ex.Message);
                        continueJob = false; 
                        break;
                    }
                }

                if (!continueJob) break; // Äußeren Loop (repeating) verlassen

                if (job.Repeating)
                {
                    if(Console.KeyAvailable)
                    {
                        job.Repeating = false; // Stoppt die Wiederholung
                    }
                }

            } while (job.Repeating && continueJob);

            if (job.Steps.OfType<VideoCreationStep>().Any())
            {
                _videoRecorder.StopAndSave();
                _logger.LogInformation("VideoRecorder gestoppt und gespeichert. Video-Datei: {OutputDirectory}", _videoRecorder.OutputDirectory);
            }

            // Finale Aufräumarbeiten für den Job
            _processDuplicationResult?.Dispose();
            _processDuplicationResult = null;

            _videoRecorder?.Dispose(); // VideoWriter schließen, falls er geöffnet war
            _videoRecorder = null;

            Cv2.DestroyAllWindows();
            _logger.LogInformation("Job '{JobName}' abgeschlossen.", job.Name);
        }

        /// <summary>
        /// Führt einen einzelnen Job-Schritt aus.
        /// </summary>
        /// <param name="step">Das Schrittobjekt.</param>
        /// <param name="jobContext">Der aktuelle Job-Kontext (für übergreifende Infos wie Repeating).</param>
        /// <returns>True, wenn der Job fortgesetzt werden soll, false bei Abbruch.</returns>
        private bool ExecuteStep(object step, Job jobContext)
        {
            if (step == null)
                return false;

            if (_stepHandlers.TryGetValue(step.GetType(), out var handler))
            {
                return handler.Execute(step, jobContext, this);
            }

            _logger.LogWarning("Unbekannter Step-Typ: {StepType}. Bitte implementieren Sie einen Handler für diesen Typ.", step.GetType().Name);
            return true;
        }

        public void SetMakroFilePath(string filePath)
        {
            if (Directory.Exists(filePath))
            {
                _makroFolderPath = filePath;
            }
            else
            {
                _logger.LogError("Makro-Dateipfad '{FilePath}' existiert nicht.", filePath);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _processDuplicationResult?.Dispose();
                    _currentImage?.Dispose();
                    _videoRecorder?.Dispose();
                    _currentDesktopFrame?.Dispose();
                    _currentImageWithResult?.Dispose();
                    _processDuplicator?.Dispose();
                    _desktopDuplicator?.Dispose();
                    _templateMatcher?.Dispose();
                    _templateMatchingResult?.Dispose();
                    _imageToProcess?.Dispose();
                }
                _disposed = true;
            }
        }
    }
}