using TaskAutomation.Jobs;
using TaskAutomation.Steps;
using TaskAutomation.Tests.TestDoubles;

namespace TaskAutomation.Tests.Steps;

public sealed class ShowTextStepHandlerTests
{
    [Fact]
    public async Task ExecuteAsync_ExplicitText_ForwardsAllDisplaySettings()
    {
        var overlay = new RecordingDesktopResultOverlay();
        var context = new PipelineContextStub { DesktopResultOverlay = overlay };
        var step = new ShowTextStep
        {
            Id = "text-step",
            Settings = new ShowTextSettings
            {
                Text = "Hallo", FontSize = 18, FontColor = "#123456", Opacity = .5f,
                DesktopIndex = 2, OffsetX = 10, OffsetY = 20, DurationMs = 500, ClearOnJobEnd = true
            }
        };
        var result = Assert.IsType<ShowTextResult>(await new ShowTextStepHandler().ExecuteAsync(step, context, default));
        var call = Assert.Single(overlay.TextCalls);
        Assert.True(result.Success);
        Assert.Equal("text-step", call.StepKey);
        Assert.Equal("Hallo", call.Text);
        Assert.Equal((byte)0x12, call.R);
        Assert.Equal((byte)0x34, call.G);
        Assert.Equal((byte)0x56, call.B);
        Assert.Equal((byte)127, call.A);
        Assert.Equal(2, call.DesktopIndex);
        Assert.Equal(500, call.DurationMs);
        Assert.True(call.ClearOnJobEnd);
    }

    [Theory]
    [InlineData("#FF0000", 255, 0, 0)]
    [InlineData("FF0000", 255, 0, 0)]
    [InlineData("#80112233", 17, 34, 51)]
    [InlineData("invalid", 255, 255, 255)]
    public async Task ExecuteAsync_ParsesSupportedColorsOrFallsBackToWhite(string color, byte r, byte g, byte b)
    {
        var overlay = new RecordingDesktopResultOverlay();
        var context = new PipelineContextStub { DesktopResultOverlay = overlay };
        var step = new ShowTextStep { Settings = new() { Text = "x", FontColor = color } };
        await new ShowTextStepHandler().ExecuteAsync(step, context, default);
        var call = Assert.Single(overlay.TextCalls);
        Assert.Equal((r, g, b), (call.R, call.G, call.B));
    }

    [Fact]
    public async Task ExecuteAsync_TaskResult_UsesCurrentValueOnEveryExecution()
    {
        var overlay = new RecordingDesktopResultOverlay();
        var context = new PipelineContextStub { DesktopResultOverlay = overlay };
        var step = new ShowTextStep
        {
            Id = "display",
            Settings = new()
            {
                TextSource = ShowTextSource.TaskResult,
                TextResult = new() { SourceStepId = "audio", PropertyPath = "Percentage" }
            }
        };
        var handler = new ShowTextStepHandler();
        context.Results.Set<WindowsStateQueryStep>(new WindowsStateQueryResult { WasExecuted = true, Percentage = 25 }, "audio");
        await handler.ExecuteAsync(step, context, default);
        context.Results.Set<WindowsStateQueryStep>(new WindowsStateQueryResult { WasExecuted = true, Percentage = 70 }, "audio");
        await handler.ExecuteAsync(step, context, default);
        Assert.Equal(["25", "70"], overlay.TextCalls.Select(call => call.Text));
    }

    [Fact]
    public async Task ExecuteAsync_TaskResultBoolean_FormatsCurrentCultureValue()
    {
        var overlay = new RecordingDesktopResultOverlay();
        var context = new PipelineContextStub { DesktopResultOverlay = overlay };
        context.Results.Set<WindowsStateQueryStep>(new WindowsStateQueryResult { WasExecuted = true, IsMuted = true }, "audio");
        var step = new ShowTextStep { Settings = new() { TextSource = ShowTextSource.TaskResult,
            TextResult = new() { SourceStepId = "audio", PropertyPath = "IsMuted" } } };
        await new ShowTextStepHandler().ExecuteAsync(step, context, default);
        Assert.Equal(bool.TrueString, Assert.Single(overlay.TextCalls).Text);
    }

    [Fact]
    public async Task ExecuteAsync_UnavailableTaskResult_ThrowsAndDoesNotRender()
    {
        var overlay = new RecordingDesktopResultOverlay();
        var context = new PipelineContextStub { DesktopResultOverlay = overlay };
        var step = new ShowTextStep { Settings = new() { TextSource = ShowTextSource.TaskResult,
            TextResult = new() { SourceStepId = "missing", PropertyPath = "Text" } } };
        await Assert.ThrowsAsync<InvalidOperationException>(() => new ShowTextStepHandler().ExecuteAsync(step, context, default));
        Assert.Empty(overlay.TextCalls);
    }
}
