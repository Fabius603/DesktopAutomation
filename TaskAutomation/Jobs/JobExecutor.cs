using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using ImageCapture.ProcessDuplication;
using ImageCapture.DesktopDuplication;
using ImageCapture.Video;
using System.Drawing;
using ImageDetection.Algorithms.TemplateMatching;
using ImageDetection;
using System.Text;
using System.Diagnostics;
using ImageHelperMethods;
using TaskAutomation.Makros;
using System.Linq;
using TaskAutomation.Steps;
using Microsoft.Extensions.Logging;
using ImageCapture.DesktopDuplication.RecordingIndicator;
using TaskAutomation.Scripts;
using TaskAutomation.Orchestration;
using Common.JsonRepository;
using ImageDetection.YOLO;
using TaskAutomation.Events;
using TaskAutomation.Logging;
using TaskAutomation.Timing;

namespace TaskAutomation.Jobs
{
    public class JobExecutor : IJobExecutor, IDisposable
    {
        private readonly ILogger<JobExecutor> _logger;
        private readonly IJsonRepository<Job> _jobRepository;
        private readonly IJsonRepository<Makro> _makroRepository;
        private readonly IRecordingIndicatorOverlay _recordingOverlay;
        private readonly IImageDisplayService _imageDisplayService;
        private readonly IDesktopResultOverlay _desktopResultOverlay;
        private readonly IMakroExecutor _makroExecutor;
        private readonly IScriptExecutor _scriptExecutor;
        private readonly IYoloManager _yoloManager;
        private readonly IDesktopCaptureService _desktopCaptureService;
        private readonly IExecutionLogService _executionLogService;
        private bool _disposed = false;

        private readonly DxgiResources _dxgiResources = DxgiResources.Instance;
        private readonly Dictionary<string, Job>   _allJobs   = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Makro> _allMakros = new(StringComparer.OrdinalIgnoreCase);
        private Job? _currentJob;

        // ── Events ─────────────────────────────────────────────────────────────
        public event EventHandler<JobErrorEventArgs>?     JobErrorOccurred;
        public event EventHandler<JobStepErrorEventArgs>? JobStepErrorOccurred;

        private readonly Lazy<IJobLauncher> _lazyLauncher;

        // ── Zyklus-Erkennung ───────────────────────────────────────────────────
        private static readonly AsyncLocal<ImmutableHashSet<Guid>> _executionChain = new();

        // ── Pipeline-Validierungs-Cache (pro Job-ID, wird bei Reload gelöscht) ──

        // ── Handler-Registry ───────────────────────────────────────────────────
        private readonly Dictionary<Type, IJobStepHandler> _stepHandlers = new()
        {
            { typeof(ProcessDuplicationStep),  new ProcessDuplicationStepHandler()  },
            { typeof(DesktopDuplicationStep),  new DesktopDuplicationStepHandler()  },
            { typeof(TemplateMatchingStep),    new TemplateMatchingStepHandler()    },
            { typeof(ColorDetectionStep),      new ColorDetectionStepHandler()      },
            { typeof(PredictMovementStep),     new PredictMovementStepHandler()     },
            { typeof(ShowImageStep),           new ShowImageStepHandler()           },
            { typeof(ShowOnDesktopStep),        new ShowOnDesktopStepHandler()       },
            { typeof(VideoCreationStep),       new VideoCreationStepHandler()       },
            { typeof(MakroExecutionStep),      new MakroExecutionStepHandler()      },
            { typeof(ScriptExecutionStep),     new ScriptExecutionStepHandler()     },
            { typeof(KlickOnPointStep),        new KlickOnPointStepHandler()        },
            { typeof(KlickOnPoint3DStep),      new KlickOnPoint3DStepHandler()      },
            { typeof(JobExecutionStep),        new JobExecutionStepHandler()        },
            { typeof(YOLODetectionStep),       new YOLOStepHandler()                },
            { typeof(ActiveProcessStep),       new ActiveProcessStepHandler()       },
            { typeof(StartProcessStep),        new StartProcessStepHandler()        },
            { typeof(FocusProcessStep),         new FocusProcessStepHandler()        },
            { typeof(ShowTextStep),             new ShowTextStepHandler()            },
            { typeof(ActiveWindowStep),        new ActiveWindowStepHandler()        },
            { typeof(KeyPointMatchingStep),    new KeyPointMatchingStepHandler()    },
            { typeof(PointComparisonStep),     new PointComparisonStepHandler()     },
            { typeof(DynamicRoiStep),          new DynamicRoiStepHandler()          },
        };

        // ── IJobExecutor ───────────────────────────────────────────────────────
        public IReadOnlyDictionary<string, Job>   AllJobs   => _allJobs;
        public IReadOnlyDictionary<string, Makro> AllMakros => _allMakros;
        public IYoloManager   YoloManager   => _yoloManager;
        public IMakroExecutor MakroExecutor => _makroExecutor;

        public Job? CurrentJob
        {
            get => _currentJob;
            private set => _currentJob = value;
        }

        public JobExecutor(
            ILogger<JobExecutor> logger,
            IJsonRepository<Job> jobRepo,
            IJsonRepository<Makro> makroRepo,
            IMakroExecutor makroExecutor,
            IScriptExecutor scriptExecutor,
            IRecordingIndicatorOverlay recordingOverlay,
            IYoloManager yoloManager,
            IImageDisplayService imageDisplayService,
            IDesktopResultOverlay desktopResultOverlay,
            IDesktopCaptureService desktopCaptureService,
            IExecutionLogService executionLogService,
            IPreciseDelayService preciseDelayService,
            Lazy<IJobLauncher>? lazyLauncher = null)
        {
            _logger               = logger;
            _jobRepository        = jobRepo;
            _makroRepository      = makroRepo;
            _makroExecutor        = makroExecutor;
            _recordingOverlay     = recordingOverlay;
            _scriptExecutor       = scriptExecutor;
            _yoloManager          = yoloManager;
            _imageDisplayService  = imageDisplayService;
            _desktopResultOverlay = desktopResultOverlay;
            _desktopCaptureService = desktopCaptureService;
            _executionLogService = executionLogService;
            _lazyLauncher = lazyLauncher ?? new Lazy<IJobLauncher>(() => null!);
            _stepHandlers[typeof(TimeoutStep)] = new TimeoutStepHandler(preciseDelayService);

            _logger.LogInformation(
                "JobExecutor initialisiert. Jobs: {Jobs}, Makros: {Makros}",
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
                _allJobs[j.Id.ToString()] = j;
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
                _allMakros[m.Id.ToString()] = m;
                added++;
            }
            _logger.LogInformation("Makros geladen: {Count}", added);
        }


        public Task ExecuteJob(string jobName, CancellationToken ct = default)
            => ExecuteJobAsync(jobName, ct);

        public Task ExecuteJob(Guid jobId, CancellationToken ct = default)
            => ExecuteJob(jobId, JobStartContext.Unknown, ct);

        public Task ExecuteJob(Guid jobId, JobStartContext startContext, CancellationToken ct = default)
        {
            var job = AllJobs.Values.FirstOrDefault(j => j.Id == jobId);
            if (job == null)
            {
                var errorMessage = $"Job mit ID '{jobId}' existiert nicht.";
                _logger.LogError(errorMessage);
                JobErrorOccurred?.Invoke(this, new JobErrorEventArgs(jobId.ToString(), new ArgumentException(errorMessage)));
                return Task.CompletedTask;
            }
            return ExecuteJobAsync(job, startContext, ct);
        }

        private async Task ExecuteJobAsync(string jobName, CancellationToken ct)
        {
            var job = AllJobs.Values.FirstOrDefault(j => string.Equals(j.Name, jobName, StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrWhiteSpace(jobName) || job == null)
            {
                var errorMessage = $"Job '{jobName}' existiert nicht.";
                _logger.LogError(errorMessage);
                
                // Event für allgemeine Job-Fehler auslösen
                JobErrorOccurred?.Invoke(this, new JobErrorEventArgs(jobName ?? "Unknown", 
                    new ArgumentException(errorMessage)));
                return;
            }
            await ExecuteJobAsync(job, JobStartContext.Unknown, ct).ConfigureAwait(false);
        }

        private async Task ExecuteJobAsync(Job job, JobStartContext startContext, CancellationToken ct)
        {
            if (job == null)
            {
                var err = "Job ist null.";
                _logger.LogError(err);
                JobErrorOccurred?.Invoke(this, new JobErrorEventArgs("Unknown",
                    new ArgumentNullException(nameof(job), err)));
                return;
            }

            if (job.ActiveStepCount == 0)
            {
                var err = $"Job '{job.Name}' kann nicht ausgeführt werden, weil er keine aktiven Steps hat.";
                _logger.LogWarning(err);
                JobErrorOccurred?.Invoke(this, new JobErrorEventArgs(job.Name, new InvalidOperationException(err)));
                return;
            }

            CurrentJob = job;
            var jobRunStopwatch = Stopwatch.StartNew();
            var executionLog = _executionLogService.BeginJob(job.Id, job.Name, startContext);
            bool jobCompletedSuccessfully = false;
            bool jobWasCancelled = false;
            _logger.LogInformation("Starte Job: {JobName}", job.Name);
            _executionLogService.Write(
                executionLog,
                ExecutionLogLevel.Debug,
                "Job vorbereitet.",
                $"Steps={job.ActiveStepCount}, Repeating={job.Repeating}");

            // ── Zyklus-Erkennung ──────────────────────────────────────────────
            var parentChain = _executionChain.Value ?? ImmutableHashSet<Guid>.Empty;
            if (parentChain.Contains(job.Id))
            {
                var chain = string.Join(" → ", parentChain.Select(id =>
                    _allJobs.Values.FirstOrDefault(j => j.Id == id)?.Name ?? id.ToString()));
                var err = $"Zirkuläre Abhängigkeit erkannt: Job '{job.Name}' ist bereits in [{chain}].";
                _logger.LogError(err);
                _executionLogService.Write(executionLog, ExecutionLogLevel.Error, "Job vor Ausführung abgebrochen.", err);
                _executionLogService.Complete(executionLog, false, err);
                JobErrorOccurred?.Invoke(this, new JobErrorEventArgs(job.Name, new InvalidOperationException(err)));
                await UnloadYoloModelsAsync(job);
                _executionChain.Value = parentChain;
                CurrentJob = null;
                return;
            }
            _executionChain.Value = parentChain.Add(job.Id);

            // ── YOLO-Modelle vorladen ─────────────────────────────────────────
            try
            {
                await PreloadYoloModelsAsync(job, ct);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Job '{JobName}' vor Ausführung abgebrochen.", job.Name);
                _executionLogService.Write(executionLog, ExecutionLogLevel.Information, "Job vor Ausführung gestoppt.");
                _executionLogService.Complete(executionLog, false, "Gestoppt während YOLO-Preload.", cancelled: true);
                await UnloadYoloModelsAsync(job);
                _executionChain.Value = parentChain;
                CurrentJob = null;
                return;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Job '{JobName}' vor Ausführung fehlgeschlagen: {Message}", job.Name, ex.Message);
                _executionLogService.Write(executionLog, ExecutionLogLevel.Error, "Job vor Ausführung fehlgeschlagen.", ex.ToString());
                _executionLogService.Complete(executionLog, false, ex.Message);
                JobErrorOccurred?.Invoke(this, new JobErrorEventArgs(job.Name, ex));
                await UnloadYoloModelsAsync(job);
                _executionChain.Value = parentChain;
                CurrentJob = null;
                return;
            }

            VideoCreationStep? videoStep;
            DesktopDuplicationStep? desktopDuplicationStep;
            StepPipelineContext pipelineCtx;
            try
            {
                // ── Schritte analysieren ──────────────────────────────────────
                videoStep = job.Steps.OfType<VideoCreationStep>().FirstOrDefault(s => s.IsEnabled);
                desktopDuplicationStep = job.Steps.OfType<DesktopDuplicationStep>().FirstOrDefault(s => s.IsEnabled);

                // ── Pipeline-Kontext erstellen ────────────────────────────────
                var launcher = _lazyLauncher.Value;
                pipelineCtx = new StepPipelineContext(
                    _logger,
                    _dxgiResources,
                    _allJobs,
                    _allMakros,
                    _makroExecutor,
                    _scriptExecutor,
                    _yoloManager,
                    _imageDisplayService,
                    _desktopResultOverlay,
                    job,
                    ExecuteJob,
                    _desktopCaptureService,
                    executionLog,
                    launcher == null ? null : id => launcher.StartJob(id, new JobStartContext(JobStartSource.Job, job.Name, job.Id)),
                    launcher == null ? (Action<Guid>?)null : launcher.CancelJob,
                    launcher == null ? null : (id, token) => launcher.StartJobAsync(id, token, new JobStartContext(JobStartSource.Job, job.Name, job.Id)));
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation("Job '{JobName}' vor der Step-Ausführung gestoppt.", job.Name);
                _executionLogService.Write(
                    executionLog,
                    ExecutionLogLevel.Information,
                    "Job vor der Step-Ausführung gestoppt.");
                _executionLogService.Complete(executionLog, false, ex.Message, cancelled: true);
                await UnloadYoloModelsAsync(job);
                _executionChain.Value = parentChain;
                CurrentJob = null;
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Job '{JobName}' konnte den Pipeline-Kontext nicht initialisieren.", job.Name);
                _executionLogService.Write(
                    executionLog,
                    ExecutionLogLevel.Error,
                    "Job vor der Step-Ausführung fehlgeschlagen.",
                    ex.ToString());
                _executionLogService.Complete(executionLog, false, ex.Message);
                JobErrorOccurred?.Invoke(this, new JobErrorEventArgs(job.Name, ex));
                await UnloadYoloModelsAsync(job);
                _executionChain.Value = parentChain;
                CurrentJob = null;
                return;
            }

            bool recorderStarted = false;

            try
            {
                ct.ThrowIfCancellationRequested();

                // ── VideoRecorder initialisieren ──────────────────────────────
                if (videoStep != null)
                {
                    try
                    {
                        int videoWidth  = 1920;
                        int videoHeight = 1080;

                        if (desktopDuplicationStep != null)
                        {
                            var sb = ScreenHelper.GetDesktopBounds(desktopDuplicationStep.Settings.DesktopIdx);
                            if (!sb.IsEmpty) { videoWidth = sb.Width; videoHeight = sb.Height; }
                        }

                        pipelineCtx.VideoRecorder = new StreamVideoRecorder(videoWidth, videoHeight, 60)
                        {
                            OutputDirectory = videoStep.Settings.SavePath,
                            FileName        = videoStep.Settings.FileName
                        };
                        await pipelineCtx.VideoRecorder.StartAsync(ct).ConfigureAwait(false);
                        recorderStarted = true;
                        _logger.LogInformation("VideoRecorder gestartet.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Fehler beim Starten des VideoRecorders: {Message}", ex.Message);
                        throw;
                    }
                }

                // ── Aufnahme-Overlay ──────────────────────────────────────────
                if (desktopDuplicationStep != null && !_recordingOverlay.IsRunning)
                {
                    try
                    {
                        StartRecordingOverlay(new RecordingIndicatorOptions
                        {
                            MonitorIndex    = desktopDuplicationStep.Settings.DesktopIdx,
                            Color           = new GameOverlay.Drawing.Color(255, 64, 64, 220),
                            BorderThickness = 2f,
                            Mode            = RecordingIndicatorMode.RedBorder,
                            BadgeCorner     = Corner.TopRight,
                            Label           = "REC"
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Fehler beim Starten des Aufnahme-Overlays: {Message}", ex.Message);
                        throw;
                    }
                }

                // ── Ausführungsschleife ───────────────────────────────────────
                bool jobEndedByStep = false;
                int iteration = 0;
                do
                {
                    iteration++;
                    var iterationStopwatch = Stopwatch.StartNew();
                    pipelineCtx.ResetResults();
                    _executionLogService.Write(
                        executionLog,
                        ExecutionLogLevel.Debug,
                        $"Job-Runde {iteration} gestartet.");

                    var steps = job.Steps ?? Enumerable.Empty<JobStep>().ToList();
                    var branchStack = new Stack<BranchFrame>();
                    var conditionSources = BuildConditionSources(steps);

                    foreach (var step in steps)
                    {
                        ct.ThrowIfCancellationRequested();

                        bool parentActive = branchStack.Count == 0 || branchStack.Peek().CurrentActive;

                        // ── Control-flow steps: handle without executing ────────
                        if (step is IfStep ifStep)
                        {
                            var evaluation = parentActive
                                ? EvaluateCondition(ifStep.Settings, pipelineCtx.Results, conditionSources)
                                : new ConditionEvaluation(false, "Nicht ausgewertet: Der übergeordnete Bedingungszweig ist inaktiv.");
                            bool condMet = parentActive && evaluation.IsMatch;
                            _executionLogService.Write(executionLog, ExecutionLogLevel.Information,
                                condMet ? "IF-Zweig wird ausgeführt." : "IF-Zweig wird übersprungen.",
                                evaluation.Details,
                                stepId: step.Id, stepType: step.GetType().Name);
                            branchStack.Push(new BranchFrame(parentActive, condMet, condMet));
                            continue;
                        }

                        if (step is ElseIfStep elseIfStep)
                        {
                            if (branchStack.Count > 0)
                            {
                                var top = branchStack.Pop();
                                if (top.ParentActive && !top.AnyMatched)
                                {
                                    var evaluation = EvaluateCondition(elseIfStep.Settings, pipelineCtx.Results, conditionSources);
                                    bool condMet = evaluation.IsMatch;
                                    _executionLogService.Write(executionLog, ExecutionLogLevel.Information,
                                        condMet ? "ELSE-IF-Zweig wird ausgeführt." : "ELSE-IF-Zweig wird übersprungen.",
                                        evaluation.Details,
                                        stepId: step.Id, stepType: step.GetType().Name);
                                    branchStack.Push(new BranchFrame(top.ParentActive, condMet, condMet));
                                }
                                else
                                {
                                    // A branch already matched or parent inactive – skip this branch.
                                    var reason = !top.ParentActive
                                        ? "Nicht ausgewertet: Der übergeordnete Bedingungszweig ist inaktiv."
                                        : "Nicht ausgewertet: Ein vorheriger IF-/ELSE-IF-Zweig wurde bereits ausgeführt.";
                                    _executionLogService.Write(executionLog, ExecutionLogLevel.Information,
                                        "ELSE-IF-Zweig wird übersprungen.", reason,
                                        stepId: step.Id, stepType: step.GetType().Name);
                                    branchStack.Push(new BranchFrame(top.ParentActive, top.AnyMatched, false));
                                }
                            }
                            else
                            {
                                _executionLogService.Write(executionLog, ExecutionLogLevel.Warning,
                                    "ELSE-IF steht außerhalb eines IF-Blocks und wird übersprungen.",
                                    stepId: step.Id, stepType: step.GetType().Name);
                            }
                            continue;
                        }

                        if (step is ElseStep)
                        {
                            if (branchStack.Count > 0)
                            {
                                var top = branchStack.Pop();
                                bool executeElse = top.ParentActive && !top.AnyMatched;
                                var details = executeElse
                                    ? "Kein vorheriger IF-/ELSE-IF-Zweig wurde erfüllt."
                                    : !top.ParentActive
                                        ? "Der übergeordnete Bedingungszweig ist inaktiv."
                                        : "Ein vorheriger IF-/ELSE-IF-Zweig wurde bereits ausgeführt.";
                                _executionLogService.Write(executionLog, ExecutionLogLevel.Information,
                                    executeElse ? "ELSE-Zweig wird ausgeführt." : "ELSE-Zweig wird übersprungen.",
                                    details, stepId: step.Id, stepType: step.GetType().Name);
                                branchStack.Push(new BranchFrame(top.ParentActive, true, executeElse));
                            }
                            else
                            {
                                _executionLogService.Write(executionLog, ExecutionLogLevel.Warning,
                                    "ELSE steht außerhalb eines IF-Blocks und wird übersprungen.",
                                    stepId: step.Id, stepType: step.GetType().Name);
                            }
                            continue;
                        }

                        if (step is EndIfStep)
                        {
                            if (branchStack.Count > 0)
                                branchStack.Pop();
                            continue;
                        }

                        // ── Regular step: execute only when current branch is active ──
                        if (!parentActive)
                        {
                            _executionLogService.Write(executionLog, ExecutionLogLevel.Debug,
                                "Step wegen inaktivem Bedingungszweig übersprungen.",
                                stepId: step.Id, stepType: step.GetType().Name);
                            continue;
                        }

                        // ── Disabled step: skip without executing ─────────────────────
                        if (!step.IsEnabled)
                        {
                            _executionLogService.Write(
                                executionLog,
                                ExecutionLogLevel.Debug,
                                "Step übersprungen, weil er deaktiviert ist.",
                                stepId: step.Id,
                                stepType: step.GetType().Name);
                            continue;
                        }

                        // ── EndJob: immediately stop the job ──────────────────────────
                        if (step is EndJobStep)
                        {
                            _logger.LogInformation(
                                "Job '{JobName}' durch EndJob-Step beendet.", job.Name);
                            _executionLogService.Write(
                                executionLog,
                                ExecutionLogLevel.Information,
                                "Job durch EndJob-Step beendet.",
                                stepId: step.Id,
                                stepType: step.GetType().Name);
                            jobEndedByStep = true;
                            break;
                        }

                        try
                        {
                            await ExecuteStepAsync(step, pipelineCtx, job, ct).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException) { throw; }
                        catch (Exception ex)
                        {
                            _executionLogService.Write(
                                executionLog,
                                ExecutionLogLevel.Error,
                                "Step fehlgeschlagen.",
                                ex.ToString(),
                                step.Id,
                                step.GetType().Name);
                            JobStepErrorOccurred?.Invoke(this, new JobStepErrorEventArgs(job.Name, step.GetType().Name, ex));
                            // StepException verhindert, dass der äußere catch ein zweites Event feuert.
                            throw new StepException(ex);
                        }
                        _logger.LogDebug(
                            "Job '{JobName}' → Step '{StepType}' abgeschlossen.",
                            job.Name, step.GetType().Name);
                    }

                    iterationStopwatch.Stop();
                    _executionLogService.Write(
                        executionLog,
                        ExecutionLogLevel.Information,
                        $"Job-Runde {iteration} beendet.",
                        $"Durchgangsdauer={iterationStopwatch.ElapsedMilliseconds} ms",
                        durationMs: iterationStopwatch.ElapsedMilliseconds);

                    if (jobEndedByStep) break;
                }
                while (job.Repeating && !ct.IsCancellationRequested);
                jobCompletedSuccessfully = true;
            }
            catch (OperationCanceledException)
            {
                jobWasCancelled = true;
                _logger.LogInformation("Job '{JobName}' abgebrochen.", job.Name);
                _executionLogService.Write(executionLog, ExecutionLogLevel.Information, "Job gestoppt.");
            }
            catch (StepException)
            {
                // Fehler wurde bereits über JobStepErrorOccurred gemeldet – kein weiteres Event.
                _logger.LogDebug("Job '{JobName}' nach Step-Fehler gestoppt.", job.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler in Job '{JobName}': {Message}", job.Name, ex.Message);
                _executionLogService.Write(executionLog, ExecutionLogLevel.Error, "Job fehlgeschlagen.", ex.ToString());
                JobErrorOccurred?.Invoke(this, new JobErrorEventArgs(job.Name, ex));
            }
            finally
            {
                jobRunStopwatch.Stop();
                CurrentJob = null;

                // Fire-and-forget Sub-Jobs abbrechen wenn der Eltern-Job endet (egal ob Abbruch oder normales Ende).
                if (pipelineCtx.ChildJobInstanceIds.Count > 0)
                {
                    _logger.LogInformation(
                        "Job '{JobName}' beendet: beende {Count} Kind-Job-Instanz(en).",
                        job.Name, pipelineCtx.ChildJobInstanceIds.Count);
                    foreach (var childId in pipelineCtx.ChildJobInstanceIds)
                        try { pipelineCtx.CancelJobViaDispatcher?.Invoke(childId); } catch { /* best-effort */ }
                }

                // Nur die von diesem Job-Lauf geöffneten Bildvorschau-Fenster schließen.
                foreach (var winName in pipelineCtx.OpenedWindowNames)
                    try { _imageDisplayService.CloseWindow(winName); } catch { /* best-effort */ }

                // Desktop-Ergebnis-Overlay leeren (ShowOnDesktopStep).
                if (job.Steps.OfType<ShowOnDesktopStep>().Any())
                    try { _desktopResultOverlay.Clear(); } catch { /* best-effort */ }

                // Text-Overlay: bei Job-Ende aufräumen (ShowTextStep mit ClearOnJobEnd).
                try { _desktopResultOverlay.OnJobEnded(); } catch { /* best-effort */ }

                if (recorderStarted && pipelineCtx.VideoRecorder != null)
                {
                    try
                    {
                        await pipelineCtx.VideoRecorder.StopAndSave();
                        _logger.LogInformation("VideoRecorder gestoppt und gespeichert.");
                    }
                    catch (Exception ex) { _logger.LogError(ex, "Fehler beim Stoppen des VideoRecorders."); }
                }

                if (desktopDuplicationStep != null)
                {
                    try { StopRecordingOverlay(); }
                    catch (Exception ex) { _logger.LogError(ex, "Fehler beim StopRecordingOverlay."); }
                }

                try { pipelineCtx.VideoRecorder?.Dispose();   } catch { /* best-effort */ }
                try { pipelineCtx.KeyPointMatcher?.Dispose(); } catch { /* best-effort */ }
                try { pipelineCtx.Dispose(); }                catch { /* best-effort */ }

                await UnloadYoloModelsAsync(job);

                _logger.LogInformation("Job '{JobName}' beendet.", job.Name);
                _executionLogService.Complete(
                    executionLog,
                    jobCompletedSuccessfully,
                    $"Gesamtdauer={jobRunStopwatch.ElapsedMilliseconds} ms",
                    cancelled: jobWasCancelled);

                _executionChain.Value = parentChain;
            }
        }

        /// <summary>Führt einen einzelnen Step aus – loggt Fehler und rethrowt.</summary>
        private async Task ExecuteStepAsync(
            JobStep step, StepPipelineContext ctx, Job job, CancellationToken ct)
        {
            if (!_stepHandlers.TryGetValue(step.GetType(), out var handler))
            {
                _logger.LogWarning("Unbekannter Step-Typ: {StepType}", step.GetType().Name);
                return;
            }

            try
            {
                var stopwatch = Stopwatch.StartNew();
                if (ctx.ExecutionLogSession != null)
                {
                    _executionLogService.Write(
                        ctx.ExecutionLogSession,
                        ExecutionLogLevel.Debug,
                        "Step gestartet.",
                        stepId: step.Id,
                        stepType: step.GetType().Name);
                }

                await handler.ExecuteAsync(step, ctx, ct);

                stopwatch.Stop();
                if (ctx.ExecutionLogSession != null)
                {
                    var result = ctx.Results.GetRaw(step.Id);
                    _executionLogService.Write(
                        ctx.ExecutionLogSession,
                        ExecutionLogLevel.Information,
                        "Step abgeschlossen.",
                        BuildStepResultDetails(result),
                        step.Id,
                        step.GetType().Name,
                        stopwatch.ElapsedMilliseconds);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler in Step '{StepType}': {Message}", step.GetType().Name, ex.Message);
                throw;
            }
        }

        private static string? BuildStepResultDetails(object? result)
        {
            if (result == null) return null;

            var parts = new List<string>();
            foreach (var name in new[] { "WasExecuted", "Success", "Found", "Confidence", "Point", "AppliedRoi", "UsedDynamicRoi", "RoiUpdated", "RoiReset", "GlobalBounds", "ConsecutiveMisses", "FullSearchInterval", "IsPredicted", "PredictedForUtc", "ErrorMessage", "SourceCaptureIsFresh", "SourceCaptureTimestampUtc", "CaptureTimestampUtc" })
            {
                var property = result.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
                if (property == null) continue;

                var value = property.GetValue(result);
                if (value != null)
                    parts.Add($"{name}={value}");
            }

            return parts.Count == 0 ? result.GetType().Name : string.Join(", ", parts);
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

        /// <summary>
        /// Lädt alle YOLO-Modelle vor, die im Job verwendet werden, um bessere Performance zu erzielen.
        /// </summary>
        private async Task PreloadYoloModelsAsync(Job job, CancellationToken ct)
        {
            if (_yoloManager == null)
            {
                _logger.LogDebug("YoloManager nicht verfügbar - YOLO-Modell-Vorladen übersprungen");
                return;
            }

            var yoloSteps = job.Steps.OfType<YOLODetectionStep>().ToList();
            if (yoloSteps.Count == 0)
            {
                _logger.LogDebug("Keine YOLO-Steps im Job '{JobName}' - Vorladen übersprungen", job.Name);
                return;
            }

            var modelsToPreload = yoloSteps
                .Select(step => step.Settings.Model)
                .Where(model => !string.IsNullOrWhiteSpace(model))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (modelsToPreload.Count == 0)
            {
                _logger.LogWarning("YOLO-Steps gefunden, aber keine gültigen Modell-Namen in Job '{JobName}'", job.Name);
                return;
            }

            _logger.LogInformation("Lade {Count} YOLO-Modell(e) vor für Job '{JobName}': {Models}", 
                modelsToPreload.Count, job.Name, string.Join(", ", modelsToPreload));

            var preloadTasks = modelsToPreload.Select(async model =>
            {
                try
                {
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    await _yoloManager.EnsureModelAsync(model, ct);
                    stopwatch.Stop();
                    _logger.LogInformation("YOLO-Modell '{Model}' erfolgreich vorgeladen in {ElapsedMs}ms", 
                        model, stopwatch.ElapsedMilliseconds);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fehler beim Vorladen von YOLO-Modell '{Model}': {Message}", 
                        model, ex.Message);
                    throw; // Weiterwerfen, damit der Job nicht startet, wenn Modelle fehlen
                }
            });

            await Task.WhenAll(preloadTasks);
            _logger.LogInformation("Alle YOLO-Modelle für Job '{JobName}' erfolgreich vorgeladen", job.Name);
        }

        /// <summary>
        /// Entlädt alle YOLO-Modelle, die im Job verwendet wurden, um Speicher freizugeben.
        /// </summary>
        private async Task UnloadYoloModelsAsync(Job job)
        {
            if (_yoloManager == null)
            {
                return;
            }

            var yoloSteps = job.Steps.OfType<YOLODetectionStep>().ToList();
            if (yoloSteps.Count == 0)
            {
                return;
            }

            var modelsToUnload = yoloSteps
                .Select(step => step.Settings.Model)
                .Where(model => !string.IsNullOrWhiteSpace(model))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (modelsToUnload.Count == 0)
            {
                return;
            }

            _logger.LogInformation("Entlade {Count} YOLO-Modell(e) für Job '{JobName}': {Models}", 
                modelsToUnload.Count, job.Name, string.Join(", ", modelsToUnload));

            await Task.Run(() =>
            {
                foreach (var model in modelsToUnload)
                {
                    try
                    {
                        bool unloaded = _yoloManager.UnloadModel(model);
                        if (unloaded)
                        {
                            _logger.LogDebug("YOLO-Modell '{Model}' erfolgreich entladen", model);
                        }
                        else
                        {
                            _logger.LogDebug("YOLO-Modell '{Model}' war nicht geladen oder konnte nicht entladen werden", model);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Fehler beim Entladen von YOLO-Modell '{Model}': {Message}", 
                            model, ex.Message);
                        // Weiter mit den anderen Modellen
                    }
                }
            });

            _logger.LogInformation("YOLO-Modell-Cleanup für Job '{JobName}' abgeschlossen", job.Name);
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
                _disposed = true;
            }
        }

        /// <summary>
        /// Interner Marker: zeigt an, dass ein Step-Fehler bereits über
        /// <see cref="IJobExecutor.JobStepErrorOccurred"/> gemeldet wurde.
        /// Verhindert, dass der äußere Job-Catch ein zweites Event feuert.
        /// </summary>
        private sealed class StepException : Exception
        {
            public StepException(Exception inner) : base(inner.Message, inner) { }
        }

        // ── If/Else Branching ─────────────────────────────────────────────────

        /// <summary>
        /// Tracks execution state for one if/elseif/else block on the branch stack.
        /// </summary>
        private readonly struct BranchFrame
        {
            /// <summary>True when the enclosing block (parent) is currently executing.</summary>
            public readonly bool ParentActive;
            /// <summary>True when at least one branch in this block has already matched.</summary>
            public readonly bool AnyMatched;
            /// <summary>True when the current branch should be executed.</summary>
            public readonly bool CurrentActive;

            public BranchFrame(bool parentActive, bool anyMatched, bool currentActive)
            {
                ParentActive  = parentActive;
                AnyMatched    = anyMatched;
                CurrentActive = currentActive;
            }
        }

        private sealed record ConditionEvaluation(bool IsMatch, string Details);
        private sealed record SingleConditionEvaluation(bool IsMatch, string Details);
        private sealed record ConditionStepSource(string Label, string? ResultTypeName);

        private static ConditionEvaluation EvaluateCondition(
            IfConditionSettings settings,
            IJobResultStore results,
            IReadOnlyDictionary<string, ConditionStepSource> conditionSources)
        {
            if (settings.Conditions.Count == 0)
                return new ConditionEvaluation(false, "Keine Bedingungen konfiguriert.");

            var evaluations = settings.Conditions
                .Select((condition, index) => (Index: index + 1, Evaluation: EvaluateSingleCondition(condition, results, conditionSources)))
                .ToArray();
            var isMatch = settings.MatchMode == ConditionMatchMode.All
                ? evaluations.All(item => item.Evaluation.IsMatch)
                : evaluations.Any(item => item.Evaluation.IsMatch);
            var mode = settings.MatchMode == ConditionMatchMode.All ? "ALLE (AND)" : "MINDESTENS EINE (OR)";
            var lines = new List<string> { $"Verknüpfung: {mode}" };
            lines.AddRange(evaluations.Select(item => $"{item.Index}. {item.Evaluation.Details}"));
            lines.Add($"Gesamtergebnis: {(isMatch ? "ERFÜLLT" : "NICHT ERFÜLLT")}");
            return new ConditionEvaluation(isMatch, string.Join(Environment.NewLine, lines));
        }

        private static SingleConditionEvaluation EvaluateSingleCondition(
            StepCondition condition,
            IJobResultStore results,
            IReadOnlyDictionary<string, ConditionStepSource> conditionSources)
        {
            if (string.IsNullOrEmpty(condition.SourceStepId) || string.IsNullOrEmpty(condition.PropertyPath))
                return new SingleConditionEvaluation(false, "NICHT ERFÜLLT — Quell-Step oder Ergebniseigenschaft fehlt.");

            var leftName = FormatResultReference(condition.SourceStepId, condition.PropertyPath, conditionSources);
            if (!TryReadResultValue(results, condition.SourceStepId, condition.PropertyPath,
                    conditionSources, out var descriptor, out var value, out var leftWasExecuted))
                return new SingleConditionEvaluation(false, $"NICHT ERFÜLLT — {leftName}: Wert ist nicht verfügbar.");
            var leftStatus = leftWasExecuted ? string.Empty : " (Standardwert; Step wurde nicht ausgeführt)";

            if (condition.Operator == ConditionOperator.IsEmpty)
            {
                var isEmpty = value is null || string.IsNullOrEmpty(value as string);
                return BuildSingleEvaluation(isEmpty, leftName + leftStatus, descriptor, value, "ist leer", null);
            }
            if (condition.Operator == ConditionOperator.IsNotEmpty)
            {
                var isNotEmpty = value is not null && !string.IsNullOrEmpty(value as string);
                return BuildSingleEvaluation(isNotEmpty, leftName + leftStatus, descriptor, value, "ist nicht leer", null);
            }
            if (condition.Operator == ConditionOperator.IsTrue)
                return BuildSingleEvaluation(value is bool b && b, leftName + leftStatus, descriptor, value, "=", "Festwert true");
            if (condition.Operator == ConditionOperator.IsFalse)
                return BuildSingleEvaluation(value is bool b && !b, leftName + leftStatus, descriptor, value, "=", "Festwert false");
            if (value is null)
                return new SingleConditionEvaluation(false, $"NICHT ERFÜLLT — {leftName} [{descriptor.PropertyType}]: Wert ist <null>.");

            var comparison = condition.EffectiveComparison;
            object? expected;
            string expectedText;
            if (comparison.Kind == ComparisonOperandKind.Literal)
            {
                if (!StepResultMetadata.TryParseComparison(descriptor, comparison.Value, out expected))
                    return new SingleConditionEvaluation(false,
                        $"NICHT ERFÜLLT — {leftName}: Festwert '{comparison.Value}' ist für {descriptor.PropertyType} ungültig.");
                expectedText = $"Festwert {FormatLogValue(expected)}";
            }
            else
            {
                var rightName = FormatResultReference(comparison.SourceStepId, comparison.PropertyPath, conditionSources);
                if (!TryReadResultValue(results, comparison.SourceStepId, comparison.PropertyPath,
                        conditionSources, out var rightDescriptor, out expected, out var rightWasExecuted))
                    return new SingleConditionEvaluation(false, $"NICHT ERFÜLLT — {rightName}: Vergleichswert ist nicht verfügbar.");
                if (rightDescriptor.PropertyType != descriptor.PropertyType)
                    return new SingleConditionEvaluation(false,
                        $"NICHT ERFÜLLT — Datentypen stimmen nicht überein: {descriptor.PropertyType} und {rightDescriptor.PropertyType}.");
                var rightStatus = rightWasExecuted ? string.Empty : " (Standardwert; Step wurde nicht ausgeführt)";
                expectedText = $"{rightName}{rightStatus} [{rightDescriptor.PropertyType}] = {FormatLogValue(expected)}";
            }

            var isMatch = CompareForOperator(value, descriptor, condition.Operator, expected);
            return BuildSingleEvaluation(isMatch, leftName + leftStatus, descriptor, value,
                FormatConditionOperator(condition.Operator), expectedText);
        }

        private static SingleConditionEvaluation BuildSingleEvaluation(
            bool isMatch,
            string leftName,
            ResultPropertyDescriptor descriptor,
            object? actual,
            string conditionOperator,
            string? expected) =>
            new(isMatch,
                $"{(isMatch ? "ERFÜLLT" : "NICHT ERFÜLLT")} — {leftName} [{descriptor.PropertyType}] = {FormatLogValue(actual)} "
                + conditionOperator + (expected is null ? string.Empty : $" {expected}"));

        private static Dictionary<string, ConditionStepSource> BuildConditionSources(IReadOnlyList<JobStep> steps)
        {
            var sources = new Dictionary<string, ConditionStepSource>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < steps.Count; index++)
                if (!string.IsNullOrWhiteSpace(steps[index].Id))
                    sources[steps[index].Id] = new ConditionStepSource(
                        $"Step {index + 1} ({steps[index].GetType().Name})",
                        StepPipelineRegistry.Get(steps[index].GetType())?.Output);
            return sources;
        }

        private static string FormatResultReference(
            string? stepId,
            string? propertyPath,
            IReadOnlyDictionary<string, ConditionStepSource> conditionSources)
        {
            var step = !string.IsNullOrWhiteSpace(stepId) && conditionSources.TryGetValue(stepId, out var source)
                ? source.Label
                : string.IsNullOrWhiteSpace(stepId) ? "Unbekannter Step" : stepId;
            return $"{step} → {(string.IsNullOrWhiteSpace(propertyPath) ? "Unbekannte Eigenschaft" : propertyPath)}";
        }

        private static string FormatConditionOperator(ConditionOperator conditionOperator) => conditionOperator switch
        {
            ConditionOperator.Equals => "=",
            ConditionOperator.NotEquals => "!=",
            ConditionOperator.GreaterThan => ">",
            ConditionOperator.LessThan => "<",
            ConditionOperator.GreaterThanOrEqual => ">=",
            ConditionOperator.LessThanOrEqual => "<=",
            ConditionOperator.Contains => "enthält",
            ConditionOperator.StartsWith => "beginnt mit",
            _ => conditionOperator.ToString()
        };

        private static string FormatLogValue(object? value) => value switch
        {
            null => "<null>",
            string text => $"\"{text.Replace("\r", "\\r").Replace("\n", "\\n")}\"",
            DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
            bool boolean => boolean ? "true" : "false",
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? "<null>"
        };

        private static bool TryReadResultValue(
            IJobResultStore results,
            string? sourceStepId,
            string? propertyPath,
            IReadOnlyDictionary<string, ConditionStepSource> conditionSources,
            out ResultPropertyDescriptor descriptor,
            out object? value,
            out bool wasExecuted)
        {
            descriptor = null!;
            value = null;
            wasExecuted = false;
            if (string.IsNullOrWhiteSpace(sourceStepId) || string.IsNullOrWhiteSpace(propertyPath)) return false;
            var result = results.GetRaw(sourceStepId);
            if (result is null
                && conditionSources.TryGetValue(sourceStepId, out var source)
                && !string.IsNullOrWhiteSpace(source.ResultTypeName))
                result = StepResultMetadata.CreateDefaultResult(source.ResultTypeName);
            wasExecuted = result?.WasExecuted == true;
            return result is not null
                   && StepResultMetadata.TryGetProperty(result.GetType().Name, propertyPath, out descriptor)
                   && StepResultMetadata.TryReadValue(result, descriptor, out value);
        }

        private static bool CompareForOperator(object value, ResultPropertyDescriptor descriptor, ConditionOperator op, object? expected)
        {
            if (expected is null) return false;
            if (descriptor.PropertyType == ResultPropertyType.String)
            {
                var actualText = value.ToString() ?? ""; var expectedText = expected?.ToString() ?? "";
                if (op == ConditionOperator.Contains) return actualText.Contains(expectedText, StringComparison.OrdinalIgnoreCase);
                if (op == ConditionOperator.StartsWith) return actualText.StartsWith(expectedText, StringComparison.OrdinalIgnoreCase);
            }
            int cmp = descriptor.PropertyType switch
            {
                ResultPropertyType.Double => Convert.ToDouble(value).CompareTo(Convert.ToDouble(expected)),
                ResultPropertyType.Integer => Convert.ToInt64(value).CompareTo(Convert.ToInt64(expected)),
                ResultPropertyType.DateTime => Convert.ToDateTime(value).CompareTo((DateTime)expected!),
                ResultPropertyType.Bool => Convert.ToBoolean(value).CompareTo(Convert.ToBoolean(expected)),
                _ => string.Compare(value.ToString(), expected?.ToString(), StringComparison.OrdinalIgnoreCase)
            };
            return op switch
            {
                ConditionOperator.Equals             => cmp == 0,
                ConditionOperator.NotEquals          => cmp != 0,
                ConditionOperator.GreaterThan        => cmp > 0,
                ConditionOperator.LessThan           => cmp < 0,
                ConditionOperator.GreaterThanOrEqual => cmp >= 0,
                ConditionOperator.LessThanOrEqual    => cmp <= 0,
                _                                    => false
            };
        }
    }
}
