using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging.Abstractions;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;

namespace TaskAutomation.Tests.Jobs;

public sealed class OverlayTextLocalValueTests
{
    [Theory]
    [InlineData("show_image")]
    [InlineData("show_on_desktop")]
    [InlineData("save_image")]
    [InlineData("video_creation")]
    public void PersistedLocalOverlayText_ValidatesAndResolvesWithoutChangingItsReference(string type)
    {
        var capture = new DesktopDuplicationStep();
        var value = new LocalValue { Name = "Status", ValueKind = ResultValueKind.Text, Value = JsonValue.Create("Ready") };
        var binding = new ResultBinding { ProviderId = ValueProviderIds.LocalValue, SourceId = value.Id.ToString("D") };
        var overlay = new VisualOverlaySettings { TextResults = [new() { Result = binding }] };
        var image = ResultBinding.ForStepResult(capture.Id, "image");
        JobStep step = type switch
        {
            "show_image" => new ShowImageStep { Settings = new() { ImageSource = image, Overlay = overlay } },
            "show_on_desktop" => new ShowOnDesktopStep { Settings = new() { Overlay = overlay } },
            "save_image" => new SaveImageStep
            {
                Settings = new()
                {
                    ImageSource = image,
                    Overlay = overlay,
                    SavePath = Path.GetTempPath(),
                    FileName = "image.png"
                }
            },
            _ => new VideoCreationStep
            {
                Settings = new()
                {
                    ImageSource = image,
                    Overlay = overlay,
                    SavePath = Path.GetTempPath(),
                    FileName = "output.mp4"
                }
            }
        };
        value.OwnerStepId = step.Id;
        value.InputPath = "overlay.text_results.0.result";
        var job = new Job { Steps = [capture, step], LocalValues = [value] };
        var options = new JsonSerializerOptions();
        JobJsonSerialization.Configure(options);
        job = JsonSerializer.Deserialize<Job>(JsonSerializer.Serialize(job, options), options)!;
        step = job.Steps[1];
        var jobValidation = JobValidation.ValidateJob(job);
        Assert.True(jobValidation.IsValid, string.Join(Environment.NewLine, jobValidation.Steps.Select(item => item.Error)));
        var validation = JobValidation.ValidateStep(job.Steps, step, variables: job.LocalValues);
        Assert.True(validation.IsValid, validation.Error);
        overlay = step switch
        {
            ShowImageStep show => show.Settings.Overlay,
            ShowOnDesktopStep show => show.Settings.Overlay,
            SaveImageStep save => save.Settings.Overlay,
            VideoCreationStep video => video.Settings.Overlay,
            _ => throw new InvalidOperationException()
        };
        Assert.Equal(value.Id.ToString("D"), Assert.Single(overlay.TextResults).Result.SourceId);
        var resolved = VisualOverlayResolver.Resolve(new JobResultStore(localValues: job.LocalValues),
            overlay, null, NullLogger.Instance);
        Assert.Equal("Ready", Assert.Single(resolved.Texts).Text);
        job.LocalValues.Clear();
        Assert.False(JobValidation.ValidateStep(job.Steps, step, variables: job.LocalValues).IsValid);
    }
}
