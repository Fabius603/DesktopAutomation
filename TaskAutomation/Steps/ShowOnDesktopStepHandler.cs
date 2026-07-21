using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps
{
    /// <summary>
    /// Zeigt alle Ergebnisse eines Detection-Steps (BoundingBox + Mittelpunkt)
    /// direkt auf dem Desktop über ein transparentes Overlay-Fenster an –
    /// analog zu <see cref="ShowImageStepHandler"/>, jedoch ohne eigenes Vorschaufenster.
    /// Bestes Ergebnis (Index 0) wird farblich hervorgehoben, alle weiteren in Grün.
    /// </summary>
    public sealed class ShowOnDesktopStepHandler : JobStepHandler<ShowOnDesktopStep, ShowOnDesktopResult>
    {
        protected override Task<ShowOnDesktopResult> ExecuteCoreAsync(
            ShowOnDesktopStep step, IStepPipelineContext ctx, CancellationToken ct)
        {
            var resolved = ResultBindingResolver.ResolveDetections(ctx.Results, step.Settings.DetectionsSource);

            if (!resolved.IsSuccess)
            {
                ctx.DesktopResultOverlay.Clear();
                ctx.Logger.LogInformation(
                    "ShowOnDesktopStepHandler: Kein Treffer im Quell-Step {SourceStepId}; Overlay wurde geleert.",
                    step.Settings.DetectionsSource.SourceStepId);
                return Task.FromResult(new ShowOnDesktopResult { WasExecuted = true, Success = true });
            }

            IReadOnlyList<DetectionItem> items = resolved.Values;

            ctx.DesktopResultOverlay.ShowResult(items);
            ctx.Logger.LogInformation(
                "ShowOnDesktopStepHandler: {Count} Treffer aus Quell-Step {SourceStepId} auf dem Desktop angezeigt.",
                items.Count, step.Settings.DetectionsSource.SourceStepId);

            return Task.FromResult(new ShowOnDesktopResult { WasExecuted = true, Success = true });
        }

        protected override ShowOnDesktopResult CreateDefault() => ShowOnDesktopResult.Default;
    }
}
