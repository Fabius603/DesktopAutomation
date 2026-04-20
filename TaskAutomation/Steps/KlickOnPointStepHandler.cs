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

namespace TaskAutomation.Steps
{
    public sealed class KlickOnPointStepHandler : JobStepHandler<KlickOnPointStep, TaskResult>
    {
        protected override async Task<TaskResult> ExecuteCoreAsync(
            KlickOnPointStep step, IStepPipelineContext ctx, CancellationToken ct)
        {
            var logger = ctx.Logger;
            logger.LogDebug(
                "KlickOnPointStepHandler: click='{ClickType}' double={Double} timeout={T}ms",
                step.Settings.ClickType, step.Settings.DoubleClick, step.Settings.TimeoutMs);

            // Hole den detektierten Punkt vom letzten Detection-Step
            var detection = GetDetection(ctx.Results);
            if (!detection.Found || detection.Point is null)
            {
                logger.LogInformation("KlickOnPointStepHandler: No detection point available, skipping click");
                return new TaskResult { WasExecuted = true, Success = false, ErrorMessage = "No detection point available" };
            }

            // Timeout-Check
            var stepKey = $"KlickOnPoint_{step.Id}";
            if (ctx.StepTimeouts.TryGetValue(stepKey, out var last))
            {
                var elapsed = DateTime.Now - last;
                if (elapsed.TotalMilliseconds < step.Settings.TimeoutMs)
                {
                    logger.LogDebug("KlickOnPointStepHandler: Timeout not elapsed ({R:F0}ms remaining), skipping",
                        step.Settings.TimeoutMs - elapsed.TotalMilliseconds);
                    return new TaskResult { WasExecuted = true, Success = true };
                }
            }
            ctx.StepTimeouts[stepKey] = DateTime.Now;

            var point = detection.Point.Value;
            logger.LogInformation("KlickOnPointStepHandler: Clicking at ({X},{Y})", point.X, point.Y);

            var macro = CreateClickMacro(step.Settings, point);
            await ctx.MakroExecutor.ExecuteMakro(macro, ctx.DxgiResources, ct);

            return new TaskResult { WasExecuted = true, Success = true };
        }

        protected override TaskResult CreateDefault() => TaskResult.Default;

        internal static DetectionResult GetDetection(IJobResultStore results)
        {
            var r = results.Get<TemplateMatchingStep, DetectionResult>();
            if (!r.WasExecuted) r = results.Get<YOLODetectionStep, DetectionResult>();
            return r;
        }

        private static Makro CreateClickMacro(KlickOnPointSettings settings, System.Drawing.Point point)
        {
            var commands = new ObservableCollection<MakroBefehl>();

            // Always move mouse to position first - now using relative movement
            commands.Add(new MouseMoveAbsoluteBefehl
            {
                X = point.X,
                Y = point.Y
            });

            // Check if only mouse move is requested (no click)
            if (settings.ClickType == "none")
            {
                // Only mouse move, no additional clicks
                return new Makro
                {
                    Name = $"TempMove_{DateTime.Now:HHmmss}",
                    Befehle = commands
                };
            }

            // Perform the click based on configuration
            if (settings.DoubleClick)
            {
                // Double click: Down-Up-Down-Up sequence with small delay
                commands.Add(new MouseDownBefehl
                {
                    Button = settings.ClickType
                });

                commands.Add(new MouseUpBefehl
                {
                    Button = settings.ClickType
                });

                // Small delay between clicks (50ms is typical for double-click)
                commands.Add(new TimeoutBefehl { Duration = 50 });

                commands.Add(new MouseDownBefehl
                {
                    Button = settings.ClickType
                });

                commands.Add(new MouseUpBefehl
                {
                    Button = settings.ClickType
                });
            }
            else
            {
                // Single click: Down-Up
                commands.Add(new MouseDownBefehl
                {
                    Button = settings.ClickType
                });

                commands.Add(new MouseUpBefehl
                {
                    Button = settings.ClickType
                });
            }

            return new Makro
            {
                Name = $"TempClick_{DateTime.Now:HHmmss}",
                Befehle = commands
            };
        }
    }
}
