using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using OpenCvSharp;
using ImageCapture.ProcessDuplication;
using ImageCapture.DesktopDuplication;
using ImageCapture.Video;
using System.Drawing;
using ImageDetection.Algorithms.TemplateMatching;
using ImageDetection;
using System.Text;
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

namespace TaskAutomation.Jobs
{
    public class JobExecutor : IJobExecutor, IDisposable
    {
        private readonly ILogger<JobExecutor> _logger;
        private readonly IJsonRepository<Job> _jobRepository;
        private readonly IJsonRepository<Makro> _makroRepository;
        private readonly IRecordingIndicatorOverlay _recordingOverlay;
        private readonly IImageDisplayService _imageDisplayService;
        private readonly IMakroExecutor _makroExecutor;
        private readonly IScriptExecutor _scriptExecutor;
        private readonly IYoloManager _yoloManager;
        private bool _disposed = false;

        private readonly DxgiResources _dxgiResources = DxgiResources.Instance;
        private readonly Dictionary<string, Job>   _allJobs   = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Makro> _allMakros = new(StringComparer.OrdinalIgnoreCase);
        private Job? _currentJob;

        // ── Events ─────────────────────────────────────────────────────────────
        public event EventHandler<JobErrorEventArgs>?     JobErrorOccurred;
        public event EventHandler<JobStepErrorEventArgs>? JobStepErrorOccurred;

        // ── Zyklus-Erkennung ───────────────────────────────────────────────────
        private static readonly AsyncLocal<ImmutableHashSet<Guid>> _executionChain = new();

        // ── Exklusive Steps ────────────────────────────────────────────────────
        private static readonly Dictionary<Type, string> _exclusiveSteps = new()
        {
            [typeof(DesktopDuplicationStep)] = "Desktop Duplication wird bereits von Job '{0}' verwendet.",
        };
        private readonly ConcurrentDictionary<Type, string> _exclusiveStepOwners = new();

        // ── Pipeline-Validierungs-Cache (pro Job-ID, wird bei Reload gelöscht) ──

        // ── Handler-Registry ───────────────────────────────────────────────────
        private readonly Dictionary<Type, IJobStepHandler> _stepHandlers = new()
        {
            { typeof(ProcessDuplicationStep),  new ProcessDuplicationStepHandler()  },
            { typeof(DesktopDuplicationStep),  new DesktopDuplicationStepHandler()  },
            { typeof(TemplateMatchingStep),    new TemplateMatchingStepHandler()    },
            { typeof(ShowImageStep),           new ShowImageStepHandler()           },
            { typeof(VideoCreationStep),       new VideoCreationStepHandler()       },
            { typeof(MakroExecutionStep),      new MakroExecutionStepHandler()      },
            { typeof(ScriptExecutionStep),     new ScriptExecutionStepHandler()     },
            { typeof(KlickOnPointStep),        new KlickOnPointStepHandler()        },
            { typeof(KlickOnPoint3DStep),      new KlickOnPoint3DStepHandler()      },
            { typeof(JobExecutionStep),        new JobExecutionStepHandler()        },
            { typeof(YOLODetectionStep),       new YOLOStepHandler()                },
            { typeof(TimeoutStep),             new TimeoutStepHandler()             },
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
            IImageDisplayService imageDisplayService)
        {
            _logger            = logger;
            _jobRepository     = jobRepo;
            _makroRepository   = makroRepo;
            _makroExecutor     = makroExecutor;
            _recordingOverlay  = recordingOverlay;
            _scriptExecutor    = scriptExecutor;
            _yoloManager       = yoloManager;
            _imageDisplayService = imageDisplayService;

            _ = ReloadJobsAsync();
            _ = ReloadMakrosAsync();

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
                _allMakros[m.Id.ToString()] = m;
                added++;
            }
            _logger.LogInformation("Makros geladen: {Count}", added);
        }


        public Task ExecuteJob(string jobName, CancellationToken ct = default)
            => ExecuteJobAsync(jobName, ct);

        public Task ExecuteJob(Guid jobId, CancellationToken ct = default)
        {
            var job = AllJobs.Values.FirstOrDefault(j => j.Id == jobId);
            if (job == null)
            {
                var errorMessage = $"Job mit ID '{jobId}' existiert nicht.";
                _logger.LogError(errorMessage);
                JobErrorOccurred?.Invoke(this, new JobErrorEventArgs(jobId.ToString(), new ArgumentException(errorMessage)));
                return Task.CompletedTask;
            }
            return ExecuteJobAsync(job, ct);
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
            await ExecuteJobAsync(job, ct).ConfigureAwait(false);
        }

        private async Task ExecuteJobAsync(Job job, CancellationToken ct)
        {
            if (job == null)
            {
                var err = "Job ist null.";
                _logger.LogError(err);
                JobErrorOccurred?.Invoke(this, new JobErrorEventArgs("Unknown",
                    new ArgumentNullException(nameof(job), err)));
                return;
            }

            CurrentJob = job;
            _logger.LogInformation("Starte Job: {JobName}", job.Name);

            // ── Zyklus-Erkennung ──────────────────────────────────────────────
            var parentChain = _executionChain.Value ?? ImmutableHashSet<Guid>.Empty;
            if (parentChain.Contains(job.Id))
            {
                var chain = string.Join(" → ", parentChain.Select(id =>
                    _allJobs.Values.FirstOrDefault(j => j.Id == id)?.Name ?? id.ToString()));
                var err = $"Zirkuläre Abhängigkeit erkannt: Job '{job.Name}' ist bereits in [{chain}].";
                _logger.LogError(err);
                JobErrorOccurred?.Invoke(this, new JobErrorEventArgs(job.Name, new InvalidOperationException(err)));
                return;
            }
            _executionChain.Value = parentChain.Add(job.Id);

            // ── YOLO-Modelle vorladen ─────────────────────────────────────────
            await PreloadYoloModelsAsync(job, ct);

            // ── Schritte analysieren ──────────────────────────────────────────
            var videoStep             = job.Steps.OfType<VideoCreationStep>().FirstOrDefault();
            var desktopDuplicationStep = job.Steps.OfType<DesktopDuplicationStep>().FirstOrDefault();
            var showImageStep         = job.Steps.OfType<ShowImageStep>().FirstOrDefault();

            // ── Exklusive Steps reservieren ───────────────────────────────────
            var acquiredSteps = new List<Type>();
            foreach (var (stepType, errTemplate) in _exclusiveSteps)
            {
                if (!job.Steps.Any(s => s.GetType() == stepType)) continue;

                if (!_exclusiveStepOwners.TryAdd(stepType, job.Name))
                {
                    var owner = _exclusiveStepOwners[stepType];
                    var err   = string.Format(errTemplate, owner);
                    _logger.LogWarning(err);
                    JobErrorOccurred?.Invoke(this, new JobErrorEventArgs(job.Name, new InvalidOperationException(err)));
                    foreach (var acquired in acquiredSteps)
                        _exclusiveStepOwners.TryRemove(acquired, out _);
                    return;
                }
                acquiredSteps.Add(stepType);
            }

            // ── Pipeline-Kontext erstellen ────────────────────────────────────
            var pipelineCtx = new StepPipelineContext(
                _logger,
                _dxgiResources,
                _allJobs,
                _allMakros,
                _makroExecutor,
                _scriptExecutor,
                _yoloManager,
                _imageDisplayService,
                job,
                ExecuteJob);

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
                do
                {
                    pipelineCtx.ResetResults();

                    var steps = job.Steps ?? Enumerable.Empty<JobStep>().ToList();
                    var branchStack = new Stack<BranchFrame>();

                    foreach (var step in steps)
                    {
                        ct.ThrowIfCancellationRequested();

                        bool parentActive = branchStack.Count == 0 || branchStack.Peek().CurrentActive;

                        // ── Control-flow steps: handle without executing ────────
                        if (step is IfStep ifStep)
                        {
                            bool condMet = parentActive && EvaluateCondition(ifStep.Settings, pipelineCtx.Results);
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
                                    bool condMet = EvaluateCondition(elseIfStep.Settings, pipelineCtx.Results);
                                    branchStack.Push(new BranchFrame(top.ParentActive, condMet, condMet));
                                }
                                else
                                {
                                    // A branch already matched or parent inactive – skip this branch.
                                    branchStack.Push(new BranchFrame(top.ParentActive, top.AnyMatched, false));
                                }
                            }
                            continue;
                        }

                        if (step is ElseStep)
                        {
                            if (branchStack.Count > 0)
                            {
                                var top = branchStack.Pop();
                                bool executeElse = top.ParentActive && !top.AnyMatched;
                                branchStack.Push(new BranchFrame(top.ParentActive, true, executeElse));
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
                        if (!parentActive) continue;

                        try
                        {
                            await ExecuteStepAsync(step, pipelineCtx, job, ct).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException) { throw; }
                        catch (Exception ex)
                        {
                            JobStepErrorOccurred?.Invoke(this, new JobStepErrorEventArgs(job.Name, step.GetType().Name, ex));
                            // StepException verhindert, dass der äußere catch ein zweites Event feuert.
                            throw new StepException(ex);
                        }
                        _logger.LogDebug(
                            "Job '{JobName}' → Step '{StepType}' abgeschlossen.",
                            job.Name, step.GetType().Name);
                    }
                }
                while (job.Repeating && !ct.IsCancellationRequested);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Job '{JobName}' abgebrochen.", job.Name);
            }
            catch (StepException)
            {
                // Fehler wurde bereits über JobStepErrorOccurred gemeldet – kein weiteres Event.
                _logger.LogDebug("Job '{JobName}' nach Step-Fehler gestoppt.", job.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler in Job '{JobName}': {Message}", job.Name, ex.Message);
                JobErrorOccurred?.Invoke(this, new JobErrorEventArgs(job.Name, ex));
            }
            finally
            {
                CurrentJob = null;

                if (showImageStep != null)
                {
                    try { Cv2.DestroyAllWindows(); } catch { /* best-effort */ }
                }

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

                try { pipelineCtx.VideoRecorder?.Dispose(); } catch { /* best-effort */ }
                try { pipelineCtx.Dispose(); }                catch { /* best-effort */ }
                try { Cv2.DestroyAllWindows(); }              catch { /* best-effort */ }

                await UnloadYoloModelsAsync(job);

                _logger.LogInformation("Job '{JobName}' beendet.", job.Name);

                foreach (var acquired in acquiredSteps)
                    _exclusiveStepOwners.TryRemove(acquired, out _);

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
                await handler.ExecuteAsync(step, ctx, ct);
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

        /// <summary>
        /// Evaluates an <see cref="IfConditionSettings"/> against the current pipeline results.
        /// Returns true when all (or any) conditions in the settings are met.
        /// An empty condition list is treated as "always true".
        /// </summary>
        private static bool EvaluateCondition(IfConditionSettings settings, IJobResultStore results)
        {
            if (settings.Conditions.Count == 0) return true;

            return settings.MatchMode == ConditionMatchMode.All
                ? settings.Conditions.All(c  => EvaluateSingleCondition(c, results))
                : settings.Conditions.Any(c  => EvaluateSingleCondition(c, results));
        }

        private static bool EvaluateSingleCondition(StepCondition condition, IJobResultStore results)
        {
            if (string.IsNullOrEmpty(condition.SourceStepId) || string.IsNullOrEmpty(condition.Property))
                return false;

            var result = results.GetRaw(condition.SourceStepId);
            if (result is null) return false;

            // Use reflection so property access stays in sync with StepResultMetadata
            // without maintaining a parallel hard-coded switch.
            var prop = result.GetType()
                             .GetProperty(condition.Property, BindingFlags.Public | BindingFlags.Instance);
            if (prop is null) return false;

            var value = prop.GetValue(result);
            if (value is null) return false;

            return condition.Operator switch
            {
                ConditionOperator.IsTrue  => value is bool b  && b,
                ConditionOperator.IsFalse => value is bool b2 && !b2,
                _                         => CompareForOperator(value, condition.Operator, condition.ComparisonValue)
            };
        }

        private static bool CompareForOperator(object value, ConditionOperator op, string? comparisonValue)
        {
            int cmp;
            switch (value)
            {
                case bool b:
                    if (!bool.TryParse(comparisonValue, out bool bv)) return false;
                    cmp = b.CompareTo(bv);
                    break;
                case double d:
                    if (!double.TryParse(comparisonValue,
                            System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out double dv)) return false;
                    cmp = d.CompareTo(dv);
                    break;
                default:
                    cmp = string.Compare(value.ToString(), comparisonValue,
                                         StringComparison.OrdinalIgnoreCase);
                    break;
            }
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
