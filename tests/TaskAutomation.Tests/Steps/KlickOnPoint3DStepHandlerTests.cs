using System.Drawing;
using TaskAutomation.Jobs;
using TaskAutomation.Makros;
using TaskAutomation.Steps;
using TaskAutomation.Tests.TestDoubles;
using TaskAutomation.Contracts.Geometry;

namespace TaskAutomation.Tests.Steps;

public sealed class KlickOnPoint3DStepHandlerTests
{
    [Fact]
    public void ResolveGlobalOrigin_MonitorLocalCoordinatesIncludeNegativeMonitorOffset()
    {
        var settings = new KlickOnPoint3DSettings
        {
            OriginX = 960,
            OriginY = 540,
            OriginMonitorIndex = 1,
            OriginCoordinateSpace = KlickOnPoint3DSettings.MonitorLocalCoordinates
        };

        var origin = KlickOnPoint3DStepHandler.ResolveGlobalOrigin(
            settings,
            new Rectangle(-1920, 0, 1920, 1080));

        Assert.Equal(new Point(-960, 540), origin);
    }

    [Fact]
    public void ResolveGlobalOrigin_LegacySettingsRemainGlobal()
    {
        var settings = new KlickOnPoint3DSettings { OriginX = -960, OriginY = 540 };

        var origin = KlickOnPoint3DStepHandler.ResolveGlobalOrigin(
            settings,
            new Rectangle(-1920, 0, 1920, 1080));

        Assert.Equal(new Point(-960, 540), origin);
    }

    [Theory]
    [InlineData(35, 15, 0.5, 2.0, 18, 30)]
    [InlineData(-35, -15, 0.5, 2.0, -18, -30)]
    [InlineData(20, -10, 1.5, 0.5, 30, -5)]
    public void ApplyMovementFactors_ScaleAxesIndependentlyWithStableRounding(
        int deltaX, int deltaY, double factorX, double factorY, int expectedX, int expectedY)
    {
        var applied = KlickOnPoint3DStepHandler.ApplyMovementFactors(
            new Point(deltaX, deltaY), factorX, factorY);

        Assert.Equal(new Point(expectedX, expectedY), applied);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(double.NaN)]
    [InlineData(101)]
    public void ApplyMovementFactors_InvalidFactorFails(double factor)
    {
        Assert.Throws<InvalidOperationException>(() =>
            KlickOnPoint3DStepHandler.ApplyMovementFactors(new Point(10, 10), factor, 1));
        Assert.Throws<InvalidOperationException>(() =>
            KlickOnPoint3DStepHandler.ApplyMovementFactors(new Point(10, 10), 1, factor));
    }

    [Fact]
    public void ResultContract_UsesStableDeltaPropertyIds()
    {
        var contract = StepResultMetadata.GetResultType(nameof(KlickOnPoint3DResult));

        Assert.Contains(contract.Properties, property =>
            property.Name == nameof(KlickOnPoint3DResult.DeltaX)
            && property.Id == "click_on_point_3d.delta_x");
        Assert.Contains(contract.Properties, property =>
            property.Name == nameof(KlickOnPoint3DResult.DeltaY)
            && property.Id == "click_on_point_3d.delta_y");
    }

    [Fact]
    public async Task ExecuteAsync_ReportsCalculatedDeltasForExecutionLog()
    {
        var macroExecutor = new RecordingMakroExecutor();
        var context = new PipelineContextStub { MakroExecutor = macroExecutor };
        context.Results.Set<TemplateMatchingStep>(new TemplateMatchingResult
        {
            WasExecuted = true,
            Found = true,
            Point = new PixelPoint(130, 75),
            Confidence = 0.9,
            SourceCaptureIsFresh = true
        }, "detection");
        var step = new KlickOnPoint3DStep
        {
            Settings = new KlickOnPoint3DSettings
            {
                OriginX = 100,
                OriginY = 50,
                OffsetX = 5,
                OffsetY = -10,
                MovementFactorX = 0.5,
                MovementFactorY = 2.0,
                ClickType = "none",
                PointsSource = new ResultBinding
                {
                    SourceStepId = "detection",
                    PropertyPath = "Point"
                }
            }
        };

        var result = Assert.IsType<KlickOnPoint3DResult>(
            await new KlickOnPoint3DStepHandler().ExecuteAsync(step, context, default));

        Assert.True(result.Success);
        Assert.Equal(35, result.DeltaX);
        Assert.Equal(15, result.DeltaY);
        Assert.Equal(0.5, result.MovementFactorX);
        Assert.Equal(2.0, result.MovementFactorY);
        Assert.Equal(18, result.AppliedDeltaX);
        Assert.Equal(30, result.AppliedDeltaY);
        var move = Assert.IsType<MouseMoveRelativeBefehl>(
            Assert.Single(Assert.Single(macroExecutor.Macros).Befehle));
        Assert.Equal((result.AppliedDeltaX, result.AppliedDeltaY), (move.DeltaX, move.DeltaY));
    }

    [Fact]
    public async Task ExecuteAsync_MissingPointDoesNotReportDeltas()
    {
        var result = Assert.IsType<KlickOnPoint3DResult>(
            await new KlickOnPoint3DStepHandler().ExecuteAsync(
                new KlickOnPoint3DStep(),
                new PipelineContextStub(),
                default));

        Assert.False(result.Success);
        Assert.Null(result.DeltaX);
        Assert.Null(result.DeltaY);
    }

    private sealed class RecordingMakroExecutor : IMakroExecutor
    {
        public List<Makro> Macros { get; } = [];

        public Task ExecuteMakro(Makro makro, ImageHelperMethods.DxgiResources dxgi, CancellationToken ct)
        {
            Macros.Add(makro);
            return Task.CompletedTask;
        }
    }
}
