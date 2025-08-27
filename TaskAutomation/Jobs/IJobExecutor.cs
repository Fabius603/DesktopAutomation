using ImageCapture.DesktopDuplication;
using ImageCapture.DesktopDuplication.RecordingIndicator;
using ImageCapture.ProcessDuplication;
using ImageDetection.Algorithms.TemplateMatching;
using ImageHelperMethods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TaskAutomation.Makros;
using TaskAutomation.Scripts;

namespace TaskAutomation.Jobs
{
    public interface IJobExecutor
    {
        IReadOnlyDictionary<string, Job> AllJobs { get; }
        IReadOnlyDictionary<string, Makro> AllMakros { get; }

        Task ExecuteJob(string jobName, CancellationToken ct = default);
        Task ReloadJobsAsync();
        Task ReloadMakrosAsync();
    }

    public interface IJobExecutionContext
    {
        IReadOnlyDictionary<string, Makro> AllMakros { get; }

        DxgiResources DxgiResources { get; }

        // Laufzeit-Zustände (mutierbar)
        ProcessDuplicatorResult ProcessDuplicationResult { get; set; }
        DesktopFrame CurrentDesktopFrame { get; set; }
        System.Drawing.Bitmap CurrentImage { get; set; }
        OpenCvSharp.Mat CurrentImageWithResult { get; set; }

        // Ressourcen
        StreamVideoRecorder VideoRecorder { get; set; }
        ProcessDuplicator ProcessDuplicator { get; set; }
        DesktopDuplicator DesktopDuplicator { get; set; }
        TemplateMatching TemplateMatcher { get; set; }
        TemplateMatchingResult TemplateMatchingResult { get; set; }
        OpenCvSharp.Mat ImageToProcess { get; set; }

        // Kontext-Parameter
        OpenCvSharp.Point CurrentOffset { get; set; }

        OpenCvSharp.Point? LatestCalculatedPoint { get; set; }

        // Aktionen
        TaskAutomation.Makros.IMakroExecutor MakroExecutor { get; }
        IScriptExecutor ScriptExecutor { get; }

        void StartRecordingOverlay(RecordingIndicatorOptions? options = null);
        void StopRecordingOverlay();
    }
}
