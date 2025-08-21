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
using ImageCapture.DesktopDuplication.RecordingIndicator;
using System.CodeDom.Compiler;

namespace TaskAutomation.Jobs
{
    public class JobExecutor : IJobExecutor, IJobExecutionContext, IDisposable
    {
        private readonly ILogger<JobExecutor> _logger;
        private readonly IJsonRepository<Job> _jobRepository;
        private readonly IJsonRepository<Makro> _makroRepository;
        private readonly IRecordingIndicatorOverlay _recordingOverlay;

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
        private DxgiResources _dxgiResources { get; } = DxgiResources.Instance;
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

        public DxgiResources DxgiResources => _dxgiResources;

        public IMakroExecutor MakroExecutor
        {
            get => _makroExecutor;
            set => _makroExecutor = value;
        }

        public JobExecutor(
            ILogger<JobExecutor> logger,
            IJsonRepository<Job> jobRepo,
            IJsonRepository<Makro> makroRepo,
            IMakroExecutor makroExecutor,
            IRecordingIndicatorOverlay recordingOverlay)
        {
            _logger = logger;
            _jobRepository = jobRepo;
            _makroRepository = makroRepo;
            _makroExecutor = makroExecutor;
            _recordingOverlay = recordingOverlay;

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

            // Schritte, die optional aktiv sind
            var videoStep = job.Steps.OfType<VideoCreationStep>().FirstOrDefault();
            var desktopDuplicationStep = job.Steps.OfType<DesktopDuplicationStep>().FirstOrDefault();

            bool recorderStarted = false;
            bool cancelled = false;

            try
            {
                ct.ThrowIfCancellationRequested();

                // Videoaufnahme vorbereiten/optional starten
                if (videoStep != null)
                {
                    _videoRecorder = new StreamVideoRecorder(1920, 1080, 60)
                    {
                        OutputDirectory = videoStep.Settings.SavePath,
                        FileName = videoStep.Settings.FileName
                    };

                    // Kann selbst abgebrochen werden -> Flag erst NACH erfolgreichem Start setzen
                    await _videoRecorder.StartAsync(ct).ConfigureAwait(false);
                    recorderStarted = true;
                    _logger.LogInformation("VideoRecorder gestartet …");
                }

                if (desktopDuplicationStep != null && !_recordingOverlay.IsRunning)
                {
                    StartRecordingOverlay(
                        options: new RecordingIndicatorOptions
                        {
                            MonitorIndex = desktopDuplicationStep.Settings.DesktopIdx,
                            Color = new GameOverlay.Drawing.Color(255, 64, 64, 220),
                            BorderThickness = 2f,
                            Mode = RecordingIndicatorMode.CornerBadge,
                            BadgeCorner = Corner.TopRight,
                            Label = "REC"
                        });
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
                            // Schritt ausführen; false => Job beenden
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
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
                // NICHT erneut throwen – das Aufräumen erfolgt im finally; Ausnahme kann ggf. außerhalb erneut behandelt werden.
                throw;
            }
            finally
            {
                // Stop/Speichern VideoRecorder nur, wenn tatsächlich gestartet
                if (recorderStarted && _videoRecorder != null)
                {
                    try
                    {
                        _videoRecorder.StopAndSave();
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
        private Task<bool> ExecuteStepAsync(object step, Job jobContext, CancellationToken ct)
        {
            if (_stepHandlers.TryGetValue(step.GetType(), out var handler))
                return handler.ExecuteAsync(step, jobContext, (IJobExecutionContext)this, ct);

            _logger.LogWarning("Unbekannter Step-Typ: {StepType}", step.GetType().Name);
            return Task.FromResult(true);
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