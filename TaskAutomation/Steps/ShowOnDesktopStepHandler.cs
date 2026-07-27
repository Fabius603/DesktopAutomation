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
            var overlay = VisualOverlayResolver.Resolve(
                ctx.Results, step.Settings.Overlay, step.Settings.DetectionsSource, ctx.Logger);
            if (!overlay.HasContent)
            {
                ctx.DesktopResultOverlay.ClearOverlay(step.Id);
                ctx.Logger.LogInformation(
                    "ShowOnDesktopStepHandler: Keine anzeigbaren Ergebnisse; Overlay dieses Steps wurde geleert.");
                return Task.FromResult(new ShowOnDesktopResult { WasExecuted = true, Success = true });
            }

            ctx.DesktopResultOverlay.ShowOverlay(step.Id, overlay);
            ctx.Logger.LogInformation(
                "ShowOnDesktopStepHandler: {DetectionGroups} Erkennungsgruppen und {Texts} Texte auf dem Desktop angezeigt.",
                overlay.DetectionGroups.Count, overlay.Texts.Count);

            return Task.FromResult(new ShowOnDesktopResult { WasExecuted = true, Success = true });
        }

        protected override ShowOnDesktopResult CreateDefault() => ShowOnDesktopResult.Default;
    }
}
