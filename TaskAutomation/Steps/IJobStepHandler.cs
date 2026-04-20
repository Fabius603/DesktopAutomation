using System;
using System.Threading;
using System.Threading.Tasks;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps
{
    /// <summary>
    /// Marker-Interface für alle Step-Handler. Verwende <see cref="JobStepHandler{TStep,TResult}"/>
    /// als Basisklasse für konkrete Implementierungen.
    /// </summary>
    public interface IJobStepHandler
    {
        Type StepType { get; }
        Task<StepResultBase> ExecuteAsync(JobStep step, IStepPipelineContext ctx, CancellationToken ct);
    }

    /// <summary>
    /// Typsichere Basisklasse für Step-Handler.
    /// Übernimmt den Laufzeit-Cast, den Aufruf von <see cref="ExecuteCoreAsync"/>
    /// und das automatische Speichern des Ergebnisses im <see cref="IJobResultStore"/>.
    /// </summary>
    public abstract class JobStepHandler<TStep, TResult> : IJobStepHandler
        where TStep   : JobStep
        where TResult : StepResultBase
    {
        public Type StepType => typeof(TStep);

        public async Task<StepResultBase> ExecuteAsync(
            JobStep step, IStepPipelineContext ctx, CancellationToken ct)
        {
            if (step is not TStep typed)
                return CreateDefault();

            var result = await ExecuteCoreAsync(typed, ctx, ct).ConfigureAwait(false);
            ctx.Results.Set<TStep>(result, step.Id);
            return result;
        }

        protected abstract Task<TResult> ExecuteCoreAsync(
            TStep step, IStepPipelineContext ctx, CancellationToken ct);

        protected abstract TResult CreateDefault();
    }
}

