using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TaskAutomation.Jobs;
using TaskAutomation.Makros;
using Microsoft.Extensions.Logging;
using ImageHelperMethods;
using Point = System.Drawing.Point;

namespace TaskAutomation.Steps
{
    public sealed class KlickOnPoint3DStepHandler : JobStepHandler<KlickOnPoint3DStep, KlickOnPoint3DResult>
    {

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
                return new KlickOnPoint3DResult { WasExecuted = true, Success = false, ErrorMessage = "No detection point available" };
            }

            if (detection is not null && !detection.SourceCaptureIsFresh)
            {
                logger.LogInformation(
                    "KlickOnPoint3DStepHandler: Detection came from a cached capture frame, skipping. SourceStepId={SourceStepId}, Confidence={Confidence:F3}",
                    step.Settings.PointsSource.SourceStepId,
                    detection.Confidence);
                return new KlickOnPoint3DResult { WasExecuted = true, Success = false, ErrorMessage = "Detection came from a cached capture frame" };
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

            var globalOrigin = ResolveGlobalOrigin(step.Settings);
            var delta = new Point(target.X - globalOrigin.X, target.Y - globalOrigin.Y);
            var appliedDelta = ApplyMovementFactors(
                delta,
                step.Settings.EffectiveMovementFactorX,
                step.Settings.EffectiveMovementFactorY);

            logger.LogInformation(
                "KlickOnPoint3DStepHandler: Pixel delta (dx:{DX}, dy:{DY}), movement factors=(x:{FactorX:F3}, y:{FactorY:F3}), applied mouse delta (dx:{AppliedDX}, dy:{AppliedDY}), global origin=({OriginX},{OriginY}), target=({X},{Y}), confidence={Confidence:F3}, offset=({OffsetX},{OffsetY}), click='{Click}'",
                delta.X, delta.Y, step.Settings.EffectiveMovementFactorX,
                step.Settings.EffectiveMovementFactorY, appliedDelta.X, appliedDelta.Y,
                globalOrigin.X, globalOrigin.Y, target.X, target.Y, detection.Confidence,
                step.Settings.OffsetX, step.Settings.OffsetY, step.Settings.ClickType);

            var macro = CreateClickMacro(step.Settings, appliedDelta);
            await ctx.MakroExecutor.ExecuteMakro(macro, ctx.DxgiResources, ct);
            ctx.StepTimeouts[stepKey] = DateTime.Now;

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
                commands.Add(new MouseUpBefehl   { Button = settings.ClickType });
                commands.Add(new TimeoutBefehl   { Duration = 50 });
                commands.Add(new MouseDownBefehl { Button = settings.ClickType });
                commands.Add(new MouseUpBefehl   { Button = settings.ClickType });
            }
            else
            {
                commands.Add(new MouseDownBefehl { Button = settings.ClickType });
                commands.Add(new MouseUpBefehl   { Button = settings.ClickType });
            }

            return new Makro { Name = $"TempClick_{DateTime.Now:HHmmss}", Befehle = commands };
        }
    }
}

