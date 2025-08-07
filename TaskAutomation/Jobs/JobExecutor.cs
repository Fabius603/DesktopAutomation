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
    public class JobExecutor : IJobExecutor, IDisposable
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
        private string _makroFolderPath = Path.Combine(AppContext.BaseDirectory, "Configs\\Makro");
        private string _jobFolderPath = Path.Combine(AppContext.BaseDirectory, "Configs\\Job");
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
            CreateMacroExecutor();
            _logger.LogInformation("JobExecutor initialisiert. Verfügbare Jobs: {JobCount}, Verfügbare Makros: {MakroCount}", _allJobs.Count, _allMakros.Count);
        }

        private void SetAllJobs()
        {
            if (!Directory.Exists(JobFolderPath))
            {
                _logger.LogError("Das Job-Verzeichnis '{JobFolderPath}' existiert nicht.", JobFolderPath);
                return;
            }

            string[] files = Directory.GetFiles(JobFolderPath, "*.json");
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

            string[] files = Directory.GetFiles(MakroFolderPath, "*.json");
            if (files.Length == 0)
            {
                _logger.LogWarning("Keine Makro-Dateien im Verzeichnis '{MakroFolderPath}' gefunden.", MakroFolderPath);
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

        public void CreateMacroExecutor()
        {
            _makroExecutor = new MakroExecutor();
        }

        public Task ExecuteJob(string actionName, CancellationToken ct)
                => ExecuteJobAsync(actionName, ct);

        private async Task ExecuteJobAsync(string jobName, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(jobName) || !_allJobs.ContainsKey(jobName))
            {
                _logger.LogError("Job '{JobName}' existiert nicht.", jobName);
                return;
            }

            await ExecuteJobAsync(_allJobs[jobName], ct).ConfigureAwait(false);
        }

        private async Task ExecuteJobAsync(Job job, CancellationToken ct)
        {
            if (job == null)
            {
                _logger.LogError("Job ist null.");
                return;
            }

            _logger.LogInformation("Starte Job: {JobName}", job.Name);
            ct.ThrowIfCancellationRequested();

            var videoStep = job.Steps.OfType<VideoCreationStep>().FirstOrDefault();
            if (videoStep != null)
            {
                _videoRecorder = new StreamVideoRecorder(1920, 1080, 60)
                {
                    OutputDirectory = videoStep.SavePath,
                    FileName = videoStep.FileName
                };
                await _videoRecorder.StartAsync(ct).ConfigureAwait(false);
                _logger.LogInformation("VideoRecorder gestartet …");
            }

            // Makros initialisieren …

            bool continueJob = true;
            do
            {
                _processDuplicationResult?.Dispose();
                _processDuplicationResult = null;

                foreach (var step in job.Steps)
                {
                    ct.ThrowIfCancellationRequested();

                    try
                    {
                        // Wenn ExecuteStep asynchron wäre:
                        // continueJob = await ExecuteStepAsync(step, job, ct);
                        // Sonst synchron aufrufen:
                        if (!await ExecuteStepAsync(step, job, ct).ConfigureAwait(false))
                        {
                            continueJob = false;
                            _logger.LogWarning("Job '{JobName}' vorzeitig beendet durch Step '{StepType}'.",
                                job.Name, step.GetType().Name);
                            break;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogInformation("Schritt '{StepType}' abgebrochen.", step.GetType().Name);
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Fehler in Schritt '{StepType}': {Message}",
                            step.GetType().Name, ex.Message);
                        continueJob = false;
                        break;
                    }
                }

                if (!continueJob) break;

                if (job.Repeating && Console.KeyAvailable)
                {
                    job.Repeating = false;
                    _logger.LogInformation("Job-Wiederholung gestoppt per Tastendruck.");
                }
            }
            while (job.Repeating && continueJob);

            if (videoStep != null)
            {
                _videoRecorder.StopAndSave();
                _logger.LogInformation("VideoRecorder gestoppt und gespeichert.");
            }

            // Aufräumen
            _processDuplicationResult?.Dispose();
            _videoRecorder?.Dispose();
            Cv2.DestroyAllWindows();

            _logger.LogInformation("Job '{JobName}' abgeschlossen.", job.Name);
        }

        /// <summary>
        /// Führt einen einzelnen Job-Schritt aus.
        /// </summary>
        /// <param name="step">Das Schrittobjekt.</param>
        /// <param name="jobContext">Der aktuelle Job-Kontext (für übergreifende Infos wie Repeating).</param>
        /// <returns>True, wenn der Job fortgesetzt werden soll, false bei Abbruch.</returns>
        private async Task<bool> ExecuteStepAsync(object step, Job jobContext, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (step == null)
                return false;

            if (_stepHandlers.TryGetValue(step.GetType(), out var handler))
            {
                return await handler.ExecuteAsync(step, jobContext, this, ct);
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