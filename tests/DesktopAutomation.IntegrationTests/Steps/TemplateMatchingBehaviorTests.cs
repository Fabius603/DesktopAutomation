using System.Drawing;
using ImageDetection.Algorithms.TemplateMatching;
using OpenCvSharp;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;
using TaskAutomation.Steps.Definitions;
using TaskAutomation.Tests.TestDoubles;

namespace TaskAutomation.Tests.Steps;

public sealed class TemplateMatchingBehaviorTests
{
    [Theory]
    [InlineData(TemplateMatchModes.CCoeffNormed)]
    [InlineData(TemplateMatchModes.CCorrNormed)]
    [InlineData(TemplateMatchModes.SqDiffNormed)]
    public async Task ZeroThreshold_MultipleMatchesTerminateAndHaveNormalizedConfidence(TemplateMatchModes mode)
    {
        using var images = new Images();
        using var matcher = new TemplateMatching(mode);
        matcher.SetTemplate(images.TemplatePath);
        matcher.SetThreshold(0);
        matcher.EnableMultiplePoints();
        matcher.SetSuppressionRadius(0);
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var result = await Task.Run(() => matcher.Detect(images.Source, stop.Token)).WaitAsync(TimeSpan.FromSeconds(10));
        Assert.True(result.Success);
        Assert.InRange(result.AllResults.Count, 1, 49);
        Assert.Equal(result.AllResults.Count, result.AllResults.Select(item => item.CenterPoint).Distinct().Count());
        Assert.All(result.AllResults, item => Assert.InRange(item.Confidence, 0, 1));
    }

    [Fact]
    public async Task ConsecutiveSteps_ApplyEachModeAndMultiplePointsSetting()
    {
        using var images = new Images();
        using var matcher = new TemplateMatching(TemplateMatchModes.CCoeffNormed);
        var context = new PipelineContextStub { TemplateMatcher = matcher };
        context.TemplateMatcher.SetSuppressionRadius(0);
        context.Results.Set<DesktopDuplicationStep>(new DesktopDuplicationResult { WasExecuted = true, Image = images.Source }, "capture");
        var first = Step(images.TemplatePath, TemplateMatchModes.CCorrNormed, true);
        var handler = new TemplateMatchingStepHandler();
        var multiple = Assert.IsType<TemplateMatchingResult>(await handler.ExecuteAsync(first, context, default));
        first.Settings.MultiplePoints = false;
        first.Id = "second";
        var single = Assert.IsType<TemplateMatchingResult>(await handler.ExecuteAsync(first, context, default));
        Assert.Single(single.AllDetections);
        Assert.True(multiple.AllDetections.Count > 1);
        Assert.True(multiple.AllDetections.Select(item => item.Confidence).Distinct().Count() > 1);
        Assert.All(multiple.AllDetections, item => Assert.InRange(item.Confidence, 0, 1));
        Assert.Equal(single.Confidence, single.AllDetections[0].Confidence);
        using var white = new Bitmap(2, 2);
        using (var graphics = Graphics.FromImage(white)) graphics.Clear(Color.White);
        white.Save(images.TemplatePath + ".png");
        first.Settings.TemplatePath = images.TemplatePath + ".png";
        first.Settings.TemplateMatchMode = TemplateMatchModes.CCoeffNormed;
        first.Settings.ConfidenceThreshold = .99;
        Assert.True(Assert.IsType<TemplateMatchingResult>(await handler.ExecuteAsync(first, context, default)).Found);
        first.Settings.TemplateMatchMode = TemplateMatchModes.SqDiffNormed;
        Assert.False(Assert.IsType<TemplateMatchingResult>(await handler.ExecuteAsync(first, context, default)).Found);
    }

    [Fact]
    public void Editor_OffersOnlySupportedModesAndMultiplePoints()
    {
        var descriptor = new TemplateMatchingStepDefinition().Descriptor;
        var modes = Assert.Single(descriptor.Fields, field => field.Id == TemplateMatchingStepDefinition.MatchModeFieldId);
        Assert.Equal(TemplateMatching.SupportedModes.Select(mode => mode.ToString()), modes.Constraints!.AllowedValues);
        Assert.Contains(descriptor.Presentation!.EditorSections, section => section.FieldIds.Contains(TemplateMatchingStepDefinition.MultiplePointsFieldId));
    }

    [Fact]
    public void CancelledMatching_DoesNotRunNativeDetection()
    {
        using var images = new Images();
        using var matcher = new TemplateMatching(TemplateMatchModes.CCoeffNormed);
        matcher.SetTemplate(images.TemplatePath);
        using var stop = new CancellationTokenSource(); stop.Cancel();
        Assert.ThrowsAny<OperationCanceledException>(() => matcher.Detect(images.Source, stop.Token));
    }

    private static TemplateMatchingStep Step(string template, TemplateMatchModes mode, bool multiple) => new()
    {
        Settings = new()
        {
            TemplatePath = template,
            TemplateMatchMode = mode,
            MultiplePoints = multiple,
            ConfidenceThreshold = 0,
            ImageSource = ResultBinding.ForStepResult("capture", "image")
        }
    };

    private sealed class Images : IDisposable
    {
        private readonly string _directory = Path.Combine(Path.GetTempPath(), "DesktopAutomation.Tests", Guid.NewGuid().ToString("N"));
        public Bitmap Source { get; } = new(8, 8);
        public string TemplatePath => Path.Combine(_directory, "template.png");
        public Images()
        {
            Directory.CreateDirectory(_directory);
            using var template = new Bitmap(2, 2);
            template.SetPixel(0, 0, Color.Black); template.SetPixel(1, 0, Color.White);
            template.SetPixel(0, 1, Color.Gray); template.SetPixel(1, 1, Color.Red);
            template.Save(TemplatePath);
            using var graphics = Graphics.FromImage(Source);
            graphics.Clear(Color.Black); graphics.DrawImageUnscaled(template, 0, 0); graphics.DrawImageUnscaled(template, 5, 5);
        }
        public void Dispose() { Source.Dispose(); Directory.Delete(_directory, true); }
    }
}
