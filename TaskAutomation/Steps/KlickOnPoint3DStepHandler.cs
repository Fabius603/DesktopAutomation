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
            if (!detection.Found || detection.Point is null)
            {
                logger.LogInformation("KlickOnPoint3DStepHandler: No detection point available, skipping");
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

            var target = detection.Point.Value;

            // Compute relative delta from configured origin to detected target.
            // Origin is the user-set reference point (e.g. screen center / crosshair).
            var origin = new Point(step.Settings.OriginX, step.Settings.OriginY);
            var delta = new Point(target.X - origin.X, target.Y - origin.Y);

            logger.LogInformation(
                "KlickOnPoint3DStepHandler: Moving mouse (dx:{DX}, dy:{DY}) to ({X},{Y}), click='{Click}'",
                delta.X, delta.Y, target.X, target.Y, step.Settings.ClickType);

            var macro = CreateClickMacro(step.Settings, delta);
            await ctx.MakroExecutor.ExecuteMakro(macro, ctx.DxgiResources, ct);

            return new TaskResult { WasExecuted = true, Success = true };
        }

        protected override TaskResult CreateDefault() => TaskResult.Default;

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

