using ImageCapture.DesktopDuplication;
using ImageCapture.DesktopDuplication.RecordingIndicator;
using ImageCapture.ProcessDuplication;
using ImageDetection.Algorithms.TemplateMatching;
using ImageHelperMethods;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TaskAutomation.Makros;
using TaskAutomation.Orchestration;
using TaskAutomation.Scripts;
using ImageDetection.Model;
using ImageDetection.YOLO;
using TaskAutomation.Events;
using System.Drawing;

namespace TaskAutomation.Jobs
{
    public interface IJobExecutor
    {
        // Event für allgemeine Job Fehler
        event EventHandler<JobErrorEventArgs>? JobErrorOccurred;
        
        // Event für Job Step Fehler
        event EventHandler<JobStepErrorEventArgs>? JobStepErrorOccurred;

        // Original IJobExecutor members
        IReadOnlyDictionary<string, Job> AllJobs { get; }
        IReadOnlyDictionary<string, Makro> AllMakros { get; }
        Task ExecuteJob(string jobName, CancellationToken ct = default);
        Task ReloadJobsAsync();
        Task ReloadMakrosAsync();

        // Original IJobExecutor members
        Job? CurrentJob { get; }
        DxgiResources DxgiResources { get; }

        // Logger für Step Handlers
        ILogger Logger { get; }

        // Laufzeit-Zustände (mutierbar)
        ProcessDuplicatorResult ProcessDuplicationResult { get; set; }
        DesktopFrame CurrentDesktopFrame { get; set; }
        System.Drawing.Bitmap CurrentImage { get; set; }
        System.Drawing.Bitmap CurrentImageWithResult { get; set; }

        // Ressourcen
        StreamVideoRecorder VideoRecorder { get; set; }
        ProcessDuplicator ProcessDuplicator { get; set; }
        DesktopDuplicator DesktopDuplicator { get; set; }
        TemplateMatching TemplateMatcher { get; set; }
        IYoloManager YoloManager { get; set; }
        IDetectionResult DetectionResult { get; set; }
        System.Drawing.Bitmap ImageToProcess { get; set; }
        Rectangle DesktopBounds { get; set; }

        // Kontext-Parameter
        OpenCvSharp.Point CurrentOffset { get; set; }
        OpenCvSharp.Point? LatestCalculatedPoint { get; set; }

        // Timeout-Tracking für Steps
        Dictionary<string, DateTime> StepTimeouts { get; }

        // Aktionen
        TaskAutomation.Makros.IMakroExecutor MakroExecutor { get; }
        IScriptExecutor ScriptExecutor { get; }
        IImageDisplayService ImageDisplayService { get; }

        void StartRecordingOverlay(RecordingIndicatorOptions? options = null);
        void StopRecordingOverlay();

        // Methode für Step-Handler um Fehler zu melden
        void ReportStepError(string stepType, Exception exception);
    }
}
