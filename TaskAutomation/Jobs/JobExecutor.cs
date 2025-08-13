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
using TaskAutomation.Persistence;
using static System.Reflection.Metadata.BlobBuilder;

namespace TaskAutomation.Jobs
{
    public class JobExecutor : IJobExecutor, IJobExecutionContext, IDisposable
    {
        private readonly ILogger<JobExecutor> _logger;
        private readonly IJsonRepository<Job> _jobRepository;
        private readonly IJsonRepository<Makro> _makroRepository;

        private ProcessDuplicatorResult _processDuplicationResult;
        private DesktopFrame _currentDesktopFrame;
        private IMakroExecutor _makroExecutor;
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
        private DxgiResources _dxgiResources { get; } = DxgiResources.Instance;
        private string _makroFolderPath = Path.Combine(AppContext.BaseDirectory, "Configs\\Makro");
        private string _jobFolderPath = Path.Combine(AppContext.BaseDirectory, "Configs\\Job");
        private readonly Dictionary<string, Job> _allJobs = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Makro> _allMakros = new(StringComparer.OrdinalIgnoreCase);
        public IReadOnlyDictionary<string, Job> AllJobs => _allJobs;
        public IReadOnlyDictionary<string, Makro> AllMakros => _allMakros;



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

        public IMakroExecutor MakroExecutor
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

        public JobExecutor(
            ILogger<JobExecutor> logger,
            IJsonRepository<Job> jobRepo,
            IJsonRepository<Makro> makroRepo,
            IMakroExecutor makroExecutor)
        {
            _logger = logger;
            _jobRepository = jobRepo;
            _makroRepository = makroRepo;
            _makroExecutor = makroExecutor;

            _ = ReloadJobsAsync();
            _ = ReloadMakrosAsync();

            _logger.LogInformation("JobExecutor initialisiert. Verfügbare Jobs: {JobCount}, Makros: {MakroCount}",
                AllJobs.Count, AllMakros.Count);
        }

        public async Task ReloadJobsAsync()
        {
            var snapshot = await _jobRepository.LoadAllAsync().ConfigureAwait(false);

            _allJobs.Clear();
            int added = 0;
            foreach (var j in snapshot)
            {
                if (j == null || string.IsNullOrWhiteSpace(j.Name))
                {
                    _logger.LogWarning("Job ohne gültigen Namen ignoriert.");
                    continue;
                }
                _allJobs[j.Name] = j;
                added++;
            }
            _logger.LogInformation("Jobs geladen: {Count}", added);
        }

        public async Task ReloadMakrosAsync()
        {
            var snapshot = await _makroRepository.LoadAllAsync().ConfigureAwait(false);

            _allMakros.Clear();
            int added = 0;
            foreach (var m in snapshot)
            {
                if (m == null || string.IsNullOrWhiteSpace(m.Name))
                {
                    _logger.LogWarning("Makro ohne gültigen Namen ignoriert.");
                    continue;
                }
                _allMakros[m.Name] = m;
                added++;
            }
            _logger.LogInformation("Makros geladen: {Count}", added);
        }


        public Task ExecuteJob(string jobName, CancellationToken ct = default)
            => ExecuteJobAsync(jobName, ct);

        private async Task ExecuteJobAsync(string jobName, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(jobName) || !AllJobs.TryGetValue(jobName, out var job))
            {
                _logger.LogError("Job '{JobName}' existiert nicht.", jobName);
                return;
            }
            await ExecuteJobAsync(job, ct).ConfigureAwait(false);
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
                    OutputDirectory = videoStep.Settings.SavePath,
                    FileName = videoStep.Settings.FileName
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

                foreach (JobStep step in job.Steps)
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
        private Task<bool> ExecuteStepAsync(object step, Job jobContext, CancellationToken ct)
        {
            if (_stepHandlers.TryGetValue(step.GetType(), out var handler))
                return handler.ExecuteAsync(step, jobContext, (IJobExecutionContext)this, ct);

            _logger.LogWarning("Unbekannter Step-Typ: {StepType}", step.GetType().Name);
            return Task.FromResult(true);
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