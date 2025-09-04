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
using ImageCapture.DesktopDuplication.RecordingIndicator;
using System.CodeDom.Compiler;
using TaskAutomation.Scripts;
using TaskAutomation.Orchestration;
using Common.JsonRepository;
using ImageDetection.Model;

namespace TaskAutomation.Jobs
{
    public class JobExecutor : IJobExecutor, IDisposable
    {
        private readonly ILogger<JobExecutor> _logger;
        private readonly IJsonRepository<Job> _jobRepository;
        private readonly IJsonRepository<Makro> _makroRepository;
        private readonly IRecordingIndicatorOverlay _recordingOverlay;

        // Event für allgemeine Job Fehler
        public event EventHandler<JobErrorEventArgs>? JobErrorOccurred;
        
        // Event für Job Step Fehler
        public event EventHandler<JobStepErrorEventArgs>? JobStepErrorOccurred;

        private ProcessDuplicatorResult _processDuplicationResult;
        private DesktopFrame _currentDesktopFrame;
        private IMakroExecutor _makroExecutor;
        private IScriptExecutor _scriptExecutor;
        private Bitmap _currentImage;
        private Mat _currentImageWithResult;
        private bool _disposed = false;
        private StreamVideoRecorder _videoRecorder;
        private ProcessDuplicator _processDuplicator;
        private DesktopDuplicator _desktopDuplicator;
        private TemplateMatching _templateMatcher;
        private IDetectionResult _detectionResult;
        private Mat _imageToProcess;
        private Point _currentOffset = new Point(0, 0);
        private DxgiResources _dxgiResources { get; } = DxgiResources.Instance;
        private readonly Dictionary<string, Job> _allJobs = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Makro> _allMakros = new(StringComparer.OrdinalIgnoreCase);
        private Job? _currentJob;
        
        public IReadOnlyDictionary<string, Job> AllJobs => _allJobs;
        public IReadOnlyDictionary<string, Makro> AllMakros => _allMakros;
        public Job? CurrentJob 
        {
            get => _currentJob;
            private set => _currentJob = value;
        }

        private Point? _latestCalculatedPoint;
        private readonly Dictionary<string, DateTime> _stepTimeouts = new(StringComparer.OrdinalIgnoreCase);


        private readonly Dictionary<Type, IJobStepHandler> _stepHandlers = new()
        {
            { typeof(ProcessDuplicationStep), new ProcessDuplicationStepHandler() },
            { typeof(DesktopDuplicationStep), new DesktopDuplicationStepHandler() },
            { typeof(TemplateMatchingStep), new TemplateMatchingStepHandler() },
            { typeof(ShowImageStep), new ShowImageStepHandler() },
            { typeof(VideoCreationStep), new VideoCreationStepHandler() },
            { typeof(MakroExecutionStep), new MakroExecutionStepHandler() },
            { typeof(ScriptExecutionStep), new ScriptExecutionStepHandler() },
            { typeof(KlickOnPointStep), new KlickOnPointStepHandler() },
            { typeof(JobExecutionStep), new JobExecutionStepHandler() },
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

        public IDetectionResult DetectionResult
        {
            get => _detectionResult;
            set => _detectionResult = value;
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

        public DxgiResources DxgiResources => _dxgiResources;

        public IMakroExecutor MakroExecutor
        {
            get => _makroExecutor;
            set => _makroExecutor = value;
        }
        public IScriptExecutor ScriptExecutor
        {
            get => _scriptExecutor;
            set => _scriptExecutor = value;
        }

        public Point? LatestCalculatedPoint
        {
            get => _latestCalculatedPoint;
            set => _latestCalculatedPoint = value;
        }

        public Dictionary<string, DateTime> StepTimeouts => _stepTimeouts;

        public ILogger Logger => _logger;

        public JobExecutor(
            ILogger<JobExecutor> logger,
            IJsonRepository<Job> jobRepo,
            IJsonRepository<Makro> makroRepo,
            IMakroExecutor makroExecutor,
            IScriptExecutor scriptExecutor,
            IRecordingIndicatorOverlay recordingOverlay)
        {
            _logger = logger;
            _jobRepository = jobRepo;
            _makroRepository = makroRepo;
            _makroExecutor = makroExecutor;
            _recordingOverlay = recordingOverlay;
            _scriptExecutor = scriptExecutor;


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

        public void StartRecordingOverlay(RecordingIndicatorOptions? options = null)
        {
            _recordingOverlay.Start(options);
        }

        public void StopRecordingOverlay()
        {
            _recordingOverlay.Stop();
            _recordingOverlay.Dispose();
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
                var errorMessage = $"Job '{jobName}' existiert nicht.";
                _logger.LogError(errorMessage);
                
                // Event für allgemeine Job-Fehler auslösen
                JobErrorOccurred?.Invoke(this, new JobErrorEventArgs(jobName ?? "Unknown", 
                    new ArgumentException(errorMessage)));
                return;
            }
            await ExecuteJobAsync(job, ct).ConfigureAwait(false);
        }

        private async Task ExecuteJobAsync(Job job, CancellationToken ct)
        {
            if (job == null)
            {
                var errorMessage = "Job ist null.";
                _logger.LogError(errorMessage);
                
                // Event für allgemeine Job-Fehler auslösen  
                JobErrorOccurred?.Invoke(this, new JobErrorEventArgs("Unknown", 
                    new ArgumentNullException(nameof(job), errorMessage)));
                return;
            }

            CurrentJob = job;
            _logger.LogInformation("Starte Job: {JobName}", job.Name);

            // Schritte, die optional aktiv sind
            var videoStep = job.Steps.OfType<VideoCreationStep>().FirstOrDefault();
            var desktopDuplicationStep = job.Steps.OfType<DesktopDuplicationStep>().FirstOrDefault();
            var showImageStep = job.Steps.OfType<ShowImageStep>().FirstOrDefault();

            bool recorderStarted = false;

            try
            {
                ct.ThrowIfCancellationRequested();

                // Videoaufnahme vorbereiten/optional starten
                if (videoStep != null)
                {
                    try
                    {
                        // Determine video resolution based on the desktop duplication step
                        int videoWidth = 1920;
                        int videoHeight = 1080;

                        if (desktopDuplicationStep != null)
                        {
                            var screenBounds = ImageHelperMethods.ScreenHelper.GetDesktopBounds(desktopDuplicationStep.Settings.DesktopIdx);
                            if (!screenBounds.IsEmpty)
                            {
                                videoWidth = screenBounds.Width;
                                videoHeight = screenBounds.Height;
                            }
                        }

                        _videoRecorder = new StreamVideoRecorder(videoWidth, videoHeight, 60)
                        {
                            OutputDirectory = videoStep.Settings.SavePath,
                            FileName = videoStep.Settings.FileName
                        };

                        // Kann selbst abgebrochen werden -> Flag erst NACH erfolgreichem Start setzen
                        await _videoRecorder.StartAsync(ct).ConfigureAwait(false);
                        recorderStarted = true;
                        _logger.LogInformation("VideoRecorder gestartet …");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Fehler beim Starten des VideoRecorders: {Message}", ex.Message);
                        throw;
                    }
                }

                if (desktopDuplicationStep != null && !_recordingOverlay.IsRunning)
                {
                    try
                    {
                        StartRecordingOverlay(
                            options: new RecordingIndicatorOptions
                            {
                                MonitorIndex = desktopDuplicationStep.Settings.DesktopIdx,
                                Color = new GameOverlay.Drawing.Color(255, 64, 64, 220),
                                BorderThickness = 2f,
                                Mode = RecordingIndicatorMode.RedBorder,
                                BadgeCorner = Corner.TopRight,
                                Label = "REC"
                            });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Fehler beim Starten des Aufnahme-Overlays: {Message}", ex.Message);
                        throw; // Job abbrechen
                    }
                }

                bool continueJob = true;
                do
                {
                    // Vor jeder Runde alte Prozess-Duplizierung freigeben
                    _processDuplicationResult?.Dispose();
                    _processDuplicationResult = null;

                    foreach (JobStep step in job.Steps)
                    {
                        ct.ThrowIfCancellationRequested();

                        try
                        {
                            bool success = await ExecuteStepAsync(step, job, ct).ConfigureAwait(false);
                            if (success)
                            {
                                _logger.LogDebug("Job '{JobName}' Schritt '{StepType}' erfolgreich.", job.Name, step.GetType().Name);
                            }
                            else
                            {
                                _logger.LogWarning("Job '{JobName}' Schritt '{StepType}' fehlgeschlagen - Job wird beendet.", job.Name, step.GetType().Name);
                                continueJob = false;
                                break; // Job bei Fehler sofort beenden
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            _logger.LogInformation("Job '{JobName}' abgebrochen.", job.Name);
                            continueJob = false;
                            break;
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
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Job '{JobName}' abgebrochen.", job.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unerwarteter Fehler in Job '{JobName}': {Message}", job.Name, ex.Message);
                
                // Event für allgemeine Job-Fehler auslösen
                JobErrorOccurred?.Invoke(this, new JobErrorEventArgs(job.Name, ex));
            }
            finally
            {
                CurrentJob = null;
                if (showImageStep != null)
                {
                    Cv2.DestroyAllWindows();
                }

                // Stop/Speichern VideoRecorder nur, wenn tatsächlich gestartet
                if (recorderStarted && _videoRecorder != null)
                {
                    try
                    {
                        await _videoRecorder.StopAndSave();
                        _logger.LogInformation("VideoRecorder gestoppt und gespeichert.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Fehler beim Stoppen/Speichern des VideoRecorders.");
                    }
                }

                // Desktop-Duplizierung/Overlay nur, wenn der Step existierte (und ggf. initialisiert wurde)
                if (desktopDuplicationStep != null)
                {
                    try
                    {
                        _desktopDuplicator?.Dispose();
                        _desktopDuplicator = null;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Fehler beim Dispose des DesktopDuplicators.");
                    }

                    try
                    {
                        StopRecordingOverlay();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Fehler beim StopRecordingOverlay.");
                    }
                }

                // Generelles Aufräumen (idempotent)
                try { _processDuplicationResult?.Dispose(); } catch (Exception ex) { _logger.LogError(ex, "Fehler beim Dispose von _processDuplicationResult."); }
                try { _videoRecorder?.Dispose(); } catch (Exception ex) { _logger.LogError(ex, "Fehler beim Dispose des VideoRecorders."); }
                try { Cv2.DestroyAllWindows(); } catch (Exception ex) { _logger.LogError(ex, "Fehler beim Schließen von OpenCV-Fenstern."); }

                // Abschluss-Log (Status abhängig von Abbruch)
                _logger.LogInformation("Job '{JobName}' {Status}.", job.Name, "beendet");
            }
        }

        /// <summary>
        /// Führt einen einzelnen Job-Schritt aus.
        /// </summary>
        /// <param name="step">Das Schrittobjekt.</param>
        /// <param name="jobContext">Der aktuelle Job-Kontext (für übergreifende Infos wie Repeating).</param>
        /// <returns>True, wenn der Job fortgesetzt werden soll, false bei Abbruch.</returns>
        private async Task<bool> ExecuteStepAsync(object step, Job jobContext, CancellationToken ct)
        {
            if (!_stepHandlers.TryGetValue(step.GetType(), out var handler))
            {
                _logger.LogWarning("Unbekannter Step-Typ: {StepType}", step.GetType().Name);
                return true;
            }

            try
            {
                return await handler.ExecuteAsync(step, jobContext, this, ct);
            }
            catch (Exception ex)
            {
                var stepTypeName = step.GetType().Name;
                _logger.LogError(ex, "Fehler in Step '{StepType}': {Message}", stepTypeName, ex.Message);
                
                // Event für UI-Fehlerbehandlung auslösen
                JobStepErrorOccurred?.Invoke(this, new JobStepErrorEventArgs(
                    jobContext.Name, 
                    stepTypeName, 
                    ex));
                
                return false; // Job stoppen bei Fehler
            }
        }

        /// <summary>
        /// Methode für Step-Handler um Fehler zu melden
        /// </summary>
        public void ReportStepError(string stepType, Exception exception)
        {
            var jobName = CurrentJob?.Name ?? "Unknown";
            _logger.LogError(exception, "Step-Fehler gemeldet: {StepType} in Job {JobName}: {Message}", 
                stepType, jobName, exception.Message);
            
            JobStepErrorOccurred?.Invoke(this, new JobStepErrorEventArgs(jobName, stepType, exception));
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
                    _imageToProcess?.Dispose();
                }
                _disposed = true;
            }
        }
    }
}
