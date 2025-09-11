using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskAutomation.Jobs;
using TaskAutomation.Makros;
using Microsoft.Extensions.Logging;
using System.Drawing;
using Point = OpenCvSharp.Point;

namespace TaskAutomation.Steps
{
    public class KlickOnPoint3DStepHandler : IJobStepHandler
    {
        public async Task<bool> ExecuteAsync(object step, Job jobContext, IJobExecutor jobExecutor, CancellationToken ct)
        {
            var logger = jobExecutor.Logger;

            if (step is not KlickOnPoint3DStep klickStep3D)
            {
                var errorMessage = $"Invalid step type - expected KlickOnPoint3DStep, got {step?.GetType().Name ?? "null"}";
                logger.LogError("KlickOnPoint3DStepHandler: {ErrorMessage}", errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            logger.LogDebug("KlickOnPoint3DStepHandler: Processing klick on point 3D step with FOV '{FOV}', sensitivity X: {SensitivityX}, sensitivity Y: {SensitivityY}, click type: '{ClickType}', double click: {DoubleClick}, timeout: {TimeoutMs}ms",
                klickStep3D.Settings.FOV, klickStep3D.Settings.MausSensitivityX, klickStep3D.Settings.MausSensitivityY,
                klickStep3D.Settings.ClickType, klickStep3D.Settings.DoubleClick, klickStep3D.Settings.TimeoutMs);

            try
            {
                if (jobExecutor.LatestCalculatedPoint == null)
                {
                    var infoMessage = "No valid point available for clicking - point not set by previous steps";
                    logger.LogInformation("KlickOnPoint3DStepHandler: {InfoMessage}", infoMessage);
                    return true;
                }

                var stepKey = $"KlickOnPoint3D_{klickStep3D.Id}";

                if (jobExecutor.StepTimeouts.TryGetValue(stepKey, out var lastExecution))
                {
                    var timeSinceLastExecution = DateTime.Now - lastExecution;
                    if (timeSinceLastExecution.TotalMilliseconds < klickStep3D.Settings.TimeoutMs)
                    {
                        // Timeout not yet elapsed, skip execution
                        logger.LogDebug("KlickOnPoint3DStepHandler: Skipping click - timeout not yet elapsed ({TimeRemaining}ms remaining)",
                            klickStep3D.Settings.TimeoutMs - timeSinceLastExecution.TotalMilliseconds);
                        return true;
                    }
                }
                jobExecutor.StepTimeouts[stepKey] = DateTime.Now;

                var point = jobExecutor.LatestCalculatedPoint.Value;
                var screenBounds = jobExecutor.DesktopBounds;

                var (dx, dy) = CalculateRelativeMouseMovement(
                    point,
                    screenBounds.Size,
                    klickStep3D.Settings.FOV,
                    klickStep3D.Settings.MausSensitivityX,
                    klickStep3D.Settings.MausSensitivityY
                    );
                logger.LogInformation("KlickOnPoint3DStepHandler: Moving mouse by (dx: {DX}, dy: {DY}) to target point ({X}, {Y}) and executing {ClickType} click",
                    dx, dy, point.X, point.Y, klickStep3D.Settings.ClickType);

                // Create temporary macro for click execution
                var macro = CreateClickMacro(klickStep3D.Settings, new Point(dx, dy));

                // Execute the macro
                await jobExecutor.MakroExecutor.ExecuteMakro(macro, jobExecutor.DxgiResources, ct);

                // Reset the point after click
                jobExecutor.LatestCalculatedPoint = null;
                
                logger.LogDebug("KlickOnPoint3DStepHandler: Click executed successfully, point reset");
                return true;
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("KlickOnPoint3DStepHandler: Klick on point 3D was cancelled");
                return false; // Return false for cancellation, don't treat as error
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "KlickOnPoint3DStepHandler: Failed to execute klick on point 3D: {ErrorMessage}", ex.Message);
                throw; // Re-throw all other exceptions
            }
        }

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

        private static (int dx, int dy) CalculateRelativeMouseMovement(Point target, Size desktopSize, float fov, float sensitivityX, float sensitivityY)
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
            float dx = -yawErr / radPerCountX;
            float dy = -pitchErr / radPerCountY;

            // 6) Runden auf ganzzahlige Counts
            return ((int)MathF.Round(dx), (int)MathF.Round(dy));
        }

        private static float Deg2Rad(float d) => d * (MathF.PI / 180f);
    }
}
