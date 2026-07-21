using System;
using System.Drawing;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps
{
    /// <summary>
    /// Zeigt einen konfigurierbaren Text direkt auf dem Desktop an.
    /// Der Text bleibt sichtbar, bis der Job endet oder dieser Step erneut mit leerem Text ausgeführt wird.
    /// </summary>
    public sealed class ShowTextStepHandler : JobStepHandler<ShowTextStep, ShowTextResult>
    {
        protected override Task<ShowTextResult> ExecuteCoreAsync(
            ShowTextStep step, IStepPipelineContext ctx, CancellationToken ct)
        {
            var s = step.Settings;

            // Farbe parsen (#RRGGBB oder #AARRGGBB)
            if (!TryParseHexColor(s.FontColor, out byte r, out byte g, out byte b))
            {
                r = 255; g = 255; b = 255;
                ctx.Logger.LogWarning(
                    "ShowTextStepHandler: Ungültige Farbe '{Color}', weiß wird verwendet.", s.FontColor);
            }

            // Opacity (0..1) → Alpha (0..255), auf den Farbwert anwenden
            var alpha = (byte)Math.Clamp((int)(s.Opacity * 255f), 0, 255);

            ctx.DesktopResultOverlay.ShowText(
                stepKey:     step.Id,
                text:        s.Text,
                fontSize:    s.FontSize,
                r: r, g: g, b: b, a: alpha,
                desktopIndex: s.DesktopIndex,
                offsetX:     s.OffsetX,
                offsetY:     s.OffsetY,
                durationMs:  s.DurationMs,
                clearOnJobEnd: s.ClearOnJobEnd);

            ctx.Logger.LogInformation(
                "ShowTextStepHandler: Text '{Text}' auf Monitor {Index} bei ({X},{Y}) angezeigt.",
                s.Text, s.DesktopIndex, s.OffsetX, s.OffsetY);

            return Task.FromResult(new ShowTextResult { WasExecuted = true, Success = true });
        }

        protected override ShowTextResult CreateDefault() => ShowTextResult.Default;

        // ── Hilfsmethoden ──────────────────────────────────────────────────────

        /// <summary>
        /// Parst eine Hex-Farbe der Form "#RRGGBB" oder "#AARRGGBB".
        /// </summary>
        private static bool TryParseHexColor(string? hex, out byte r, out byte g, out byte b)
        {
            r = g = b = 0;
            if (string.IsNullOrWhiteSpace(hex)) return false;
            hex = hex.TrimStart('#');

            if (hex.Length == 6 &&
                byte.TryParse(hex.Substring(0, 2), NumberStyles.HexNumber, null, out r) &&
                byte.TryParse(hex.Substring(2, 2), NumberStyles.HexNumber, null, out g) &&
                byte.TryParse(hex.Substring(4, 2), NumberStyles.HexNumber, null, out b))
                return true;

            if (hex.Length == 8)
            {
                // AARRGGBB
                if (byte.TryParse(hex.Substring(2, 2), NumberStyles.HexNumber, null, out r) &&
                    byte.TryParse(hex.Substring(4, 2), NumberStyles.HexNumber, null, out g) &&
                    byte.TryParse(hex.Substring(6, 2), NumberStyles.HexNumber, null, out b))
                    return true;
            }

            return false;
        }
    }
}
