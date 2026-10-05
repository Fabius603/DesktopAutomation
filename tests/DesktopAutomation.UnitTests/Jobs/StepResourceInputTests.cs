using System.Drawing;
using System.Text.Json;
using ImageCapture.Video;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;
using TaskAutomation.Tests.TestDoubles;

namespace TaskAutomation.Tests.Jobs;

public sealed class StepResourceInputTests
{
    [Fact]
    public async Task ReopenedYoloPreloadsAndUnloadsResolvedModelAndIgnoresDisabledSteps()
    {
        var capture = new DesktopDuplicationStep();
        var detection = new YOLODetectionStep
        {
            Settings = new()
            {
                Model = "saved-model",
                ClassName = "object",
                ImageSource = ResultBinding.ForStepResult(capture.Id, "image")
            }
        };
        var disabled = new YOLODetectionStep { IsEnabled = false, Settings = new() { Model = "disabled-model" } };
        var job = Roundtrip(new Job { Name = "models", Steps = [capture, detection, disabled] });
        var builder = new JobExecutorTestBuilder().WithJobs(job);
        using var executor = await builder.BuildAsync();
        await executor.ExecuteJob(job.Id);

        Assert.Equal(["saved-model"], builder.Yolo.EnsureCalls.Select(call => call.Model));
        Assert.Equal(["saved-model"], builder.Yolo.UnloadedModels);
        Assert.True(Assert.Single(builder.Logs.Completions).Success);
    }

    [Fact]
    public async Task CancelledYoloPreloadReleasesAlreadyLoadedModels()
    {
        using var cancellation = new CancellationTokenSource();
        var job = Roundtrip(new Job
        {
            Name = "preload stop",
            Steps =
            [new YOLODetectionStep { Settings = new() { Model = "saved-model", ClassName = "object" } }]
        });
        var builder = new JobExecutorTestBuilder().WithJobs(job);
        builder.Yolo.EnsureAction = _ => { cancellation.Cancel(); return Task.CompletedTask; };
        using var executor = await builder.BuildAsync();
        await executor.ExecuteJob(job.Id, cancellation.Token);
        Assert.Equal(["saved-model"], builder.Yolo.UnloadedModels);
        Assert.True(Assert.Single(builder.Logs.Completions).Cancelled);
    }

    [Fact]
    public async Task VideoStartsWithResolvedPathsAndActualFrameDimensionsAndReusesRecorder()
    {
        var capture = new DesktopDuplicationStep();
        var video = new VideoCreationStep
        {
            Settings = new()
            {
                SavePath = Path.GetTempPath(),
                FileName = "saved-name.mp4",
                ImageSource = ResultBinding.ForStepResult(capture.Id, "image")
            }
        };
        var job = Roundtrip(new Job { Steps = [capture, video] });
        video = Assert.IsType<VideoCreationStep>(job.Steps[1]);
        using var image = new Bitmap(6, 4);
        var recorder = new RecordingVideoRecorder();
        var dimensions = new List<(int Width, int Height, int Fps)>();
        var context = new PipelineContextStub(localValues: job.LocalValues)
        {
            RecorderFactory = (width, height, fps) => { dimensions.Add((width, height, fps)); return recorder; }
        };
        context.Results.Set<DesktopDuplicationStep>(new DesktopDuplicationResult { WasExecuted = true, Image = image }, capture.Id);

        video = Assert.IsType<VideoCreationStep>(StepInputMaterializer.Materialize(video, context.Results));
        await new VideoCreationStepHandler().ExecuteAsync(video, context, CancellationToken.None);
        await new VideoCreationStepHandler().ExecuteAsync(video, context, CancellationToken.None);

        Assert.Equal([(6, 4, 60)], dimensions);
        Assert.Equal(Path.GetTempPath(), recorder.OutputDirectory);
        Assert.Equal("saved-name.mp4", recorder.FileName);
        Assert.Equal(2, recorder.SubmittedFrameCount);
        Assert.Same(recorder, context.VideoRecorder);
    }

    [Fact]
    public async Task VideoWithoutAFrameDoesNotCreateARecorder()
    {
        var capture = new DesktopDuplicationStep();
        var video = new VideoCreationStep
        {
            Settings = new()
            {
                SavePath = Path.GetTempPath(),
                FileName = "empty.mp4",
                ImageSource = ResultBinding.ForStepResult(capture.Id, "image")
            }
        };
        var context = new PipelineContextStub { RecorderFactory = (_, _, _) => throw new InvalidOperationException("unexpected recorder") };
        context.Results.Set<DesktopDuplicationStep>(new DesktopDuplicationResult { WasExecuted = true }, capture.Id);
        await new VideoCreationStepHandler().ExecuteAsync(video, context, CancellationToken.None);
        Assert.Null(context.VideoRecorder);
        Assert.False(context.Results.GetById<VideoCreationResult>(video.Id).Success);
    }

    [Fact]
    public async Task FailedVideoStartDisposesRecorderAndDoesNotKeepItInContext()
    {
        var capture = new DesktopDuplicationStep();
        var video = new VideoCreationStep
        {
            Settings = new()
            {
                SavePath = Path.GetTempPath(),
                FileName = "failed.mp4",
                ImageSource = ResultBinding.ForStepResult(capture.Id, "image")
            }
        };
        using var image = new Bitmap(2, 2);
        var recorder = new RecordingVideoRecorder { FailStart = true };
        var context = new PipelineContextStub { RecorderFactory = (_, _, _) => recorder };
        context.Results.Set<DesktopDuplicationStep>(new DesktopDuplicationResult { WasExecuted = true, Image = image }, capture.Id);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new VideoCreationStepHandler().ExecuteAsync(video, context, CancellationToken.None));
        Assert.True(recorder.Disposed);
        Assert.Null(context.VideoRecorder);
    }

    private static Job Roundtrip(Job job)
    {
        JobVariableInputMigration.Migrate(job);
        var options = new JsonSerializerOptions(); JobJsonSerialization.Configure(options);
        return JsonSerializer.Deserialize<Job>(JsonSerializer.Serialize(job, options), options)!;
    }

    private sealed class RecordingVideoRecorder : IVideoRecorder
    {
        public string OutputDirectory { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string? OutputFilePath => Path.Combine(OutputDirectory, FileName);
        public long SubmittedFrameCount { get; private set; }
        public bool FailStart { get; init; }
        public bool Disposed { get; private set; }
        public Task StartAsync(CancellationToken token) => FailStart
            ? Task.FromException(new InvalidOperationException("recorder start failed")) : Task.CompletedTask;
        public void AddFrame(Bitmap frame) => SubmittedFrameCount++;
        public Task StopAndSave() => Task.CompletedTask;
        public void Dispose() => Disposed = true;
    }
}
