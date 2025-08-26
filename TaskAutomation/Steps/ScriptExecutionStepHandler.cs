using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps
{
    public class ScriptExecutionStepHandler : IJobStepHandler
    {
        public async Task<bool> ExecuteAsync(object step, Job jobContext, IJobExecutionContext executor, CancellationToken ct)
        {
            var scStep = step as ScriptExecutionStep;
            if (scStep == null)
            {
                return false;
            }

            var isFile = File.Exists(scStep.Settings.ScriptPath);
            if (!isFile)
            {
                throw new FileNotFoundException($"Script file not found: {scStep.Settings.ScriptPath}");
            }

            if (scStep.Settings.FireAndForget)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await executor.ScriptExecutor.ExecuteScriptFile(
                            scStep.Settings.ScriptPath,
                            ct);
                    }
                    catch (Exception ex)
                    {
                        // Log the exception or handle it as needed
                        Console.WriteLine($"Error executing script: {ex.Message}");
                    }
                });
                return true;
            }
            else
            {
                try
                {
                    await executor.ScriptExecutor.ExecuteScriptFile(
                    scStep.Settings.ScriptPath,
                    ct);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error executing script: {ex.Message}");
                }
            }
            return true;
        }
    }
}
