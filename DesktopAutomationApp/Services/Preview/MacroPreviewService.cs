using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskAutomation.Makros;
using DesktopOverlay.OverlayItems;
using System.Drawing;
using Font = GameOverlay.Drawing.Font;
using SolidBrush = GameOverlay.Drawing.SolidBrush;
using Graphics = GameOverlay.Drawing.Graphics;
using Color = GameOverlay.Drawing.Color;
using DesktopOverlay;

namespace DesktopAutomationApp.Services.Preview
{
    public sealed class MacroPreviewService : IMacroPreviewService
    {
        private const double MoveMs = 120;  // nur für Playback-Visualisierung
        private const double ClickMs = 180;

        public sealed record PreviewResult(
            IEnumerable<IOverlayItem> StaticItems,
            IEnumerable<IOverlayItem> TimedItems,
            double TotalSeconds);

        /// <summary>
        /// Baut die Items für Overlay. virtualBounds = gesamter virtueller Desktop (Pixel), overlayBounds = Overlay-Fenster-Bounds.
        /// </summary>
        public PreviewResult Build(Makro makro, Rectangle virtualBounds, Rectangle overlayBounds)
        {
            var overlayLocal = new Rectangle(0, 0, overlayBounds.Width, overlayBounds.Height);
            var tr = OverlayTransform.FromVirtualToOverlayLocal(virtualBounds, overlayLocal);

            var ptsGlobal = new List<(float x, float y)>();
            var segments = new List<PlayheadItem.SegmentGlobal>();
            var timed = new List<IOverlayItem>();
            var stat = new List<IOverlayItem>();
            var keyDown = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

            double t = 0.0;
            int nodeIdx = 0;
            (float x, float y)? lastGlobal = null;

            foreach (var cmd in makro.Befehle)
            {
                switch (cmd)
                {
                    case MouseMoveBefehl m:
                        {
                            var p = ((float)m.X, (float)m.Y); // globale (virtuelle) Pixel
                            if (lastGlobal is { } prev)
                                segments.Add(new PlayheadItem.SegmentGlobal(prev, p, t, t + MoveMs / 1000.0));

                            ptsGlobal.Add(p);
                            nodeIdx++;

                            var node = new NodeItem($"node_{nodeIdx}",
                                                    p.Item1, p.Item2, 6f, nodeIdx.ToString(),
                                                    new Color(20, 130, 255, 160),
                                                    new Color(20, 130, 255, 220),
                                                    2f,
                                                    new Color(255, 255, 255, 255))
                            { Transform = tr };
                            stat.Add(node);

                            t += MoveMs / 1000.0;
                            lastGlobal = p;
                            break;
                        }

                    case MouseDownBefehl d:
                        {
                            var p = ((float)d.X, (float)d.Y);
                            var pulse = new ClickPulseItem($"down_{t:F3}", p.Item1, p.Item2, true, t, ClickMs / 1000.0,
                                                           new Color(255, 80, 80, 220))
                            { Transform = tr };
                            timed.Add(pulse);
                            lastGlobal = p;
                            break;
                        }

                    case MouseUpBefehl u:
                        {
                            var p = ((float)u.X, (float)u.Y);
                            var pulse = new ClickPulseItem($"up_{t:F3}", p.Item1, p.Item2, false, t, ClickMs / 1000.0,
                                                           new Color(255, 180, 80, 220))
                            { Transform = tr };
                            timed.Add(pulse);
                            lastGlobal = p;
                            break;
                        }

                    case KeyDownBefehl kd:
                        keyDown[kd.Key ?? ""] = t;
                        break;

                    case KeyUpBefehl ku:
                        {
                            var key = ku.Key ?? "";
                            if (keyDown.TryGetValue(key, out var t0))
                            {
                                // Badge in der Nähe des letzten globalen Punkts platzieren.
                                // Falls es noch keinen gibt: eine feste Ecke des virtuellen Desktops,
                                // dabei 12px/24px in LOCAL entsprechen 12/Sx bzw. 24/Sy in GLOBAL.
                                float bx, by;
                                if (ptsGlobal.Count > 0)
                                {
                                    var anchor = ptsGlobal.Last();
                                    bx = anchor.x;
                                    by = anchor.y;
                                }
                                else
                                {
                                    bx = virtualBounds.Left + (12f / tr.Sx);
                                    by = virtualBounds.Top + (12f / tr.Sy);
                                }
                                // Zeilen-Offset
                                by += (float)((24.0 / tr.Sy) * (keyDown.Count % 10));

                                var badge = new KeyBadgeItem($"key_{key}_{t0:F3}", key, bx, by, t0, t)
                                { Transform = tr };
                                timed.Add(badge);

                                keyDown.Remove(key);
                            }
                            break;
                        }

                    case TimeoutBefehl to:
                        {
                            if (lastGlobal is { } lp && to.Duration > 0)
                            {
                                var badge = new TimeoutBadgeItem($"timeout_{t:F3}", lp.x, lp.y, t, to.Duration)
                                { Transform = tr };
                                timed.Add(badge);
                                t += to.Duration / 1000.0;
                            }
                            break;
                        }
                }
            }

            if (ptsGlobal.Count >= 2)
            {
                var full = new PolylineItem("path_full", ptsGlobal, 2f, new Color(0, 180, 255, 130))
                { Transform = tr };
                var play = new PolylineItem("path_play", ptsGlobal, 4f, new Color(0, 220, 120, 220))
                { Transform = tr, Progress = 0f };

                stat.Add(full);
                stat.Add(play);
            }

            if (segments.Count > 0)
            {
                var head = new PlayheadItem("playhead", segments, 5f, new Color(255, 255, 255, 255))
                { Transform = tr };
                timed.Add(head);
            }

            double total = segments.Count > 0 ? segments[^1].End : t;
            return new PreviewResult(stat, timed, total);
        }
    }
}
