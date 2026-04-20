using ImageCapture.DesktopDuplication.RecordingIndicator;
using ImageDetection.YOLO;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TaskAutomation.Makros;
using TaskAutomation.Orchestration;

namespace TaskAutomation.Jobs
{
    public interface IJobExecutor
    {
        // ── Events ─────────────────────────────────────────────────────────────
        event EventHandler<JobErrorEventArgs>?     JobErrorOccurred;
        event EventHandler<JobStepErrorEventArgs>? JobStepErrorOccurred;

        // ── Zustand / Daten ────────────────────────────────────────────────────
        IReadOnlyDictionary<string, Job>   AllJobs   { get; }
        IReadOnlyDictionary<string, Makro> AllMakros { get; }

        /// <summary>YOLO-Manager – wird auch von UI-Dialogen für Modell-/Klassenlisten verwendet.</summary>
        IYoloManager  YoloManager   { get; }

        /// <summary>MakroExecutor – wird auch vom JobDispatcher für direkte Makro-Ausführung verwendet.</summary>
        IMakroExecutor MakroExecutor { get; }

        Job? CurrentJob { get; }

        // ── Orchestrierung ─────────────────────────────────────────────────────
        Task ExecuteJob(string jobName, CancellationToken ct = default);
        Task ExecuteJob(Guid   jobId,   CancellationToken ct = default);
        Task ReloadJobsAsync();
        Task ReloadMakrosAsync();

        void StartRecordingOverlay(RecordingIndicatorOptions? options = null);
        void StopRecordingOverlay();

        /// <summary>Meldet einen Fehler in einem Step-Handler (löst JobStepErrorOccurred aus).</summary>
        void ReportStepError(string stepType, Exception exception);
    }
}

