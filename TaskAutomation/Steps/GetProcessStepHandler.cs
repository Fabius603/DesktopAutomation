using Microsoft.Extensions.Logging;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps;

public sealed class GetProcessStepHandler : JobStepHandler<GetProcessStep, GetProcessResult>
{
    protected override Task<GetProcessResult> ExecuteCoreAsync(
        GetProcessStep step, IStepPipelineContext ctx, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var query = step.Settings.Query;
        var processIds = ProcessTargetResolver.ResolveProcessIds(query, ctx.Results);
        var process = processIds.Select(ProcessTargetResolver.CreateReference)
            .FirstOrDefault(reference => reference is not null);

        ctx.Logger.LogInformation(
            "GetProcessStepHandler: Abfrage Prozess='{ProcessName}', Pfad='{ExecutablePath}', Fenstertitel='{WindowTitle}' - gefunden: {Found}{ProcessId}.",
            query.ProcessName, query.ExecutablePath, query.WindowTitleContains, process is not null,
            process is null ? string.Empty : $", PID={process.ProcessId}");

        return Task.FromResult(new GetProcessResult
        {
            WasExecuted = true,
            Found = process is not null,
            Process = process
        });
    }

    protected override GetProcessResult CreateDefault() => GetProcessResult.Default;
}
