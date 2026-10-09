using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TaskAutomation.Jobs;
using TaskAutomation.Makros;
using Microsoft.Extensions.Logging;
using ImageHelperMethods;
using TaskAutomation.Contracts.Geometry;
using Point = System.Drawing.Point;

namespace TaskAutomation.Steps
{
    public sealed class KlickOnPoint3DStepHandler : JobStepHandler<KlickOnPoint3DStep, KlickOnPoint3DResult>
    {
        private readonly Func<long> _getTimestamp;

        public KlickOnPoint3DStepHandler() : this(Stopwatch.GetTimestamp) { }

        internal KlickOnPoint3DStepHandler(Func<long> getTimestamp) => _getTimestamp = getTimestamp;

        protected override async Task<KlickOnPoint3DResult> ExecuteCoreAsync(
            KlickOnPoint3DStep step, IStepPipelineContext ctx, CancellationToken ct)
        {
            var logger = ctx.Logger;

            var resolved = ResultBindingResolver.ResolvePoints(ctx.Results, step.Settings.PointsSource);
            var detection = resolved.SourceResult as IDetectionStepResult;
            var selectedPoint = resolved.FirstOrDefault;
            if (!resolved.IsSuccess)
            {
                logger.LogInformation(
                    "KlickOnPoint3DStepHandler: No detection point available, skipping. SourceStepId={SourceStepId}, WasExecuted={WasExecuted}, Found={Found}",
                    step.Settings.PointsSource.SourceStepId,
                    resolved.SourceResult?.WasExecuted == true,
                    detection?.Found == true);
                return new KlickOnPoint3DResult
                {
                    WasExecuted = true,
                    Success = false,
                    ErrorMessage = "No detection point available",
                    SkipReason = ResultBindingResolver.IsExpectedEmpty(resolved.Status) ? "NoInput" : null
                };
            }

            if (detection is not null && !detection.SourceCaptureIsFresh)
            {
                logger.LogInformation(
                    "KlickOnPoint3DStepHandler: Detection came from a cached capture frame, skipping. SourceStepId={SourceStepId}, Confidence={Confidence:F3}",
                    step.Settings.PointsSource.SourceStepId,
                    detection.Confidence);
                return new KlickOnPoint3DResult { WasExecuted = true, Success = false, ErrorMessage = "Detection came from a cached capture frame", SkipReason = "CachedCapture" };
            }

            var stepKey = $"KlickOnPoint3D_{step.Id}";
            if (ctx.StepTimeouts.TryGetValue(stepKey, out var last))
            {
                var elapsed = DateTime.Now - last;
                if (elapsed.TotalMilliseconds < step.Settings.TimeoutMs)
                {
                    logger.LogDebug("KlickOnPoint3DStepHandler: Timeout not elapsed ({R:F0}ms remaining), skipping",
                        step.Settings.TimeoutMs - elapsed.TotalMilliseconds);
                    return new KlickOnPoint3DResult { WasExecuted = true, Success = true };
                }
            }

            await PredictionTimingHelper.WaitUntilPredictionTimeAsync(detection, logger, ct).ConfigureAwait(false);

            var target = new Point(
                selectedPoint.X + step.Settings.OffsetX,
                selectedPoint.Y + step.Settings.OffsetY);

            var globalOrigin = ResolveGlobalOrigin(step.Settings, ctx.Results);
            var delta = new Point(target.X - globalOrigin.X, target.Y - globalOrigin.Y);
            var appliedDelta = ApplyMovementFactors(
                delta,
                step.Settings.EffectiveMovementFactorX,
                step.Settings.EffectiveMovementFactorY);

            var movement = new PixelPoint(appliedDelta.X, appliedDelta.Y);
            // A newly acquired DXGI frame may still have been presented before the
            // previous input completed. IsFresh alone does not establish this order.
            if (detection is not null && detection.SourceFrameTimestamp > 0
                && ctx.Last3DInputTimestamps.TryGetValue(step.Id, out var inputTimestamp)
                && detection.SourceFrameTimestamp <= inputTimestamp)
            {
                logger.LogDebug(
                    "KlickOnPoint3DStepHandler: Pre-input frame blocked. FrameVersion={FrameVersion}, FrameTimestamp={FrameTimestamp}, InputTimestamp={InputTimestamp}",
                    detection.SourceFrameVersion, detection.SourceFrameTimestamp, inputTimestamp);
                return new KlickOnPoint3DResult
                {
                    WasExecuted = true,
                    Success = true,
                    MovementBlocked = true,
                    DeltaX = delta.X,
                    DeltaY = delta.Y,
                    MovementFactorX = step.Settings.EffectiveMovementFactorX,
                    MovementFactorY = step.Settings.EffectiveMovementFactorY
                };
            }

            if (step.Settings.MovementThresholdPixels < 0)
                throw new InvalidOperationException("Movement threshold must be non-negative.");

            if (ctx.Last3DMovements.TryGetValue(step.Id, out var previous)
                && Math.Abs((long)previous.X - movement.X) <= step.Settings.MovementThresholdPixels
                && Math.Abs((long)previous.Y - movement.Y) <= step.Settings.MovementThresholdPixels)
            {
                logger.LogDebug(
                    "KlickOnPoint3DStepHandler: Similar consecutive movement blocked (dx:{DX}, dy:{DY}), threshold={Threshold}px.",
                    movement.X, movement.Y, step.Settings.MovementThresholdPixels);
                return new KlickOnPoint3DResult
                {
                    WasExecuted = true,
                    Success = true,
                    MovementBlocked = true,
                    DeltaX = delta.X,
                    DeltaY = delta.Y,
                    MovementFactorX = step.Settings.EffectiveMovementFactorX,
                    MovementFactorY = step.Settings.EffectiveMovementFactorY
                };
            }

            logger.LogInformation(
                "KlickOnPoint3DStepHandler: Pixel delta (dx:{DX}, dy:{DY}), movement factors=(x:{FactorX:F3}, y:{FactorY:F3}), applied mouse delta (dx:{AppliedDX}, dy:{AppliedDY}), global origin=({OriginX},{OriginY}), target=({X},{Y}), confidence={Confidence:F3}, offset=({OffsetX},{OffsetY}), click='{Click}'",
                delta.X, delta.Y, step.Settings.EffectiveMovementFactorX,
                step.Settings.EffectiveMovementFactorY, appliedDelta.X, appliedDelta.Y,
                globalOrigin.X, globalOrigin.Y, target.X, target.Y, detection?.Confidence ?? 0,
                step.Settings.OffsetX, step.Settings.OffsetY, step.Settings.ClickType);

            var macro = CreateClickMacro(step.Settings, appliedDelta);
            await ctx.MakroExecutor.ExecuteMakro(macro, ctx.DxgiResources, ct);
            var completedTimestamp = _getTimestamp();
            ctx.Last3DMovements[step.Id] = movement;
            ctx.Last3DInputTimestamps[step.Id] = completedTimestamp;
            ctx.StepTimeouts[stepKey] = DateTime.Now;
            logger.LogDebug(
                "KlickOnPoint3DStepHandler: Input sent. FrameVersion={FrameVersion}, FrameTimestamp={FrameTimestamp}, InputTimestamp={InputTimestamp}",
                detection?.SourceFrameVersion ?? 0, detection?.SourceFrameTimestamp ?? 0, completedTimestamp);

            return new KlickOnPoint3DResult
            {
                WasExecuted = true,
                Success = true,
                DeltaX = delta.X,
                DeltaY = delta.Y,
                MovementFactorX = step.Settings.EffectiveMovementFactorX,
                MovementFactorY = step.Settings.EffectiveMovementFactorY,
                AppliedDeltaX = appliedDelta.X,
                AppliedDeltaY = appliedDelta.Y
            };
        }

        protected override KlickOnPoint3DResult CreateDefault() => KlickOnPoint3DResult.Default;

        internal static Point ResolveGlobalOrigin(KlickOnPoint3DSettings settings)
        {
            if (!string.Equals(
                    settings.OriginCoordinateSpace,
                    KlickOnPoint3DSettings.MonitorLocalCoordinates,
                    StringComparison.OrdinalIgnoreCase))
                return new Point(settings.OriginPoint.X, settings.OriginPoint.Y);

            var monitorBounds = ScreenHelper.GetDesktopBounds(settings.OriginMonitorIndex);
            return ResolveGlobalOrigin(settings, monitorBounds);
        }

        internal static Point ResolveGlobalOrigin(KlickOnPoint3DSettings settings, IJobResultStore results)
        {
            if (settings.OriginSource.IsConfigured)
            {
                var resolved = ResultBindingResolver.Resolve<PixelPoint>(results, settings.OriginSource);
                if (!resolved.IsSuccess)
                    throw new InvalidOperationException(resolved.Error ?? "The origin point could not be resolved.");
                return new Point(resolved.FirstOrDefault.X, resolved.FirstOrDefault.Y);
            }
            return ResolveGlobalOrigin(settings);
        }

        internal static Point ResolveGlobalOrigin(
            KlickOnPoint3DSettings settings,
            System.Drawing.Rectangle monitorBounds)
        {
            if (!string.Equals(
                    settings.OriginCoordinateSpace,
                    KlickOnPoint3DSettings.MonitorLocalCoordinates,
                    StringComparison.OrdinalIgnoreCase))
                return new Point(settings.OriginPoint.X, settings.OriginPoint.Y);

            if (monitorBounds.IsEmpty)
                throw new InvalidOperationException(
                    $"Origin monitor {settings.OriginMonitorIndex} is not available.");

            return new Point(
                monitorBounds.Left + settings.OriginX,
                monitorBounds.Top + settings.OriginY);
        }

        internal static Point ApplyMovementFactors(
            Point delta,
            double movementFactorX,
            double movementFactorY)
        {
            if (!double.IsFinite(movementFactorX) || movementFactorX is < 0.01 or > 100
                || !double.IsFinite(movementFactorY) || movementFactorY is < 0.01 or > 100)
                throw new InvalidOperationException("Movement factors must be between 0.01 and 100.");

            return new Point(
                checked((int)Math.Round(delta.X * movementFactorX, MidpointRounding.AwayFromZero)),
                checked((int)Math.Round(delta.Y * movementFactorY, MidpointRounding.AwayFromZero)));
        }

        private static Makro CreateClickMacro(KlickOnPoint3DSettings settings, Point delta)
        {
            var commands = new ObservableCollection<MakroBefehl>
            {
                new MouseMoveRelativeBefehl { DeltaX = delta.X, DeltaY = delta.Y }
            };

            if (settings.ClickType == "none")
                return new Makro { Name = $"TempMove_{DateTime.Now:HHmmss}", Befehle = commands };

            if (settings.DoubleClick)
            {
                commands.Add(new MouseDownBefehl { Button = settings.ClickType });
                commands.Add(new MouseUpBefehl { Button = settings.ClickType });
                commands.Add(new TimeoutBefehl { Duration = 50 });
                commands.Add(new MouseDownBefehl { Button = settings.ClickType });
                commands.Add(new MouseUpBefehl { Button = settings.ClickType });
            }
            else
            {
                commands.Add(new MouseDownBefehl { Button = settings.ClickType });
                commands.Add(new MouseUpBefehl { Button = settings.ClickType });
            }

            return new Makro { Name = $"TempClick_{DateTime.Now:HHmmss}", Befehle = commands };
        }
    }
}

