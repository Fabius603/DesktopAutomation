using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskAutomation.Jobs;
using TaskAutomation.Makros;
using Microsoft.Extensions.Logging;

namespace TaskAutomation.Steps
{
    public class KlickOnPointStepHandler : IJobStepHandler
    {
        public async Task<bool> ExecuteAsync(object step, Job jobContext, IJobExecutor jobExecutor, CancellationToken ct)
        {
            var logger = jobExecutor.Logger;

            if (step is not KlickOnPointStep klickStep)
            {
                var errorMessage = $"Invalid step type - expected KlickOnPointStep, got {step?.GetType().Name ?? "null"}";
                logger.LogError("KlickOnPointStepHandler: {ErrorMessage}", errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            logger.LogDebug("KlickOnPointStepHandler: Processing click on point step with click type '{ClickType}', double click: {DoubleClick}, timeout: {TimeoutMs}ms",
                klickStep.Settings.ClickType, klickStep.Settings.DoubleClick, klickStep.Settings.TimeoutMs);

            try
            {
                // Check if we have a valid point
                if (jobExecutor.LatestCalculatedPoint == null)
                {
                    var infoMessage = "No valid point available for clicking - point not set by previous steps";
                    logger.LogInformation("KlickOnPointStepHandler: {InfoMessage}", infoMessage);
                    return true;
                }

                // Use the step's unique ID as dictionary key
                var stepKey = $"KlickOnPoint_{klickStep.Id}";

                // Check if timeout is still active
                if (jobExecutor.StepTimeouts.TryGetValue(stepKey, out var lastExecution))
                {
                    var timeSinceLastExecution = DateTime.Now - lastExecution;
                    if (timeSinceLastExecution.TotalMilliseconds < klickStep.Settings.TimeoutMs)
                    {
                        // Timeout not yet elapsed, skip execution
                        logger.LogDebug("KlickOnPointStepHandler: Skipping click - timeout not yet elapsed ({TimeRemaining}ms remaining)",
                            klickStep.Settings.TimeoutMs - timeSinceLastExecution.TotalMilliseconds);
                        return true;
                    }
                }

                // Update timeout tracker
                jobExecutor.StepTimeouts[stepKey] = DateTime.Now;

                var point = jobExecutor.LatestCalculatedPoint.Value;
                logger.LogInformation("KlickOnPointStepHandler: Executing {ClickType} click at point ({X}, {Y})",
                    klickStep.Settings.DoubleClick ? "double" : "single", point.X, point.Y);

                // Create temporary macro for click execution
                var macro = CreateClickMacro(klickStep.Settings, point);

                // Execute the macro
                await jobExecutor.MakroExecutor.ExecuteMakro(macro, jobExecutor.DxgiResources, ct);

                // Reset the point after click
                jobExecutor.LatestCalculatedPoint = null;
                logger.LogDebug("KlickOnPointStepHandler: Click executed successfully, point reset");

                return true;
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("KlickOnPointStepHandler: Click on point was cancelled");
                return false; // Return false for cancellation, don't treat as error
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "KlickOnPointStepHandler: Failed to execute click: {ErrorMessage}", ex.Message);
                throw; // Re-throw all other exceptions
            }
        }

        private static Makro CreateClickMacro(KlickOnPointSettings settings, OpenCvSharp.Point point)
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
