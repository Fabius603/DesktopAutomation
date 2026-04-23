using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TaskAutomation.Jobs;
using TaskAutomation.Makros;
using Microsoft.Extensions.Logging;

namespace TaskAutomation.Steps
{
    public sealed class MakroExecutionStepHandler : JobStepHandler<MakroExecutionStep, TaskResult>
    {
        protected override async Task<TaskResult> ExecuteCoreAsync(
            MakroExecutionStep step, IStepPipelineContext ctx, CancellationToken ct)
        {
            var logger = ctx.Logger;
            logger.LogDebug("MakroExecutionStepHandler: Executing makro '{Name}'", step.Settings.MakroName);

            Makro? makro = null;
            if (step.Settings.MakroId.HasValue)
            {
                // O(1) lookup via dictionary key (stored as ID.ToString())
                ctx.AllMakros.TryGetValue(step.Settings.MakroId.Value.ToString(), out makro);
                if (makro == null)
                    throw new InvalidOperationException($"Makro with ID '{step.Settings.MakroId}' not found");
            }
            else if (!string.IsNullOrWhiteSpace(step.Settings.MakroName))
            {
                // Fallback: name-based lookup (O(n) but only when ID is absent)
                makro = ctx.AllMakros.Values.FirstOrDefault(m =>
                    string.Equals(m.Name, step.Settings.MakroName, StringComparison.OrdinalIgnoreCase));
                if (makro == null)
                    throw new InvalidOperationException($"Makro '{step.Settings.MakroName}' not found");
            }
            else
            {
                throw new InvalidOperationException("No makro ID or name specified");
            }

            logger.LogInformation("MakroExecutionStepHandler: Executing '{Name}' (ID:{Id})", makro.Name, makro.Id);
            await ctx.MakroExecutor.ExecuteMakro(makro, ctx.DxgiResources, ct);

            return new TaskResult { WasExecuted = true, Success = true };
        }

        protected override TaskResult CreateDefault() => TaskResult.Default;
    }
}
