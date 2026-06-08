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
using Point = System.Drawing.Point;

namespace TaskAutomation.Steps
{
    public sealed class KlickOnPoint3DStepHandler : JobStepHandler<KlickOnPoint3DStep, TaskResult>
    {

        protected override async Task<TaskResult> ExecuteCoreAsync(
            KlickOnPoint3DStep step, IStepPipelineContext ctx, CancellationToken ct)
        {
            var logger = ctx.Logger;

            var detection = ctx.Results.GetById<DetectionResult>(step.Settings.SourceDetectionStepId);
            if (!detection.WasExecuted || !detection.Found || detection.Point is null)
            {
                logger.LogInformation(
                    "KlickOnPoint3DStepHandler: No detection point available, skipping. SourceStepId={SourceStepId}, WasExecuted={WasExecuted}, Found={Found}",
                    step.Settings.SourceDetectionStepId,
                    detection.WasExecuted,
                    detection.Found);
                return new TaskResult { WasExecuted = true, Success = false, ErrorMessage = "No detection point available" };
            }

            var stepKey = $"KlickOnPoint3D_{step.Id}";
            if (ctx.StepTimeouts.TryGetValue(stepKey, out var last))
            {
                var elapsed = DateTime.Now - last;
                if (elapsed.TotalMilliseconds < step.Settings.TimeoutMs)
                {
                    logger.LogDebug("KlickOnPoint3DStepHandler: Timeout not elapsed ({R:F0}ms remaining), skipping",
                        step.Settings.TimeoutMs - elapsed.TotalMilliseconds);
                    return new TaskResult { WasExecuted = true, Success = true };
                }
            }
            ctx.StepTimeouts[stepKey] = DateTime.Now;

            var target = new Point(
                detection.Point.Value.X + step.Settings.OffsetX,
                detection.Point.Value.Y + step.Settings.OffsetY);

            // Compute relative delta from configured origin to detected target.
            // Origin is the user-set reference point (e.g. screen center / crosshair).
            var origin = new Point(step.Settings.OriginX, step.Settings.OriginY);
            var delta = new Point(target.X - origin.X, target.Y - origin.Y);

            logger.LogInformation(
                "KlickOnPoint3DStepHandler: Moving mouse from origin ({OriginX},{OriginY}) by (dx:{DX}, dy:{DY}) to detected target ({X},{Y}), confidence={Confidence:F3}, offset=({OffsetX},{OffsetY}), click='{Click}'",
                origin.X, origin.Y, delta.X, delta.Y, target.X, target.Y, detection.Confidence, step.Settings.OffsetX, step.Settings.OffsetY, step.Settings.ClickType);

            var macro = CreateClickMacro(step.Settings, origin, delta);
            await ctx.MakroExecutor.ExecuteMakro(macro, ctx.DxgiResources, ct);

            return new TaskResult { WasExecuted = true, Success = true };
        }

        protected override TaskResult CreateDefault() => TaskResult.Default;

        private static Makro CreateClickMacro(KlickOnPoint3DSettings settings, Point origin, Point delta)
        {
            var commands = new ObservableCollection<MakroBefehl>
            {
                // new MouseMoveAbsoluteBefehl { X = origin.X, Y = origin.Y },
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

