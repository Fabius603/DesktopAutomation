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
    public async Task ExecuteAsync_UsesReferencedPointAsOriginWithoutCoordinateSpaceFiltering()
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
        }, "target");
        context.Results.Set<ColorDetectionStep>(new ColorDetectionResult
        {
            WasExecuted = true,
            Found = true,
            Point = new PixelPoint(100, 50),
            Confidence = 0.9,
            SourceCaptureIsFresh = true
        }, "origin");
        var step = new KlickOnPoint3DStep
        {
            Settings = new KlickOnPoint3DSettings
            {
                OriginMonitorIndex = 99,
                OriginCoordinateSpace = KlickOnPoint3DSettings.MonitorLocalCoordinates,
                OriginSource = new ResultBinding { SourceStepId = "origin", PropertyPath = "Point" },
                PointsSource = new ResultBinding { SourceStepId = "target", PropertyPath = "Point" },
                MovementFactorX = 1,
                MovementFactorY = 1,
                ClickType = "none"
            }
        };

        var result = Assert.IsType<KlickOnPoint3DResult>(
            await new KlickOnPoint3DStepHandler().ExecuteAsync(step, context, default));

        Assert.True(result.Success);
        Assert.Equal(30, result.DeltaX);
        Assert.Equal(25, result.DeltaY);
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

    [Fact]
    public async Task ExecuteAsync_BlocksSameMovementAcrossNewFramesUntilDifferentMovementIsSent()
    {
        var macros = new RecordingMakroExecutor();
        var context = new PipelineContextStub { MakroExecutor = macros };
        var step = MovementStep("move");
        var handler = new KlickOnPoint3DStepHandler(() => 100);
        SetPoint(context, 120, 40, 1);
        var first = Assert.IsType<KlickOnPoint3DResult>(await handler.ExecuteAsync(step, context, default));
        Assert.False(first.MovementBlocked);

        for (var version = 2; version <= 4; version++)
        {
            ((JobResultStore)context.Results).RetainOnly([]);
            SetPoint(context, 120, 40, version);
            var blocked = Assert.IsType<KlickOnPoint3DResult>(await handler.ExecuteAsync(step, context, default));
            Assert.True(blocked.Success);
            Assert.True(blocked.MovementBlocked);
            Assert.Null(blocked.AppliedDeltaX);
            Assert.Single(macros.Macros);
        }

        SetPoint(context, 121, 40, 5);
        Assert.False(Assert.IsType<KlickOnPoint3DResult>(
            await handler.ExecuteAsync(step, context, default)).MovementBlocked);
        SetPoint(context, 120, 40, 6);
        Assert.False(Assert.IsType<KlickOnPoint3DResult>(
            await handler.ExecuteAsync(step, context, default)).MovementBlocked);
        Assert.Equal(3, macros.Macros.Count);
        Assert.All(macros.Macros, macro =>
        {
            Assert.Contains(macro.Befehle, command => command is MouseDownBefehl);
            Assert.Contains(macro.Befehle, command => command is MouseUpBefehl);
        });
    }

    [Fact]
    public async Task ExecuteAsync_EqualityUsesAppliedMovementAfterRounding()
    {
        var macros = new RecordingMakroExecutor();
        var context = new PipelineContextStub { MakroExecutor = macros };
        var step = MovementStep("move");
        step.Settings.MovementFactorX = 0.1;
        var handler = new KlickOnPoint3DStepHandler(() => 100);
        SetPoint(context, 120, 40, 1);
        await handler.ExecuteAsync(step, context, default);
        SetPoint(context, 121, 40, 2);
        var result = Assert.IsType<KlickOnPoint3DResult>(await handler.ExecuteAsync(step, context, default));
        Assert.True(result.MovementBlocked);
        Assert.Single(macros.Macros);
    }

    [Fact]
    public async Task ExecuteAsync_MissingStaleAndTimedOutResultsDoNotReleaseMovementBlock()
    {
        var macros = new RecordingMakroExecutor();
        var context = new PipelineContextStub { MakroExecutor = macros };
        var step = MovementStep("move");
        var handler = new KlickOnPoint3DStepHandler(() => 100);
        SetPoint(context, 120, 40, 1);
        await handler.ExecuteAsync(step, context, default);
        context.Results.Set<ColorDetectionStep>(new ColorDetectionResult { WasExecuted = true }, "detection");
        await handler.ExecuteAsync(step, context, default);
        SetPoint(context, 200, 80, 2, fresh: false);
        await handler.ExecuteAsync(step, context, default);
        step.Settings.TimeoutMs = int.MaxValue;
        // A future timestamp makes the existing cooldown deterministic without sleeping.
        context.StepTimeouts["KlickOnPoint3D_move"] = DateTime.MaxValue;
        SetPoint(context, 200, 80, 3);
        await handler.ExecuteAsync(step, context, default);
        context.StepTimeouts.Clear();
        step.Settings.TimeoutMs = 0;
        SetPoint(context, 120, 40, 4);
        Assert.True(Assert.IsType<KlickOnPoint3DResult>(
            await handler.ExecuteAsync(step, context, default)).MovementBlocked);
        Assert.Single(macros.Macros);
    }

    [Fact]
    public async Task ExecuteAsync_MovementStateIsIndependentForStepsAndJobRuns()
    {
        var macros = new RecordingMakroExecutor();
        var context = new PipelineContextStub { MakroExecutor = macros };
        SetPoint(context, 120, 40, 1);
        var handler = new KlickOnPoint3DStepHandler(() => 100);
        await handler.ExecuteAsync(MovementStep("one"), context, default);
        await handler.ExecuteAsync(MovementStep("two"), context, default);
        var restarted = new PipelineContextStub { MakroExecutor = macros };
        SetPoint(restarted, 120, 40, 1);
        await handler.ExecuteAsync(MovementStep("one"), restarted, default);
        Assert.Equal(3, macros.Macros.Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteAsync_FailedOrCancelledDifferentMovementDoesNotReleaseBlock(bool cancel)
    {
        var macros = new RecordingMakroExecutor();
        var context = new PipelineContextStub { MakroExecutor = macros };
        var step = MovementStep("move");
        long now = 100;
        var handler = new KlickOnPoint3DStepHandler(() => now);
        SetPoint(context, 120, 40, 1);
        await handler.ExecuteAsync(step, context, default);
        SetPoint(context, 200, 80, 2);
        now = 200;
        macros.Error = cancel ? new OperationCanceledException() : new InvalidOperationException("Input failed");
        await Assert.ThrowsAnyAsync<Exception>(() => handler.ExecuteAsync(step, context, default));
        Assert.Equal(100, context.Last3DInputTimestamps[step.Id]);
        macros.Error = null;
        SetPoint(context, 120, 40, 3);
        Assert.True(Assert.IsType<KlickOnPoint3DResult>(
            await handler.ExecuteAsync(step, context, default)).MovementBlocked);
        Assert.Single(macros.Macros);
    }

    [Theory]
    [InlineData(900)]
    [InlineData(1000)]
    public async Task ExecuteAsync_FreshButPreInputFrameCannotSendDifferentMovementOrReleaseBlock(long frameTimestamp)
    {
        var macros = new RecordingMakroExecutor();
        var context = new PipelineContextStub { MakroExecutor = macros };
        var step = MovementStep("move");
        long now = 1000;
        var handler = new KlickOnPoint3DStepHandler(() => now);
        SetPoint(context, 120, 40, 1);
        await handler.ExecuteAsync(step, context, default);

        ((JobResultStore)context.Results).RetainOnly([]);
        SetPoint(context, 120, 42, 2, timestamp: frameTimestamp);
        var blocked = Assert.IsType<KlickOnPoint3DResult>(await handler.ExecuteAsync(step, context, default));
        Assert.True(blocked.Success);
        Assert.True(blocked.MovementBlocked);
        Assert.Null(blocked.AppliedDeltaX);
        Assert.Single(macros.Macros);
        Assert.Equal(1000, context.Last3DInputTimestamps[step.Id]);

        // A later frame with the old offset remains subject to equality suppression.
        SetPoint(context, 120, 40, 3, timestamp: 1001);
        Assert.True(Assert.IsType<KlickOnPoint3DResult>(
            await handler.ExecuteAsync(step, context, default)).MovementBlocked);

        // A later frame with a different offset releases the block without sleeping.
        now = 1100;
        SetPoint(context, 120, 42, 4, timestamp: 1002);
        Assert.False(Assert.IsType<KlickOnPoint3DResult>(
            await handler.ExecuteAsync(step, context, default)).MovementBlocked);
        Assert.Equal(2, macros.Macros.Count);
        Assert.Equal(1100, context.Last3DInputTimestamps[step.Id]);
    }

    [Fact]
    public async Task ExecuteAsync_UnknownFrameTimestampPreservesLegacyMovementBehavior()
    {
        var macros = new RecordingMakroExecutor();
        var context = new PipelineContextStub { MakroExecutor = macros };
        var handler = new KlickOnPoint3DStepHandler(() => 1000);
        var step = MovementStep("move");
        SetPoint(context, 120, 40, 1, timestamp: 0);
        await handler.ExecuteAsync(step, context, default);
        SetPoint(context, 120, 42, 2, timestamp: 0);
        var result = Assert.IsType<KlickOnPoint3DResult>(await handler.ExecuteAsync(step, context, default));
        Assert.False(result.MovementBlocked);
        Assert.Equal(2, macros.Macros.Count);
    }

    [Theory]
    [InlineData(130, 40, true)]
    [InlineData(110, 30, true)]
    [InlineData(130, 50, true)]
    [InlineData(131, 40, false)]
    [InlineData(120, 51, false)]
    [InlineData(109, 40, false)]
    public async Task ExecuteAsync_ThresholdUsesInclusiveDifferenceOnBothAppliedAxes(int x, int y, bool blocked)
    {
        var macros = new RecordingMakroExecutor();
        var context = new PipelineContextStub { MakroExecutor = macros };
        var step = MovementStep("move");
        step.Settings.MovementThresholdPixels = 10;
        var handler = new KlickOnPoint3DStepHandler(() => 100);
        SetPoint(context, 120, 40, 1);
        await handler.ExecuteAsync(step, context, default);
        SetPoint(context, x, y, 2);
        var result = Assert.IsType<KlickOnPoint3DResult>(await handler.ExecuteAsync(step, context, default));
        Assert.Equal(blocked, result.MovementBlocked);
        Assert.Equal(blocked ? 1 : 2, macros.Macros.Count);
    }

    [Fact]
    public async Task ExecuteAsync_BlockedOffsetsDoNotMoveThresholdBaseline()
    {
        var macros = new RecordingMakroExecutor();
        var context = new PipelineContextStub { MakroExecutor = macros };
        var step = MovementStep("move");
        step.Settings.MovementThresholdPixels = 10;
        var handler = new KlickOnPoint3DStepHandler(() => 100);
        SetPoint(context, 120, 40, 1);
        await handler.ExecuteAsync(step, context, default);
        SetPoint(context, 128, 40, 2);
        Assert.True(Assert.IsType<KlickOnPoint3DResult>(await handler.ExecuteAsync(step, context, default)).MovementBlocked);
        SetPoint(context, 136, 40, 3);
        Assert.False(Assert.IsType<KlickOnPoint3DResult>(await handler.ExecuteAsync(step, context, default)).MovementBlocked);
        SetPoint(context, 120, 40, 4);
        Assert.False(Assert.IsType<KlickOnPoint3DResult>(await handler.ExecuteAsync(step, context, default)).MovementBlocked);
        Assert.Equal(3, macros.Macros.Count);
    }

    [Fact]
    public async Task ExecuteAsync_ThresholdComparesScaledOffsetsAndHandlesExtremeDifferences()
    {
        var macros = new RecordingMakroExecutor();
        var context = new PipelineContextStub { MakroExecutor = macros };
        var step = MovementStep("move");
        step.Settings.MovementThresholdPixels = 10;
        step.Settings.MovementFactorX = 2;
        var handler = new KlickOnPoint3DStepHandler(() => 100);
        SetPoint(context, 120, 40, 1);
        await handler.ExecuteAsync(step, context, default);
        SetPoint(context, 126, 40, 2);
        Assert.False(Assert.IsType<KlickOnPoint3DResult>(await handler.ExecuteAsync(step, context, default)).MovementBlocked);

        step.Settings.MovementFactorX = 1;
        context.Last3DMovements[step.Id] = new PixelPoint(int.MinValue, 40);
        SetPoint(context, int.MaxValue, 40, 3);
        Assert.False(Assert.IsType<KlickOnPoint3DResult>(await handler.ExecuteAsync(step, context, default)).MovementBlocked);
    }

    private static KlickOnPoint3DStep MovementStep(string id) => new()
    {
        Id = id,
        Settings = new KlickOnPoint3DSettings
        {
            OriginX = 0,
            OriginY = 0,
            MovementFactorX = 1,
            MovementFactorY = 1,
            TimeoutMs = 0,
            MovementThresholdPixels = 0,
            ClickType = "left",
            PointsSource = new ResultBinding { SourceStepId = "detection", PropertyPath = "Point" }
        }
    };

    private static void SetPoint(PipelineContextStub context, int x, int y, long version, bool fresh = true, long? timestamp = null) =>
        context.Results.Set<ColorDetectionStep>(new ColorDetectionResult
        {
            WasExecuted = true,
            Found = true,
            Point = new PixelPoint(x, y),
            SourceCaptureIsFresh = fresh,
            SourceFrameVersion = version,
            SourceFrameTimestamp = timestamp ?? version * 100,
            SourceCaptureTimestampUtc = new DateTime(2026, 10, 6, 12, 0, 0, DateTimeKind.Utc).AddMilliseconds(version)
        }, "detection");

    private sealed class RecordingMakroExecutor : IMakroExecutor
    {
        public List<Makro> Macros { get; } = [];
        public Exception? Error { get; set; }

        public Task ExecuteMakro(Makro makro, ImageHelperMethods.DxgiResources dxgi, CancellationToken ct)
        {
            if (Error is not null) return Task.FromException(Error);
            Macros.Add(makro);
            return Task.CompletedTask;
        }
    }
}
