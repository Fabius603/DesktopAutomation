using Microsoft.Extensions.Logging;
using TaskAutomation.Jobs;
using TaskAutomation.Logging;
using TaskAutomation.Makros;
using TaskAutomation.Steps;
using TaskAutomation.Tests.TestDoubles;

namespace TaskAutomation.Tests.Logging;

public sealed class ExpectedEmptyInputTests
{
    [Fact]
    public async Task CachedDetection_IsAnIntentionalSkipWithoutAppliedInput()
    {
        var macros = new CountingMakroExecutor();
        var context = new PipelineContextStub { MakroExecutor = macros };
        context.Results.Set<ColorDetectionStep>(new ColorDetectionResult
        {
            WasExecuted = true,
            Found = true,
            Point = new TaskAutomation.Contracts.Geometry.PixelPoint(10, 20),
            SourceCaptureIsFresh = false
        }, "source");
        var result = Assert.IsType<KlickOnPoint3DResult>(await new KlickOnPoint3DStepHandler().ExecuteAsync(
            new KlickOnPoint3DStep { Settings = new() { PointsSource = new ResultBinding { SourceStepId = "source", PropertyPath = "Point" } } }, context, default));
        Assert.Equal("CachedCapture", result.SkipReason);
        Assert.Equal(ExecutionLogLevel.Information, StepLogEvents.ResultLevel(result));
        Assert.Equal(0, macros.Count);
    }

    [Theory]
    [InlineData("Point", false, "NoInput", ExecutionLogLevel.Information)]
    [InlineData("Point", true, null, ExecutionLogLevel.Warning)]
    [InlineData("UnknownProperty", false, null, ExecutionLogLevel.Warning)]
    public async Task ClickWithoutTarget_DistinguishesEmptyDetectionFromBrokenReference(
        string property, bool missingSource, string? reason, ExecutionLogLevel level)
    {
        var macros = new CountingMakroExecutor();
        var context = new PipelineContextStub { MakroExecutor = macros };
        if (!missingSource) context.Results.Set<ColorDetectionStep>(new ColorDetectionResult { WasExecuted = true, Found = false }, "source");
        var binding = new ResultBinding { SourceStepId = "source", PropertyPath = property };
        var click = Assert.IsType<KlickOnPointResult>(await new KlickOnPointStepHandler().ExecuteAsync(
            new KlickOnPointStep { Settings = new() { PointsSource = binding } }, context, default));
        var click3D = Assert.IsType<KlickOnPoint3DResult>(await new KlickOnPoint3DStepHandler().ExecuteAsync(
            new KlickOnPoint3DStep { Settings = new() { PointsSource = binding } }, context, default));
        Assert.Equal(reason, click.SkipReason);
        Assert.Equal(reason, click3D.SkipReason);
        Assert.Equal(level, StepLogEvents.ResultLevel(click));
        Assert.Equal(level, StepLogEvents.ResultLevel(click3D));
        Assert.Equal(0, macros.Count);
    }

    [Theory]
    [InlineData("AllDetections", LogLevel.Information)]
    [InlineData("UnknownProperty", LogLevel.Warning)]
    public void Overlay_EmptyDetectionIsNormalButBrokenBindingRemainsWarning(string property, LogLevel expected)
    {
        var store = new JobResultStore();
        store.Set<ColorDetectionStep>(new ColorDetectionResult { WasExecuted = true, Found = false }, "source");
        var logger = new LevelsLogger();
        var overlay = VisualOverlayResolver.Resolve(store, new VisualOverlaySettings
        {
            DetectionResults = [new ResultBinding { SourceStepId = "source", PropertyPath = property }]
        }, null, logger);
        Assert.False(overlay.HasContent);
        Assert.Equal(expected, Assert.Single(logger.Levels));
    }

    private sealed class LevelsLogger : ILogger
    {
        public List<LogLevel> Levels { get; } = [];
        public bool IsEnabled(LogLevel logLevel) => true;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) => Levels.Add(logLevel);
    }

    private sealed class CountingMakroExecutor : IMakroExecutor
    {
        public int Count { get; private set; }
        public Task ExecuteMakro(Makro makro, ImageHelperMethods.DxgiResources dxgi, CancellationToken ct)
        { Count++; return Task.CompletedTask; }
    }
}
