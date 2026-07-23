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

    /// <summary>
    /// Base class for steps whose concrete result contract depends on the step settings.
    /// The Windows state query step is the primary example: every query has its own
    /// strongly typed result while all results still use the common job result store.
    /// </summary>
    public abstract class DynamicJobStepHandler<TStep> : IJobStepHandler
        where TStep : JobStep
    {
        public Type StepType => typeof(TStep);

        public async Task<StepResultBase> ExecuteAsync(
            JobStep step, IStepPipelineContext ctx, CancellationToken ct)
        {
            if (step is not TStep typed)
                throw new ArgumentException(
                    $"Expected step type {typeof(TStep).Name}, got {step.GetType().Name}.",
                    nameof(step));

            var result = await ExecuteCoreAsync(typed, ctx, ct).ConfigureAwait(false);
            ValidateResultContract(typed, result);
            ctx.Results.Set<TStep>(result, step.Id);
            return result;
        }

        protected abstract Task<StepResultBase> ExecuteCoreAsync(
            TStep step, IStepPipelineContext ctx, CancellationToken ct);

        protected virtual void ValidateResultContract(TStep step, StepResultBase result)
        {
            var contract = StepResultMetadata.GetResultTypeForStep(step)
                ?? throw new InvalidOperationException(
                    $"No result contract is registered for {step.GetType().Name}.");
            if (!string.Equals(contract.TypeName, result.GetType().Name, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"Step {step.GetType().Name} returned {result.GetType().Name}, " +
                    $"but contract {contract.TypeName} was declared.");
        }
    }
}

