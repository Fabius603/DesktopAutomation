using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskAutomation.Jobs;
using TaskAutomation.Makros;

namespace TaskAutomation.Steps
{
    public class KlickOnPointStepHandler : IJobStepHandler
    {
        public async Task<bool> ExecuteAsync(object step, Job jobContext, IJobExecutionContext executor, CancellationToken ct)
        {
            var klickStep = step as KlickOnPointStep;
            if (klickStep == null)
            {
                return false;
            }

            // Check if we have a valid point
            if (executor.LatestCalculatedPoint == null)
            {
                return true; // Continue execution but no click performed - point not set
            }

            // Create temporary macro for click execution
            var macro = CreateClickMacro(klickStep.Settings, executor.LatestCalculatedPoint.Value);
            
            // Execute the macro
            await executor.MakroExecutor.ExecuteMakro(macro, executor.DxgiResources, ct);

            // Reset the point after click
            executor.LatestCalculatedPoint = null;

            return true;
        }

        private static Makro CreateClickMacro(KlickOnPointSettings settings, OpenCvSharp.Point point)
        {
            var commands = new ObservableCollection<MakroBefehl>();

            // Check if only mouse move is requested (no click)
            if (settings.ClickType == "none")
            {
                // Only move mouse, no click
                commands.Add(new MouseMoveBefehl
                {
                    X = point.X,
                    Y = point.Y
                });
            }
            // Perform the click based on configuration
            else if (settings.DoubleClick)
            {
                // Double click: Down-Up-Down-Up sequence with small delay
                commands.Add(new MouseDownBefehl
                {
                    Button = settings.ClickType,
                    X = point.X,
                    Y = point.Y
                });
                
                commands.Add(new MouseUpBefehl
                {
                    Button = settings.ClickType,
                    X = point.X,
                    Y = point.Y
                });

                // Small delay between clicks (50ms is typical for double-click)
                commands.Add(new TimeoutBefehl { Duration = 50 });

                commands.Add(new MouseDownBefehl
                {
                    Button = settings.ClickType,
                    X = point.X,
                    Y = point.Y
                });
                
                commands.Add(new MouseUpBefehl
                {
                    Button = settings.ClickType,
                    X = point.X,
                    Y = point.Y
                });
            }
            else
            {
                // Single click: Down-Up
                commands.Add(new MouseDownBefehl
                {
                    Button = settings.ClickType,
                    X = point.X,
                    Y = point.Y
                });
                
                commands.Add(new MouseUpBefehl
                {
                    Button = settings.ClickType,
                    X = point.X,
                    Y = point.Y
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
