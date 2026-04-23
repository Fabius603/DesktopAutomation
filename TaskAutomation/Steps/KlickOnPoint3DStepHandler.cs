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
using System.Drawing;
using Point = OpenCvSharp.Point;

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

            var point        = detection.Point.Value;
            var capture      = ctx.Results.GetById<CaptureResult>(step.Settings.SourceCaptureStepId);
            var screenBounds = capture.Bounds;

            var (dx, dy) = CalculateRelativeMouseMovement(
                new OpenCvSharp.Point(point.X, point.Y), screenBounds.Size,
                step.Settings.FOV,
                step.Settings.MausSensitivityX,
                step.Settings.MausSensitivityY,
                step.Settings.InvertMouseMovementY,
                step.Settings.InvertMouseMovementX);

            logger.LogInformation(
                "KlickOnPoint3DStepHandler: Moving mouse (dx:{DX}, dy:{DY}) to ({X},{Y}), click='{Click}'",
                dx, dy, point.X, point.Y, step.Settings.ClickType);

            var macro = CreateClickMacro(step.Settings, new Point(dx, dy));
            await ctx.MakroExecutor.ExecuteMakro(macro, ctx.DxgiResources, ct);

            return new TaskResult { WasExecuted = true, Success = true };
        }

        protected override TaskResult CreateDefault() => TaskResult.Default;

        private static Makro CreateClickMacro(KlickOnPoint3DSettings settings, Point point)
        {
            var commands = new ObservableCollection<MakroBefehl>
            {
                new MouseMoveRelativeBefehl
                {
                    DeltaX = point.X,
                    DeltaY = point.Y
                }
            };

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

        private static (int dx, int dy) CalculateRelativeMouseMovement(Point target, Size desktopSize, float fov, float sensitivityX, float sensitivityY, bool invertY, bool invertX)
        {
            // 0) optionale Grundskalierung für "Sensitivity" -> rad/Count
            //    Kalibriere diese beiden Konstanten einmalig auf deine Ziel-App!
            const float BaseScaleRadPerCountX = 0.0025f; // ~0.14° pro Count bei sens=1.0
            const float BaseScaleRadPerCountY = 0.0025f;

            float radPerCountX = MathF.Max(1e-6f, BaseScaleRadPerCountX * sensitivityX);
            float radPerCountY = MathF.Max(1e-6f, BaseScaleRadPerCountY * sensitivityY);

            // 1) Normierte Offsets relativ zur Bildmitte (y nach oben)
            float width = desktopSize.Width;
            float height = desktopSize.Height;
            float cx = width * 0.5f, cy = height * 0.5f;

            float nx = (target.X - cx) / (width * 0.5f);
            float ny = (cy - target.Y) / (height * 0.5f);

            // 2) FOV vorbereiten: gegeben ist horizontaler FOV (Grad)
            float hfov = Deg2Rad(fov);
            float aspect = width / height;
            float vfov = 2f * MathF.Atan(MathF.Tan(hfov * 0.5f) / aspect);

            // 3) Pinhole-Projection zurückrechnen: Off-Axis Richtung im Kameraraum
            float x = nx * MathF.Tan(hfov * 0.5f);
            float y = ny * MathF.Tan(vfov * 0.5f);

            // 4) Winkel-Fehler relativ zum Vorwärtsvektor (0,0,1)
            float yawErr = MathF.Atan2(x, 1f);                        // [-π, π]
            float pitchErr = MathF.Atan2(y, MathF.Sqrt(x * x + 1f));    // [-π/2, π/2]

            // 5) Winkel -> Mauscounts
            float dx = yawErr / radPerCountX;
            float dy = pitchErr / radPerCountY;

            if (invertY)
                dy = -dy;

            if (invertX)
                dx = -dx;

            // 6) Runden auf ganzzahlige Counts
            return ((int)MathF.Round(dx), (int)MathF.Round(dy));
        }

        private static float Deg2Rad(float d) => d * (MathF.PI / 180f);
    }
}
