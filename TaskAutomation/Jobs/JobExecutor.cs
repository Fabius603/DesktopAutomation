using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Collections.Concurrent;
using System.Collections.Immutable;
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
using TaskAutomation.Steps.Definitions;
using Microsoft.Extensions.Logging;
using ImageCapture.DesktopDuplication.RecordingIndicator;
using TaskAutomation.Scripts;
using TaskAutomation.Orchestration;
using Common.JsonRepository;
using ImageDetection.YOLO;
using TaskAutomation.Events;
using TaskAutomation.Logging;
using TaskAutomation.Timing;
using TaskAutomation.WindowsIntegration;
using TaskAutomation.Security;
using TaskAutomation.Jobs.ControlFlow;

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
        private readonly ICameraCaptureService _cameraCaptureService;
        private readonly IOcrService _ocrService;
        private readonly IExecutionLogService _executionLogService;
        private readonly ISecretStore? _secretStore;
        private bool _disposed = false;

        private readonly DxgiResources _dxgiResources = DxgiResources.Instance;
        private readonly Dictionary<string, Job> _allJobs = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Makro> _allMakros = new(StringComparer.OrdinalIgnoreCase);
        private Job? _currentJob;

        // ── Events ─────────────────────────────────────────────────────────────
        public event EventHandler<JobErrorEventArgs>? JobErrorOccurred;
        public event EventHandler<JobStepErrorEventArgs>? JobStepErrorOccurred;

        private readonly Lazy<IJobLauncher> _lazyLauncher;

        // ── Zyklus-Erkennung ───────────────────────────────────────────────────
        private static readonly AsyncLocal<ImmutableHashSet<Guid>> _executionChain = new();

        // ── Pipeline-Validierungs-Cache (pro Job-ID, wird bei Reload gelöscht) ──

        // ── Handler-Registry ───────────────────────────────────────────────────
        private readonly Dictionary<Type, IJobStepHandler> _stepHandlers = new()
        {
            { typeof(DesktopDuplicationStep),  new DesktopDuplicationStepHandler()  },
            { typeof(CameraCaptureStep),       new CameraCaptureStepHandler()       },
            { typeof(FileSystemOperationStep), new FileSystemOperationStepHandler() },
            { typeof(TemplateMatchingStep),    new TemplateMatchingStepHandler()    },
            { typeof(ColorDetectionStep),      new ColorDetectionStepHandler()      },
            { typeof(PredictMovementStep),     new PredictMovementStepHandler()     },
            { typeof(ShowImageStep),           new ShowImageStepHandler()           },
            { typeof(ShowOnDesktopStep),        new ShowOnDesktopStepHandler()       },
            { typeof(VideoCreationStep),       new VideoCreationStepHandler()       },
            { typeof(SaveImageStep),           new SaveImageStepHandler()           },
            { typeof(MakroExecutionStep),      new MakroExecutionStepHandler()      },
            { typeof(ScriptExecutionStep),     new ScriptExecutionStepHandler()     },
            { typeof(KlickOnPointStep),        new KlickOnPointStepHandler()        },
            { typeof(KlickOnPoint3DStep),      new KlickOnPoint3DStepHandler()      },
            { typeof(JobExecutionStep),        new JobExecutionStepHandler()        },
            { typeof(YOLODetectionStep),       new YOLOStepHandler()                },
            { typeof(ActiveProcessStep),       new ActiveProcessStepHandler()       },
            { typeof(GetProcessStep),          new GetProcessStepHandler()          },
            { typeof(StartProcessStep),        new StartProcessStepHandler()        },
            { typeof(TerminateProcessStep),    new TerminateProcessStepHandler()    },
            { typeof(FocusProcessStep),         new FocusProcessStepHandler()        },
            { typeof(ShowTextStep),             new ShowTextStepHandler()            },
            { typeof(ActiveWindowStep),        new ActiveWindowStepHandler()        },
            { typeof(KeyPointMatchingStep),    new KeyPointMatchingStepHandler()    },
            { typeof(PointComparisonStep),     new PointComparisonStepHandler()     },
            { typeof(DynamicRoiStep),          new DynamicRoiStepHandler()          },
            { typeof(BlockInputStep),          new BlockInputStepHandler()          },
            { typeof(UnblockInputStep),        new UnblockInputStepHandler()        },
        };

        // ── IJobExecutor ───────────────────────────────────────────────────────
        public IReadOnlyDictionary<string, Job> AllJobs => _allJobs;
        public IReadOnlyDictionary<string, Makro> AllMakros => _allMakros;
        public IYoloManager YoloManager => _yoloManager;
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
            ICameraCaptureService cameraCaptureService,
            IOcrService ocrService,
            IExecutionLogService executionLogService,
            IPreciseDelayService preciseDelayService,
            IWindowsSystemStateService windowsStateService,
            IUserChoiceService userChoiceService,
            Lazy<IJobLauncher>? lazyLauncher = null,
            IWindowsSystemSettingService? windowsSettingService = null,
            ISecretStore? secretStore = null)
        {
            _logger = logger;
            _jobRepository = jobRepo;
            _makroRepository = makroRepo;
            _makroExecutor = makroExecutor;
            _recordingOverlay = recordingOverlay;
            _scriptExecutor = scriptExecutor;
            _yoloManager = yoloManager;
            _imageDisplayService = imageDisplayService;
            _desktopResultOverlay = desktopResultOverlay;
            _desktopCaptureService = desktopCaptureService;
            _cameraCaptureService = cameraCaptureService;
            _ocrService = ocrService;
            _executionLogService = executionLogService;
            _secretStore = secretStore;
            _lazyLauncher = lazyLauncher ?? new Lazy<IJobLauncher>(() => null!);
            _stepHandlers[typeof(TimeoutStep)] = new TimeoutStepHandler(preciseDelayService);
            _stepHandlers[typeof(WindowsStateQueryStep)] = new WindowsStateQueryStepHandler(windowsStateService);
            _stepHandlers[typeof(UserChoiceStep)] = new UserChoiceStepHandler(userChoiceService);
            windowsSettingService ??= new WindowsSystemSettingService(
                new WindowsCapabilityCatalog(), new DefaultWindowsSettingProvider());
            _stepHandlers[typeof(WindowsSettingChangeStep)] = new WindowsSettingChangeStepHandler(windowsSettingService);
            _stepHandlers[typeof(OcrStep)] = new OcrStepHandler(_ocrService);

            _logger.LogInformation(
                "JobExecutor initialisiert. Jobs: {Jobs}, Makros: {Makros}",
                AllJobs.Count, AllMakros.Count);
        }

        public async Task ReloadJobsAsync()
        {
            var snapshot = await _jobRepository.LoadAllAsync().ConfigureAwait(false);
            foreach (var error in _jobRepository.LoadErrors)
                _logger.LogError("Job-Datei konnte nicht geladen werden: {FilePath}. {Message}",
                    error.FilePath, error.Message);

            _allJobs.Clear();
            int added = 0;
            foreach (var j in snapshot)
            {
                if (j == null || string.IsNullOrWhiteSpace(j.Name))
                {
                    _logger.LogWarning("Job ohne gültigen Namen ignoriert.");
                    continue;
                }
                JobVariableInputMigration.Migrate(j);
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
            foreach (var error in _makroRepository.LoadErrors)
                _logger.LogError("Makro-Datei konnte nicht geladen werden: {FilePath}. {Message}",
                    error.FilePath, error.Message);

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

        public async Task ExecuteJob(Guid jobId, JobStartContext startContext, CancellationToken ct = default)
        {
            using var cancellation = new JobExecutionCancellation(ct);
            await ExecuteJob(jobId, startContext, cancellation).ConfigureAwait(false);
        }

        public Task ExecuteJob(Guid jobId, JobStartContext startContext, JobExecutionCancellation cancellation)
            => ExecuteJob(jobId, startContext, cancellation, null);

        public Task ExecuteJob(Guid jobId, JobStartContext startContext, JobExecutionCancellation cancellation, JobDebugSession? debugSession)
        {
            ArgumentNullException.ThrowIfNull(cancellation);
            var job = AllJobs.Values.FirstOrDefault(j => j.Id == jobId);
            if (job == null)
            {
                var errorMessage = $"Job mit ID '{jobId}' existiert nicht.";
                _logger.LogError(errorMessage);
                JobErrorOccurred?.Invoke(this, new JobErrorEventArgs(jobId.ToString(), new ArgumentException(errorMessage)));
                return Task.CompletedTask;
            }
            return ExecuteJobAsync(job, startContext, cancellation, debugSession);
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
            using var cancellation = new JobExecutionCancellation(ct);
            await ExecuteJobAsync(job, JobStartContext.Unknown, cancellation).ConfigureAwait(false);
        }

        private async Task ExecuteJobAsync(Job job, JobStartContext startContext, JobExecutionCancellation cancellation, JobDebugSession? debugSession = null)
        {
            var ct = cancellation.ExecutionToken;
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
            _executionLogService.InitializeRun(executionLog, job, startContext.InstanceId ?? executionLog.Id);
            using var runLogScope = LogAmbient.Push(new(executionLog.Id, startContext.InstanceId ?? executionLog.Id,
                startContext.TriggerId, startContext.Source == JobStartSource.Automation ? startContext.SourceId : null,
                ParentRunId: startContext.ParentRunId));
            using var debugLog = new DebugLogSubscription(debugSession, executionLog, _executionLogService);
            cancellation.StateChanged += state => _executionLogService.Write(
                executionLog,
                state is JobExecutionState.ForceStopRequested or JobExecutionState.Failed
                    ? ExecutionLogLevel.Warning
                    : state is JobExecutionState.StopRequested or JobExecutionState.Cancelled or JobExecutionState.Completed
                        ? ExecutionLogLevel.Information
                        : ExecutionLogLevel.Debug,
                "Jobstatus geändert.",
                $"Status={state}",
                jobState: state);
            _executionLogService.Write(
                executionLog,
                ExecutionLogLevel.Information,
                "Jobstatus geändert.",
                $"Status={cancellation.State}",
                jobState: cancellation.State);
            bool jobCompletedSuccessfully = false;
            bool jobWasCancelled = false;
            bool runEndSteps = true;
            var completionReason = JobCompletionReason.Completed;
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
                cancellation.MarkCompleted(JobExecutionState.Failed);
                _executionLogService.Finish(executionLog, LogOutcome.Failed, "DependencyCycle");
                JobErrorOccurred?.Invoke(this, new JobErrorEventArgs(job.Name, new InvalidOperationException(err)));
                await UnloadYoloModelsAsync(job, []);
                _executionChain.Value = parentChain;
                CurrentJob = null;
                return;
            }
            _executionChain.Value = parentChain.Add(job.Id);

            VideoCreationStep? videoStep;
            DesktopDuplicationStep? desktopDuplicationStep;
            StepPipelineContext? pipelineCtx = null;
            try
            {
                var identityErrors = JobValueSources.ValidateIdentities(job);
                if (identityErrors.Count > 0) throw new InvalidOperationException(string.Join(", ", identityErrors));
                // ── Schritte analysieren ──────────────────────────────────────
                var allSteps = job.EnumerateAllSteps().ToList();
                videoStep = allSteps.OfType<VideoCreationStep>().FirstOrDefault(s => s.IsEnabled);
                desktopDuplicationStep = allSteps.OfType<DesktopDuplicationStep>().FirstOrDefault(s => s.IsEnabled);
                var secretValues = await LoadSecretValuesAsync(job, ct).ConfigureAwait(false);
                _executionLogService.RegisterSecrets(secretValues.Values.Select(value => value.Value));

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
                    _cameraCaptureService,
                    executionLog,
                    _executionLogService,
                    launcher == null ? null : id => launcher.StartJob(id, new JobStartContext(JobStartSource.Job, job.Name, job.Id, ParentRunId: executionLog.Id)),
                    launcher == null ? (Action<Guid>?)null : launcher.CancelJob,
                    launcher == null ? null : (id, token) => launcher.StartJobAsync(id, token, new JobStartContext(JobStartSource.Job, job.Name, job.Id, ParentRunId: executionLog.Id)),
                    secrets: secretValues);
                await PreloadYoloModelsAsync(job, pipelineCtx, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation("Job '{JobName}' vor der Step-Ausführung gestoppt.", job.Name);
                _executionLogService.Write(
                    executionLog,
                    ExecutionLogLevel.Information,
                    "Job vor der Step-Ausführung gestoppt.");
                cancellation.MarkCompleted(JobExecutionState.Cancelled);
                _executionLogService.Finish(executionLog, LogOutcome.Stopped, "CancelledBeforeSteps");
                await UnloadYoloModelsAsync(job, pipelineCtx?.LoadedYoloModels ?? []);
                pipelineCtx?.Dispose();
                _executionChain.Value = parentChain;
                CurrentJob = null;
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Job '{JobName}' konnte den Pipeline-Kontext nicht initialisieren.", job.Name);
                RecordFailure(executionLog, "Job vor der Step-Ausführung fehlgeschlagen.", ex);
                cancellation.MarkCompleted(JobExecutionState.Failed);
                _executionLogService.Finish(executionLog, LogOutcome.Failed, "InitializationFailed");
                JobErrorOccurred?.Invoke(this, new JobErrorEventArgs(job.Name, ex));
                await UnloadYoloModelsAsync(job, pipelineCtx?.LoadedYoloModels ?? []);
                pipelineCtx?.Dispose();
                _executionChain.Value = parentChain;
                CurrentJob = null;
                return;
            }

            try
            {
                ct.ThrowIfCancellationRequested();

                // ── Aufnahme-Overlay ──────────────────────────────────────────
                // Einmalige Startphase. Ein EndJob-Step beendet danach kontrolliert die Hauptphase.
                cancellation.EnterStartPhase();
                pipelineCtx.ResetResults();
                var startPhaseEndJob = await ExecuteStepSequenceAsync(
                    job.StartSteps,
                    "Startphase",
                    pipelineCtx,
                    job,
                    ct,
                    continueAfterStepError: false,
                    debugSession: debugSession).ConfigureAwait(false);
                bool jobEndedByStep = startPhaseEndJob != null;
                if (startPhaseEndJob != null)
                    runEndSteps = !startPhaseEndJob.Settings.SkipEndSteps;

                // ── Ausführungsschleife ───────────────────────────────────────
                cancellation.EnterRunPhase();
                int iteration = 0;
                var startStepIds = job.StartSteps.Select(step => step.Id).ToArray();
                while (!jobEndedByStep)
                {
                    iteration++;
                    var iterationStopwatch = Stopwatch.StartNew();
                    pipelineCtx.ResetResults(startStepIds);
                    debugSession?.SetIteration(
                        iteration,
                        startStepIds.Where(stepId => pipelineCtx.Results.GetRaw(stepId) != null));
                    _executionLogService.Write(
                        executionLog,
                        ExecutionLogLevel.Debug,
                        $"Job-Runde {iteration} gestartet.");

                    var steps = job.Steps ?? Enumerable.Empty<JobStep>().ToList();
                    var branchStack = new Stack<BranchFrame>();
                    var conditionSources = BuildConditionSources(job.StartSteps.Concat(steps).ToList());
                    bool continueJob = false;

                    foreach (var step in steps)
                    {
                        ct.ThrowIfCancellationRequested();

                        bool parentActive = branchStack.Count == 0 || branchStack.Peek().CurrentActive;

                        if (debugSession != null
                            && step.IsEnabled
                            && step is IControlFlowMarker)
                            await debugSession.BeforeStepAsync(step, "Hauptphase", ct, BuildStepStartDetails(step, "Hauptphase", iteration)).ConfigureAwait(false);

                        // ── Control-flow steps: handle without executing ────────
                        if (ProcessConditionControlFlow(
                                step, branchStack, pipelineCtx.Results, conditionSources, executionLog, "Main", iteration) is { } transition)
                        {
                            CompleteConditionControlFlow(debugSession, step, transition);
                            continue;
                        }

                        // ── Regular step: execute only when current branch is active ──
                        if (!parentActive)
                        {
                            debugSession?.MarkSkipped(step, "Inaktiver Bedingungszweig.");
                            StepLogScope.Skip(_executionLogService, executionLog, step, "Main", iteration, "InactiveBranch");
                            continue;
                        }

                        // ── Disabled step: skip without executing ─────────────────────
                        if (!step.IsEnabled)
                        {
                            StepLogScope.Skip(_executionLogService, executionLog, step, "Main", iteration, "Disabled");
                            continue;
                        }

                        if (debugSession != null)
                            await debugSession.BeforeStepAsync(step, "Hauptphase", ct, BuildStepStartDetails(step, "Hauptphase", iteration)).ConfigureAwait(false);

                        // ── EndJob: immediately stop the job ──────────────────────────
                        if (step is EndJobStep endJobStep)
                        {
                            var observed = ExecuteControlStep(endJobStep, executionLog, "Main", iteration,
                                () => (EndJobStep)StepInputMaterializer.Materialize(endJobStep, pipelineCtx.Results), "EndJob");
                            jobEndedByStep = true;
                            runEndSteps = !observed.Settings.SkipEndSteps;
                            debugSession?.MarkCompleted(step, "Job durch EndJob beendet.");
                            break;
                        }

                        if (step is ContinueJobStep)
                        {
                            ExecuteControlStep(step, executionLog, "Main", iteration, () => step, "NextIteration");
                            continueJob = true;
                            debugSession?.MarkCompleted(step, "Nächste Job-Runde angefordert.");
                            break;
                        }

                        try
                        {
                            await ExecuteStepAsync(
                                step, pipelineCtx, job, ct, "Durchlauf", iteration)
                                .ConfigureAwait(false);
                            var debugResult = pipelineCtx.Results.GetRaw(step.Id);
                            debugSession?.MarkCompleted(
                                step,
                                BuildStepResultDetails(step, debugResult, pipelineCtx.Results),
                                debugResult);
                        }
                        catch (OperationCanceledException) { throw; }
                        catch (Exception ex)
                        {
                            if (debugSession != null)
                                await debugSession.PauseAfterFailureAsync(step, ex, ct).ConfigureAwait(false);
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
                        ExecutionLogLevel.Debug,
                        $"Job-Runde {iteration} beendet.",
                        $"Durchgangsdauer={iterationStopwatch.ElapsedMilliseconds} ms",
                        durationMs: iterationStopwatch.ElapsedMilliseconds);

                    if (jobEndedByStep) break;
                    ct.ThrowIfCancellationRequested();
                    if (debugSession != null && (continueJob || job.Repeating))
                        await debugSession.PauseAfterIterationAsync(ct).ConfigureAwait(false);
                    if (continueJob) continue;
                    if (!job.Repeating) break;
                }
                completionReason = jobEndedByStep
                    ? JobCompletionReason.EndJobStep
                    : JobCompletionReason.Completed;
                jobCompletedSuccessfully = true;
            }
            catch (OperationCanceledException)
            {
                jobWasCancelled = true;
                completionReason = JobCompletionReason.Cancelled;
                _logger.LogInformation("Job '{JobName}' abgebrochen.", job.Name);
                _executionLogService.Write(executionLog, ExecutionLogLevel.Information, "Job gestoppt.");
            }
            catch (StepException)
            {
                completionReason = JobCompletionReason.StepFailed;
                // Fehler wurde bereits über JobStepErrorOccurred gemeldet – kein weiteres Event.
                _logger.LogDebug("Job '{JobName}' nach Step-Fehler gestoppt.", job.Name);
            }
            catch (Exception ex)
            {
                completionReason = JobCompletionReason.StepFailed;
                _logger.LogError(ex, "Fehler in Job '{JobName}'.", job.Name);
                RecordFailure(executionLog, "Job fehlgeschlagen.", ex);
                JobErrorOccurred?.Invoke(this, new JobErrorEventArgs(job.Name, ex));
            }
            finally
            {
                cancellation.BeginEndPhase();
                var endPhaseTimeout = TimeSpan.FromSeconds(Math.Clamp(
                    job.EndPhaseTimeoutSeconds,
                    Job.MinEndPhaseTimeoutSeconds,
                    Job.MaxEndPhaseTimeoutSeconds));
                if (!runEndSteps)
                {
                    foreach (var endStep in job.EndSteps)
                        StepLogScope.Skip(_executionLogService, executionLog, endStep, "End", null,
                            endStep.IsEnabled ? "EndPhaseSuppressed" : "Disabled");
                    _executionLogService.Write(
                        executionLog,
                        ExecutionLogLevel.Information,
                        "Konfigurierte End-Steps durch EndJob-Einstellung übersprungen.",
                        "Das interne Aufräumen wird weiterhin ausgeführt.");
                }
                else using (var endTimeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellation.EndPhaseToken))
                    {
                        if (debugSession == null)
                            endTimeoutCts.CancelAfter(endPhaseTimeout);
                        try
                        {
                            pipelineCtx.ResetResults(job.StartSteps.Concat(job.Steps).Select(step => step.Id));
                            await ExecuteStepSequenceAsync(
                                job.EndSteps,
                                "Endphase",
                                pipelineCtx,
                                job,
                                endTimeoutCts.Token,
                                continueAfterStepError: true,
                                precedingSteps: job.StartSteps.Concat(job.Steps).ToList(),
                                onStepError: () =>
                                {
                                    jobCompletedSuccessfully = false;
                                    completionReason = JobCompletionReason.StepFailed;
                                },
                                debugSession: debugSession).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            var cause = cancellation.EndPhaseToken.IsCancellationRequested
                                ? "durch ForceStop"
                                : $"nach {endPhaseTimeout.TotalSeconds:0} Sekunden wegen Zeitüberschreitung";
                            _logger.LogWarning("Endphase von Job '{JobName}' wurde {Cause} abgebrochen.", job.Name, cause);
                            _executionLogService.Write(executionLog, ExecutionLogLevel.Warning, "Endphase abgebrochen.", cause);
                        }
                        catch (Exception ex)
                        {
                            jobCompletedSuccessfully = false;
                            completionReason = JobCompletionReason.StepFailed;
                            _logger.LogError(ex, "Unerwarteter Fehler in der Endphase von Job '{JobName}'.", job.Name);
                            RecordFailure(executionLog, "Endphase fehlgeschlagen.", ex);
                        }
                    }

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

                // Nur Anzeigen dieses Jobs aufräumen; parallele Jobs bleiben sichtbar.
                var overlayStepKeys = job.EnumerateAllSteps()
                    .Where(step => step is ShowOnDesktopStep or ShowTextStep)
                    .Select(step => step.Id)
                    .ToArray();

                // Dauerhafte Texte bleiben erhalten, alle Erkennungsanzeigen dieses Jobs werden entfernt.
                try { _desktopResultOverlay.OnJobEnded(overlayStepKeys); } catch { /* best-effort */ }
                try { WindowsInputBlockController.Unblock(); } catch { /* best-effort */ }

                if (pipelineCtx.VideoRecorder != null)
                {
                    try
                    {
                        await pipelineCtx.VideoRecorder.StopAndSave();
                        _logger.LogInformation("VideoRecorder gestoppt und gespeichert.");
                        _executionLogService.Write(
                            executionLog,
                            ExecutionLogLevel.Information,
                            "Videoaufnahme gespeichert.",
                            $"Datei={pipelineCtx.VideoRecorder.OutputFilePath}, übergebene Frames={pipelineCtx.VideoRecorder.SubmittedFrameCount}",
                            stepId: videoStep?.Id,
                            stepType: videoStep?.GetType().Name);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Fehler beim Stoppen des VideoRecorders.");
                        RecordFailure(executionLog, "Videoaufnahme konnte nicht gespeichert werden.", ex, videoStep);
                    }
                }

                if (desktopDuplicationStep != null)
                {
                    try { StopRecordingOverlay(); }
                    catch (Exception ex) { _logger.LogError(ex, "Fehler beim StopRecordingOverlay."); }
                }

                try { pipelineCtx.VideoRecorder?.Dispose(); } catch { /* best-effort */ }
                try { pipelineCtx.KeyPointMatcher?.Dispose(); } catch { /* best-effort */ }
                try { pipelineCtx.Dispose(); } catch { /* best-effort */ }

                await UnloadYoloModelsAsync(job, pipelineCtx?.LoadedYoloModels ?? []);

                _logger.LogInformation("Job '{JobName}' beendet.", job.Name);
                var finalState = cancellation.IsForceStopRequested || jobWasCancelled
                    ? JobExecutionState.Cancelled
                    : completionReason == JobCompletionReason.StepFailed
                        ? JobExecutionState.Failed
                        : JobExecutionState.Completed;
                cancellation.MarkCompleted(finalState);
                debugSession?.Finish(finalState switch
                {
                    JobExecutionState.Completed => JobDebugSessionState.Completed,
                    JobExecutionState.Cancelled => JobDebugSessionState.Cancelled,
                    _ => JobDebugSessionState.Failed
                });
                _executionLogService.Finish(executionLog,
                    cancellation.IsForceStopRequested || jobWasCancelled ? LogOutcome.Stopped
                        : jobCompletedSuccessfully ? LogOutcome.Successful : LogOutcome.Failed,
                    cancellation.IsForceStopRequested ? "ForceStop" : completionReason.ToString());

                _executionChain.Value = parentChain;
            }
        }

        private async Task<EndJobStep?> ExecuteStepSequenceAsync(
            IReadOnlyList<JobStep>? configuredSteps,
            string phaseName,
            StepPipelineContext pipelineCtx,
            Job job,
            CancellationToken ct,
            bool continueAfterStepError,
            IReadOnlyList<JobStep>? precedingSteps = null,
            Action? onStepError = null,
            JobDebugSession? debugSession = null)
        {
            var steps = configuredSteps ?? [];
            if (steps.Count == 0)
            {
                _executionLogService.Write(
                    pipelineCtx.ExecutionLogSession,
                    ExecutionLogLevel.Debug,
                    $"{phaseName} enthält keine Steps.");
                return null;
            }

            _executionLogService.Write(
                pipelineCtx.ExecutionLogSession,
                ExecutionLogLevel.Information,
                $"{phaseName} gestartet.",
                $"Steps={steps.Count}");

            var branchStack = new Stack<BranchFrame>();
            var conditionSources = BuildConditionSources((precedingSteps ?? []).Concat(steps).ToList());

            foreach (var step in steps)
            {
                ct.ThrowIfCancellationRequested();
                bool parentActive = branchStack.Count == 0 || branchStack.Peek().CurrentActive;

                if (debugSession != null
                    && step.IsEnabled
                    && step is IControlFlowMarker)
                    await debugSession.BeforeStepAsync(step, phaseName, ct, BuildStepStartDetails(step, phaseName, null)).ConfigureAwait(false);

                if (ProcessConditionControlFlow(
                        step, branchStack, pipelineCtx.Results, conditionSources, pipelineCtx.ExecutionLogSession, phaseName) is { } transition)
                {
                    CompleteConditionControlFlow(
                        debugSession, step, transition);
                    continue;
                }

                if (!parentActive)
                {
                    debugSession?.MarkSkipped(step, "Inaktiver Bedingungszweig.");
                    StepLogScope.Skip(_executionLogService, pipelineCtx.ExecutionLogSession, step, phaseName, null, "InactiveBranch");
                    continue;
                }

                if (!step.IsEnabled)
                {
                    StepLogScope.Skip(_executionLogService, pipelineCtx.ExecutionLogSession, step, phaseName, null, "Disabled");
                    continue;
                }

                if (debugSession != null)
                    await debugSession.BeforeStepAsync(step, phaseName, ct, BuildStepStartDetails(step, phaseName, null)).ConfigureAwait(false);

                if (step is EndJobStep endJobStep)
                {
                    var observed = ExecuteControlStep(endJobStep, pipelineCtx.ExecutionLogSession, phaseName, null,
                        () => (EndJobStep)StepInputMaterializer.Materialize(endJobStep, pipelineCtx.Results), "EndJob");
                    debugSession?.MarkCompleted(step, $"{phaseName} durch EndJob beendet.");
                    return observed;
                }

                try
                {
                    await ExecuteStepAsync(step, pipelineCtx, job, ct, phaseName).ConfigureAwait(false);
                    var debugResult = pipelineCtx.Results.GetRaw(step.Id);
                    debugSession?.MarkCompleted(
                        step,
                        BuildStepResultDetails(step, debugResult, pipelineCtx.Results),
                        debugResult);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    if (debugSession != null)
                        await debugSession.PauseAfterFailureAsync(step, ex, ct).ConfigureAwait(false);
                    JobStepErrorOccurred?.Invoke(this, new JobStepErrorEventArgs(job.Name, step.GetType().Name, ex));
                    onStepError?.Invoke();
                    if (!continueAfterStepError) throw new StepException(ex);
                }
            }

            _executionLogService.Write(
                pipelineCtx.ExecutionLogSession,
                ExecutionLogLevel.Information,
                $"{phaseName} beendet.");
            return null;
        }

        /// <summary>Führt einen einzelnen Step aus – loggt Fehler und rethrowt.</summary>
        private async Task ExecuteStepAsync(
            JobStep step,
            StepPipelineContext ctx,
            Job job,
            CancellationToken ct,
            string phaseName,
            int? iteration = null)
        {
            using var log = new StepLogScope(_executionLogService, ctx.ExecutionLogSession, step, phaseName, iteration);
            var observedStep = step;
            try
            {
                if (!_stepHandlers.TryGetValue(step.GetType(), out var handler))
                    throw new InvalidOperationException($"No handler for {step.GetType().Name}.");
                observedStep = StepInputMaterializer.Materialize(step, ctx.Results);
                if (observedStep is DesktopDuplicationStep captureStep) StartRecordingOverlayFor(captureStep);
                await handler.ExecuteAsync(observedStep, ctx, ct);
                log.Complete(observedStep, ctx.Results.GetRaw(step.Id));
            }
            catch (Exception error)
            {
                log.Fail(error, observedStep);
                if (error is not OperationCanceledException)
                    _logger.LogError(error, "Fehler in Step '{StepType}'.", step.GetType().Name);
                throw;
            }
        }

        private TStep ExecuteControlStep<TStep>(JobStep step, ExecutionLogSession session, string phase,
            int? iteration, Func<TStep> execute, string reason) where TStep : JobStep
        {
            using var log = new StepLogScope(_executionLogService, session, step, phase, iteration);
            try
            {
                var observed = execute();
                log.Complete(observed, reason: reason);
                return observed;
            }
            catch (Exception error) { log.Fail(error); throw; }
        }

        private void StartRecordingOverlayFor(DesktopDuplicationStep step)
        {
            StartRecordingOverlay(new RecordingIndicatorOptions
            {
                MonitorIndex = step.Settings.DesktopIdx,
                Color = new GameOverlay.Drawing.Color(255, 64, 64, 220),
                BorderThickness = 2f,
                Mode = RecordingIndicatorMode.RedBorder,
                BadgeCorner = Corner.TopRight,
                Label = "REC"
            });
        }

        private void RecordFailure(ExecutionLogSession session, string message, Exception error, JobStep? step = null)
            => _executionLogService.Record(session, new LogEvent
            {
                Level = ExecutionLogLevel.Error,
                Message = message,
                Details = LogDiagnostics.ExceptionDetails(error),
                Context = LogAmbient.Current with { StepId = step?.Id ?? LogAmbient.Current.StepId },
                ProblemId = LogDiagnostics.ProblemId(error, LogAmbient.Current.StepExecutionId),
                DiagnosticCode = LogDiagnostics.Code(error),
                Parameters = new() { ["StepType"] = step?.GetType().Name }
            });

        private static string BuildStepStartDetails(JobStep step, string phaseName, int? iteration)
            => $"Phase={StepLogEvents.Phase(phaseName)}, Iteration={iteration}";

        private static string? BuildStepResultDetails(JobStep step, object? result, IJobResultStore results)
            => string.Join(", ", StepLogEvents.Result(result).Select(pair => $"{pair.Key}={pair.Value}"));

        /// <summary>
        /// Methode für Step-Handler um Fehler zu melden
        /// </summary>
        public void ReportStepError(string stepType, Exception exception)
        {
            var jobName = CurrentJob?.Name ?? "Unknown";
            _logger.LogError(exception, "Step-Fehler gemeldet: {StepType} in Job {JobName}.",
                stepType, jobName);

            JobStepErrorOccurred?.Invoke(this, new JobStepErrorEventArgs(jobName, stepType, exception));
        }

        /// <summary>
        /// Lädt alle YOLO-Modelle vor, die im Job verwendet werden, um bessere Performance zu erzielen.
        /// </summary>
        private async Task PreloadYoloModelsAsync(Job job, StepPipelineContext context, CancellationToken ct)
        {
            if (_yoloManager == null)
            {
                _logger.LogDebug("YoloManager nicht verfügbar - YOLO-Modell-Vorladen übersprungen");
                return;
            }

            var yoloSteps = job.EnumerateAllSteps().OfType<YOLODetectionStep>().Where(step => step.IsEnabled).ToList();
            if (yoloSteps.Count == 0)
            {
                _logger.LogDebug("Keine YOLO-Steps im Job '{JobName}' - Vorladen übersprungen", job.Name);
                return;
            }

            var modelsToPreload = yoloSteps
                .Select(step => ((YOLODetectionStep)StepInputMaterializer.MaterializeFields(
                    step, context.Results, new HashSet<string> { YoloDetectionStepDefinition.SelectionFieldId })).Settings.Model)
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
                    context.RegisterYoloModel(model);
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
        private async Task UnloadYoloModelsAsync(Job job, IEnumerable<string> loadedModels)
        {
            if (_yoloManager == null)
            {
                return;
            }

            var modelsToUnload = loadedModels
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
                try { WindowsInputBlockController.Unblock(); } catch { /* best-effort */ }
                try { _ocrService.Dispose(); } catch { /* best-effort */ }
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
                ParentActive = parentActive;
                AnyMatched = anyMatched;
                CurrentActive = currentActive;
            }
        }

        private sealed record ConditionControlFlowTransition(
            ExecutionLogLevel LogLevel,
            string Message,
            string? Details = null,
            ConditionEvaluation? Evaluation = null,
            bool MarkSkipped = false);

        private ConditionControlFlowTransition? ProcessConditionControlFlow(
            JobStep step,
            Stack<BranchFrame> branchStack,
            IJobResultStore results,
            IReadOnlyDictionary<string, ConditionStepSource> conditionSources,
            ExecutionLogSession executionLog,
            string phase,
            int? iteration = null)
        {
            if (step is not (IfStep or ElseIfStep or ElseStep or EndIfStep)) return null;
            var parentActive = branchStack.Count == 0 || (step is IfStep
                ? branchStack.Peek().CurrentActive : branchStack.Peek().ParentActive);
            var previousBranchMatched = branchStack.Count > 0 && branchStack.Peek().AnyMatched;
            using var log = new StepLogScope(_executionLogService, executionLog, step, phase, iteration);
            try
            {
                var transition = EvaluateConditionControlFlow(step, branchStack, results, conditionSources)!;
                var evaluation = transition.Evaluation?.DebugEvaluation;
                var active = !transition.MarkSkipped && (branchStack.Count == 0 || branchStack.Peek().CurrentActive);
                var reason = transition.MarkSkipped ? "InvalidControlFlow" : step is EndIfStep ? "BlockClosed"
                    : !parentActive ? "ParentInactive"
                    : step is ElseIfStep or ElseStep && previousBranchMatched ? "PreviousBranchMatched"
                    : evaluation?.State == ConditionDebugState.NotEvaluated ? "PreviousBranchMatched"
                    : evaluation?.Conditions.Any(item => item.State == ConditionDebugState.Unavailable) == true ? "ConditionUnavailable"
                    : active ? "BranchSelected" : "ConditionNotMet";
                var decision = new LogBranchDecision(parentActive, active, reason, evaluation?.MatchMode.ToString(),
                    evaluation?.Conditions.Select((item, index) => new LogConditionOutcome(index + 1,
                        item.State.ToString(), item.Definition.Operator.ToString())).ToArray() ?? []);
                log.Complete(reason: reason, level: transition.LogLevel, branch: decision, skipped: transition.MarkSkipped);
                return transition;
            }
            catch (Exception error) { log.Fail(error); throw; }
        }

        private ConditionControlFlowTransition? EvaluateConditionControlFlow(
            JobStep step,
            Stack<BranchFrame> branchStack,
            IJobResultStore results,
            IReadOnlyDictionary<string, ConditionStepSource> conditionSources)
        {
            var parentActive = branchStack.Count == 0 || branchStack.Peek().CurrentActive;
            if (step is IfStep configuredIf)
            {
                var settings = parentActive
                    ? ((IfStep)StepInputMaterializer.Materialize(configuredIf, results)).Settings
                    : configuredIf.Settings;
                var evaluation = parentActive
                    ? EvaluateCondition(settings, results, conditionSources)
                    : NotEvaluated(settings, "Der übergeordnete Bedingungszweig ist inaktiv.");
                var conditionMet = parentActive && evaluation.IsMatch;
                branchStack.Push(new BranchFrame(parentActive, conditionMet, conditionMet));
                return new ConditionControlFlowTransition(
                    ExecutionLogLevel.Information,
                    conditionMet ? "IF-Zweig wird ausgeführt." : "IF-Zweig wird übersprungen.",
                    evaluation.Details,
                    evaluation);
            }

            if (step is ElseIfStep configuredElseIf)
            {
                var settings = configuredElseIf.Settings ?? new IfConditionSettings();
                if (branchStack.Count == 0)
                    return new ConditionControlFlowTransition(
                        ExecutionLogLevel.Warning,
                        "ELSE-IF steht außerhalb eines IF-Blocks und wird übersprungen.",
                        MarkSkipped: true);

                var top = branchStack.Pop();
                if (top.ParentActive && !top.AnyMatched)
                {
                    settings = ((ElseIfStep)StepInputMaterializer.Materialize(configuredElseIf, results)).Settings;
                    var evaluation = EvaluateCondition(settings, results, conditionSources);
                    branchStack.Push(new BranchFrame(top.ParentActive, evaluation.IsMatch, evaluation.IsMatch));
                    return new ConditionControlFlowTransition(
                        ExecutionLogLevel.Information,
                        evaluation.IsMatch
                            ? "ELSE-IF-Zweig wird ausgeführt."
                            : "ELSE-IF-Zweig wird übersprungen.",
                        evaluation.Details,
                        evaluation);
                }

                var reason = !top.ParentActive
                    ? "Der übergeordnete Bedingungszweig ist inaktiv."
                    : "Ein vorheriger IF-/ELSE-IF-Zweig wurde bereits ausgeführt.";
                var skippedEvaluation = NotEvaluated(settings, reason);
                branchStack.Push(new BranchFrame(top.ParentActive, top.AnyMatched, false));
                return new ConditionControlFlowTransition(
                    ExecutionLogLevel.Information,
                    "ELSE-IF-Zweig wird übersprungen.",
                    skippedEvaluation.Details,
                    skippedEvaluation);
            }

            if (step is ElseStep)
            {
                if (branchStack.Count == 0)
                    return new ConditionControlFlowTransition(
                        ExecutionLogLevel.Warning,
                        "ELSE steht außerhalb eines IF-Blocks und wird übersprungen.",
                        MarkSkipped: true);

                var top = branchStack.Pop();
                var executeElse = top.ParentActive && !top.AnyMatched;
                var details = executeElse
                    ? "Kein vorheriger IF-/ELSE-IF-Zweig wurde erfüllt."
                    : !top.ParentActive
                        ? "Der übergeordnete Bedingungszweig ist inaktiv."
                        : "Ein vorheriger IF-/ELSE-IF-Zweig wurde bereits ausgeführt.";
                branchStack.Push(new BranchFrame(top.ParentActive, true, executeElse));
                return new ConditionControlFlowTransition(
                    ExecutionLogLevel.Information,
                    executeElse ? "ELSE-Zweig wird ausgeführt." : "ELSE-Zweig wird übersprungen.",
                    details);
            }

            if (step is EndIfStep)
            {
                if (branchStack.Count == 0)
                    return new ConditionControlFlowTransition(
                        ExecutionLogLevel.Warning,
                        "ENDIF steht außerhalb eines IF-Blocks.",
                        MarkSkipped: true);
                branchStack.Pop();
                return new ConditionControlFlowTransition(
                    ExecutionLogLevel.Debug,
                    "IF-Block beendet.");
            }

            return null;
        }

        private static void CompleteConditionControlFlow(
            JobDebugSession? debugSession, JobStep step, ConditionControlFlowTransition transition)
        {
            if (transition.MarkSkipped)
                debugSession?.MarkSkipped(step, transition.Message);
            else
                debugSession?.MarkCompleted(
                    step,
                    transition.Details,
                    conditionEvaluation: transition.Evaluation?.DebugEvaluation);
        }

        private sealed record ConditionEvaluation(
            bool IsMatch,
            string Details,
            ConditionDebugEvaluation DebugEvaluation);
        private sealed record SingleConditionEvaluation(
            bool IsMatch,
            string Details,
            string? ActualValue,
            string? ExpectedValue,
            ConditionDebugState State,
            string? Diagnostic = null);
        private sealed record ConditionStepSource(string Label, ResultTypeDescriptor? ResultType)
        {
            public string? ResultTypeName => ResultType?.TypeName;
        }

        private static ConditionEvaluation EvaluateCondition(
            IfConditionSettings settings,
            IJobResultStore results,
            IReadOnlyDictionary<string, ConditionStepSource> conditionSources)
        {
            var conditions = settings?.Conditions ?? [];
            if (conditions.Count == 0)
                return new ConditionEvaluation(
                    false,
                    "Keine Bedingungen konfiguriert.",
                    new ConditionDebugEvaluation(
                        settings.MatchMode,
                        ConditionDebugState.Unavailable,
                        [],
                        false,
                        "Keine Bedingungen konfiguriert."));

            var evaluations = new List<(int Index, SingleConditionEvaluation Evaluation)>(conditions.Count);
            var isAll = settings.MatchMode == ConditionMatchMode.All;
            var isMatch = isAll;
            var resultDecided = false;
            for (var index = 0; index < conditions.Count; index++)
            {
                SingleConditionEvaluation evaluation;
                if (resultDecided)
                {
                    const string reason = "Das Gesamtergebnis steht bereits fest.";
                    evaluation = new SingleConditionEvaluation(
                        false,
                        $"NICHT AUSGEWERTET — {reason}",
                        null,
                        null,
                        ConditionDebugState.NotEvaluated,
                        reason);
                }
                else
                {
                    evaluation = EvaluateSingleCondition(conditions[index], results, conditionSources);
                    if (isAll)
                    {
                        isMatch &= evaluation.IsMatch;
                        resultDecided = !evaluation.IsMatch;
                    }
                    else
                    {
                        isMatch |= evaluation.IsMatch;
                        resultDecided = evaluation.IsMatch;
                    }
                }
                evaluations.Add((index + 1, evaluation));
            }
            var mode = settings.MatchMode == ConditionMatchMode.All ? "ALLE (AND)" : "MINDESTENS EINE (OR)";
            var lines = new List<string> { $"Verknüpfung: {mode}" };
            lines.AddRange(evaluations.Select(item => $"{item.Index}. {item.Evaluation.Details}"));
            lines.Add($"Gesamtergebnis: {(isMatch ? "ERFÜLLT" : "NICHT ERFÜLLT")}");
            var debugItems = evaluations.Select((item, index) =>
            {
                var evaluation = item.Evaluation;
                return new ConditionDebugItem(
                    conditions[index],
                    evaluation.State,
                    evaluation.ActualValue,
                    evaluation.ExpectedValue,
                    evaluation.Diagnostic);
            }).ToArray();
            return new ConditionEvaluation(
                isMatch,
                string.Join(Environment.NewLine, lines),
                new ConditionDebugEvaluation(
                    settings.MatchMode,
                    isMatch ? ConditionDebugState.Met : ConditionDebugState.NotMet,
                    debugItems,
                    isMatch));
        }

        private static ConditionEvaluation NotEvaluated(
            IfConditionSettings settings,
            string reason)
        {
            var items = settings.Conditions
                .Select(condition => new ConditionDebugItem(
                    condition,
                    ConditionDebugState.NotEvaluated,
                    null,
                    null,
                    reason))
                .ToArray();
            return new ConditionEvaluation(
                false,
                $"Nicht ausgewertet: {reason}",
                new ConditionDebugEvaluation(
                    settings.MatchMode,
                    ConditionDebugState.NotEvaluated,
                    items,
                    false,
                    reason));
        }

        private static SingleConditionEvaluation EvaluateSingleCondition(
            StepCondition condition,
            IJobResultStore results,
            IReadOnlyDictionary<string, ConditionStepSource> conditionSources)
        {
            if (!condition.IsConfigured)
                return Unavailable("Die Wertreferenz der Bedingung fehlt.");

            if (!TryReadReferenceValue(results, condition, conditionSources,
                    out var descriptor, out var value, out var leftWasExecuted))
                return Unavailable($"{FormatValueReference(condition, conditionSources)}: Wert ist nicht verfügbar.");
            var leftName = FormatValueReference(condition, conditionSources, descriptor);
            var leftStatus = leftWasExecuted ? string.Empty : " (Standardwert; Step wurde nicht ausgeführt)";

            if (condition.Operator == ConditionOperator.IsEmpty)
            {
                var isEmpty = IsEmptyConditionValue(value);
                return BuildSingleEvaluation(isEmpty, leftName + leftStatus, descriptor, value, "ist leer", null);
            }
            if (condition.Operator == ConditionOperator.IsNotEmpty)
            {
                var isNotEmpty = !IsEmptyConditionValue(value);
                return BuildSingleEvaluation(isNotEmpty, leftName + leftStatus, descriptor, value, "ist nicht leer", null);
            }
            if (condition.Operator == ConditionOperator.IsTrue)
                return BuildSingleEvaluation(value is bool b && b, leftName + leftStatus, descriptor, value, "=", "Festwert true");
            if (condition.Operator == ConditionOperator.IsFalse)
                return BuildSingleEvaluation(value is bool b && !b, leftName + leftStatus, descriptor, value, "=", "Festwert false");
            if (value is null)
                return Unavailable($"{leftName} [{descriptor.DataType}]: Wert ist <null>.", "null");

            var comparison = condition.EffectiveComparison;
            object? expected;
            string expectedText;
            if (comparison.Kind == ComparisonOperandKind.Literal)
            {
                if (!StepResultMetadata.TryParseComparison(descriptor, comparison.Value, out expected))
                    return Unavailable(
                        $"{leftName}: Festwert '{comparison.Value}' ist für {descriptor.DataType} ungültig.",
                        FormatLogValue(value),
                        comparison.Value);
                expectedText = $"Festwert {FormatLogValue(expected)}";
            }
            else
            {
                if (!TryReadReferenceValue(results, comparison, conditionSources,
                        out var rightDescriptor, out expected, out var rightWasExecuted))
                    return Unavailable(
                        $"{FormatValueReference(comparison, conditionSources)}: Vergleichswert ist nicht verfügbar.",
                        FormatLogValue(value));
                var rightName = FormatValueReference(comparison, conditionSources, rightDescriptor);
                if (!ConditionRules.AreComparisonSourcesCompatible(
                        descriptor,
                        rightDescriptor,
                        string.Equals(comparison.ProviderId, ValueProviderIds.LocalValue, StringComparison.Ordinal),
                        expected?.ToString()))
                    return Unavailable(
                        $"Datentypen stimmen nicht überein: {descriptor.DataType} und {rightDescriptor.DataType}.",
                        FormatLogValue(value),
                        FormatLogValue(expected));
                var rightStatus = rightWasExecuted ? string.Empty : " (Standardwert; Step wurde nicht ausgeführt)";
                expectedText = $"{rightName}{rightStatus} [{rightDescriptor.DataType}] = {FormatLogValue(expected)}";
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
                $"{(isMatch ? "ERFÜLLT" : "NICHT ERFÜLLT")} — {leftName} [{descriptor.DataType}] = {FormatLogValue(actual)} "
                + conditionOperator + (expected is null ? string.Empty : $" {expected}"),
                FormatLogValue(actual),
                expected,
                isMatch ? ConditionDebugState.Met : ConditionDebugState.NotMet);

        private static SingleConditionEvaluation Unavailable(
            string diagnostic,
            string? actualValue = null,
            string? expectedValue = null) =>
            new(
                false,
                $"NICHT ERFÜLLT — {diagnostic}",
                actualValue,
                expectedValue,
                ConditionDebugState.Unavailable,
                diagnostic);

        private static Dictionary<string, ConditionStepSource> BuildConditionSources(IReadOnlyList<JobStep> steps)
        {
            var sources = new Dictionary<string, ConditionStepSource>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < steps.Count; index++)
                if (!string.IsNullOrWhiteSpace(steps[index].Id))
                    sources[steps[index].Id] = new ConditionStepSource(
                        $"Step {index + 1} ({steps[index].GetType().Name})",
                        StepResultMetadata.GetResultTypeForStep(steps[index]));
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

        private static string FormatValueReference(
            ResultBinding binding,
            IReadOnlyDictionary<string, ConditionStepSource> conditionSources,
            ResultPropertyDescriptor? descriptor = null)
        {
            if (binding.HasProviderReference
                && !string.Equals(binding.ProviderId, ValueProviderIds.StepResult, StringComparison.Ordinal))
                return descriptor is null
                    ? $"{binding.ProviderId} → {binding.SourceId}"
                    : $"{binding.ProviderId} → {descriptor.DisplayName}";
            return FormatResultReference(
                binding.SourceStepId,
                string.IsNullOrWhiteSpace(binding.PropertyPath) ? binding.PropertyId : binding.PropertyPath,
                conditionSources);
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

        private static bool IsEmptyConditionValue(object? value)
        {
            if (value is null) return true;
            if (value is string text) return string.IsNullOrEmpty(text);
            if (value is System.Collections.IEnumerable values)
            {
                var enumerator = values.GetEnumerator();
                try { return !enumerator.MoveNext(); }
                finally { (enumerator as IDisposable)?.Dispose(); }
            }
            return false;
        }

        private static bool TryReadReferenceValue(
            IJobResultStore results, ResultBinding binding,
            IReadOnlyDictionary<string, ConditionStepSource> conditionSources,
            out ResultPropertyDescriptor descriptor, out object? value, out bool wasExecuted)
        {
            var configured = binding.TryGetStepResult(out var stepResult)
                && conditionSources.TryGetValue(stepResult.StepId, out var source) ? source.ResultType : null;
            var resolved = ValueReferenceResolver.Resolve(results, binding, configured);
            descriptor = resolved.Descriptor?.ToResultProperty()!;
            value = resolved.Value;
            wasExecuted = resolved.IsSuccess;
            return resolved.IsSuccess && resolved.Descriptor is { IsSensitive: false };
        }

        private async Task<IReadOnlyDictionary<Guid, (ValueProviderSourceDescriptor Descriptor, string Value)>>
            LoadSecretValuesAsync(Job job, CancellationToken cancellationToken)
        {
            var ids = ValueReferenceUsageInspector.Find(job)
                .Where(usage => string.Equals(
                    usage.Reference.ProviderId, ValueProviderIds.Secret, StringComparison.Ordinal))
                .Select(usage => Guid.TryParse(usage.Reference.SourceId, out var id) ? id : Guid.Empty)
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToArray();
            if (ids.Length == 0 || _secretStore is null)
                return new Dictionary<Guid, (ValueProviderSourceDescriptor Descriptor, string Value)>();

            var descriptors = (await _secretStore.ListAsync(cancellationToken).ConfigureAwait(false))
                .ToDictionary(secret => secret.Id);
            var values = new Dictionary<Guid, (ValueProviderSourceDescriptor Descriptor, string Value)>();
            foreach (var id in ids)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!descriptors.TryGetValue(id, out var secret)) continue;
                var read = await _secretStore.ReadAsync(id, cancellationToken).ConfigureAwait(false);
                if (read.Status != SecretReadStatus.Success || read.Value is null) continue;
                _executionLogService.RegisterSecrets([read.Value]);
                values[id] = (new ValueProviderSourceDescriptor(
                    ValueProviderIds.Secret,
                    id.ToString("D"),
                    secret.Name,
                    secret.Description,
                    ResultValueKind.Text,
                    ResultCardinality.Single,
                    IsSensitive: true), read.Value);
            }
            return values;
        }

        private static bool CompareForOperator(object value, ResultPropertyDescriptor descriptor, ConditionOperator op, object? expected)
        {
            if (expected is null) return false;
            if (descriptor.DataType == ResultValueKind.Text)
            {
                var actualText = value.ToString() ?? ""; var expectedText = expected?.ToString() ?? "";
                if (op == ConditionOperator.Contains) return actualText.Contains(expectedText, StringComparison.OrdinalIgnoreCase);
                if (op == ConditionOperator.StartsWith) return actualText.StartsWith(expectedText, StringComparison.OrdinalIgnoreCase);
            }
            int cmp = descriptor.DataType switch
            {
                ResultValueKind.Number => Convert.ToDouble(value).CompareTo(Convert.ToDouble(expected)),
                ResultValueKind.Integer => Convert.ToInt64(value).CompareTo(Convert.ToInt64(expected)),
                ResultValueKind.DateTime => Convert.ToDateTime(value).CompareTo((DateTime)expected!),
                ResultValueKind.Boolean => Convert.ToBoolean(value).CompareTo(Convert.ToBoolean(expected)),
                _ => string.Compare(value.ToString(), expected?.ToString(), StringComparison.OrdinalIgnoreCase)
            };
            return op switch
            {
                ConditionOperator.Equals => cmp == 0,
                ConditionOperator.NotEquals => cmp != 0,
                ConditionOperator.GreaterThan => cmp > 0,
                ConditionOperator.LessThan => cmp < 0,
                ConditionOperator.GreaterThanOrEqual => cmp >= 0,
                ConditionOperator.LessThanOrEqual => cmp <= 0,
                _ => false
            };
        }
    }
}
