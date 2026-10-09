using System.Drawing;
using System.Text.Json.Nodes;
using ImageCapture.Video;
using TaskAutomation.Contracts.Geometry;
using TaskAutomation.Jobs;
using TaskAutomation.Logging;
using TaskAutomation.Orchestration;
using TaskAutomation.Steps;
using TaskAutomation.Tests.TestDoubles;

namespace TaskAutomation.Tests.Jobs;

public sealed class JobStepRegressionTests
{
    [Fact]
    public async Task UnavailableOptionalControlSetting_SkipsEndJobWithoutInventingDefaultValue()
    {
        var flag = new JobVariable { Name = "branch", Scope = JobVariableScope.Shared, ValueKind = ResultValueKind.Boolean, Value = JsonValue.Create(false) };
        var capture = new DesktopDuplicationStep { Id = "capture" };
        var source = new ColorDetectionStep { Id = "source", Settings = new() { ImageSource = ResultBinding.ForStepResult(capture.Id, "image") } };
        var end = new EndJobStep { Id = "end" };
        end.Inputs["skip_end_steps"] = ResultBinding.ForStepResult(source.Id, "found");
        var job = new Job
        {
            Name = "missing end policy",
            Variables = [flag],
            Steps = [capture,
            new IfStep { Settings = new() { Conditions = [new StepCondition { ProviderId = ValueProviderIds.JobVariable,
                SourceId = flag.Id.ToString("D"), Operator = ConditionOperator.IsTrue }] } }, source, new EndIfStep(), end, Text("continued")]
        };
        var builder = new JobExecutorTestBuilder().WithJobs(job);
        using var executor = await builder.BuildAsync();
        await executor.ExecuteJob(job.Id);
        Assert.True(Assert.Single(builder.Logs.Completions).Success);
        Assert.Equal("continued", Assert.Single(builder.Overlay.TextCalls).Text);
        Assert.Contains(builder.Logs.Observations, entry => entry.Context.StepId == end.Id && entry.Code == LogCodes.StepSkipped);
        Assert.DoesNotContain(builder.Logs.Observations, entry => entry.Code == LogCodes.StepFailed);
    }

    [Fact]
    public async Task RepeatingJob_UsesPreviousRoiAfterFreshResultsHaveBeenCleared()
    {
        var capture = new DesktopDuplicationStep { Id = "capture" };
        var ocr = new OcrStep
        {
            Id = "ocr",
            Settings = new()
            {
                ImageSource = ResultBinding.ForStepResult(capture.Id, "image"),
                DynamicRoiSource = ResultBinding.ForStepResult("roi", "global_bounds")
            }
        };
        var roi = new DynamicRoiStep { Id = "roi", Settings = new() { BoundsSource = ResultBinding.ForStepResult(ocr.Id, "bounding_box"), Padding = 0 } };
        var job = new Job { Name = "feedback", Repeating = true, Steps = [capture, ocr, roi, Text("iteration")] };
        Assert.True(JobValidation.ValidateJob(job).IsValid);
        var service = new RecordingOcr();
        var builder = new JobExecutorTestBuilder().WithJobs(job);
        builder.DesktopCapture = new Images(); builder.Ocr = service;
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        builder.Overlay.OnShowText = _ => { if (builder.Overlay.TextCalls.Count == 2) stop.Cancel(); };
        using var executor = await builder.BuildAsync();
        await executor.ExecuteJob(job.Id, stop.Token);
        Assert.Equal([new Size(20, 20), new Size(6, 7)], service.Sizes);
        Assert.DoesNotContain(builder.Logs.Observations, entry => entry.Code == LogCodes.StepFailed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SkippedOptionalProducer_SkipsConsumerAndContinuesJob(bool debug)
    {
        var flag = new JobVariable { Name = "branch", Scope = JobVariableScope.Shared, ValueKind = ResultValueKind.Boolean, Value = JsonValue.Create(false) };
        var capture = new DesktopDuplicationStep { Id = "capture" };
        var detection = new ColorDetectionStep { Id = "detection", Settings = new() { ImageSource = ResultBinding.ForStepResult("capture", "image") } };
        var consumer = new ShowOnDesktopStep { Id = "consumer", Settings = new() { DetectionsSource = ResultBinding.ForStepResult("detection", "point") } };
        var job = new Job
        {
            Name = "optional branch",
            Variables = [flag],
            Steps = [capture,
            new IfStep { Settings = new() { Conditions = [new StepCondition { ProviderId = ValueProviderIds.JobVariable,
                SourceId = flag.Id.ToString("D"), Operator = ConditionOperator.IsTrue }] } }, detection, new EndIfStep(), consumer, Text("continued")]
        };
        var builder = new JobExecutorTestBuilder().WithJobs(job); builder.DesktopCapture = new Images();
        using var executor = await builder.BuildAsync();
        if (debug)
        {
            using var cancellation = new JobExecutionCancellation(CancellationToken.None);
            var session = new JobDebugSession(Guid.NewGuid(), job);
            var execution = executor.ExecuteJob(job.Id, JobStartContext.Unknown, cancellation, session);
            session.Continue();
            await execution;
            Assert.Equal(JobStepDebugState.Skipped, session.GetSnapshot(consumer.Id)!.State);
        }
        else await executor.ExecuteJob(job.Id);
        Assert.True(Assert.Single(builder.Logs.Completions).Success);
        Assert.Equal("continued", Assert.Single(builder.Overlay.TextCalls).Text);
        Assert.Contains(builder.Logs.Observations, entry => entry.Context.StepId == consumer.Id
            && entry.Code == LogCodes.StepSkipped && entry.Parameters.GetValueOrDefault("Reason") == "NoInput");
        Assert.DoesNotContain(builder.Logs.Observations, entry => entry.Code == LogCodes.StepFailed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MultipleVideoSteps_FinalizeEveryRecorderAndAttributeEachResult(bool failFirst)
    {
        var capture = new DesktopDuplicationStep { Id = "capture" };
        var first = Video("first", capture.Id); var second = Video("second", capture.Id);
        var job = new Job { Name = "two recordings", Steps = [capture, first, second] };
        var recordings = new List<Recorder>();
        var builder = new JobExecutorTestBuilder().WithJobs(job); builder.DesktopCapture = new Images();
        builder.VideoRecorderFactory = (_, _, _) => { var recorder = new Recorder(failFirst && recordings.Count == 0); recordings.Add(recorder); return recorder; };
        using var executor = await builder.BuildAsync();
        await executor.ExecuteJob(job.Id);
        Assert.Equal(2, recordings.Count);
        Assert.All(recordings, recorder => { Assert.True(recorder.Stopped); Assert.True(recorder.Disposed); Assert.Equal(1, recorder.SubmittedFrameCount); });
        Assert.Equal(!failFirst, Assert.Single(builder.Logs.Completions).Success);
        Assert.Contains(builder.Logs.Entries, entry => entry.StepId == second.Id && entry.Message == "Videoaufnahme gespeichert.");
        Assert.Equal(!failFirst, builder.Logs.Entries.Any(entry => entry.StepId == first.Id && entry.Message == "Videoaufnahme gespeichert."));
    }

    private static ShowTextStep Text(string text) => new() { Settings = new() { Text = text } };
    private static VideoCreationStep Video(string id, string capture) => new()
    {
        Id = id,
        Settings = new() { SavePath = Path.GetTempPath(), FileName = id + ".mp4", ImageSource = ResultBinding.ForStepResult(capture, "image") }
    };
    private sealed class Images : IDesktopCaptureService
    {
        public Task<CaptureFrame> CaptureAsync(DesktopCaptureRequest request, CancellationToken ct) => Task.FromResult(new CaptureFrame
        { Image = new Bitmap(20, 20), Bounds = new PixelRegion(0, 0, 20, 20), IsFresh = true });
        public void Dispose() { }
    }
    private sealed class RecordingOcr : IOcrService
    {
        public List<Size> Sizes { get; } = [];
        public Task<OcrRecognition> RecognizeAsync(Bitmap image, OcrRecognitionOptions options, CancellationToken ct)
        {
            Sizes.Add(image.Size);
            return Task.FromResult(new OcrRecognition("text", .9, [], [new("text", .9, new PixelRegion(4, 5, 6, 7))]));
        }
        public void Dispose() { }
    }
    private sealed class Recorder(bool fail) : IVideoRecorder
    {
        public string OutputDirectory { get; set; } = "";
        public string FileName { get; set; } = "";
        public string? OutputFilePath => Path.Combine(OutputDirectory, FileName);
        public long SubmittedFrameCount { get; private set; }
        public bool Stopped { get; private set; }
        public bool Disposed { get; private set; }
        public Task StartAsync(CancellationToken token) => Task.CompletedTask;
        public void AddFrame(Bitmap frame) => SubmittedFrameCount++;
        public Task StopAndSave() { Stopped = true; return fail ? Task.FromException(new IOException("encoding failed")) : Task.CompletedTask; }
        public void Dispose() => Disposed = true;
    }
}
