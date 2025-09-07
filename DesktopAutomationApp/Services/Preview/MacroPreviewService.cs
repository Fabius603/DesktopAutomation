using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using DesktopOverlay;
using DesktopOverlay.OverlayItems;
using TaskAutomation.Makros;
using Color = GameOverlay.Drawing.Color;

namespace DesktopAutomationApp.Services.Preview
{
    public sealed class MacroPreviewService : IMacroPreviewService
    {
        // -------------------------
        // Anzeige-Tuning
        // -------------------------

        // Key-Badges
        private const double KeyMinVisibleSeconds = 1.0;   // Mindestdauer pro Key-Badge
        private const float KeyBadgeScale = 1.6f;  // größer & lesbarer
        private const float KeyLaneLineHeightPx = 30f;   // Abstand der Badge-Zeilen (Overlay-LOCAL)
        private const int KeyLaneMax = 8;     // max. Zeilen
        private const float KeySlotSpacingLocal = 220f; // horizontaler Abstand zwischen Badges
        private const float KeyBottomOffsetLocal = 130f;  // Abstand vom unteren Rand

        // Klick-Pulse
        private const double ClickSeconds = 0.35;

        // Segmente ohne Zeitdifferenz bekommen eine minimale Dauer,
        // damit der Playhead sichtbar „zuckt“ statt zu teleportieren.
        private const double MinSegmentSeconds = 0.02;

        public sealed record PreviewResult(
            IEnumerable<IOverlayItem> StaticItems,
            IEnumerable<IOverlayItem> TimedItems,
            double TotalSeconds);

        /// <summary>
        /// Baut die Overlay-Items. Zeitskala wird ausschließlich aus TimeoutBefehl aufgebaut,
        /// alle anderen Befehle sind zeitstempelgenau an diese Skala gebunden.
        /// </summary>
        public PreviewResult Build(Makro makro, Rectangle virtualBounds, Rectangle overlayBounds)
        {
            var overlayLocal = new Rectangle(0, 0, overlayBounds.Width, overlayBounds.Height);
            var tr = OverlayTransform.FromVirtualToOverlayLocal(virtualBounds, overlayLocal);

            var stat = new List<IOverlayItem>();
            var timed = new List<IOverlayItem>();

            // Zeitachse: nur Timeout erhöht t.
            double t = 0.0;

            // Mauspositionsliste mit Zeitstempel t_i
            var mousePositions = new List<((float x, float y) p, double t)>();

            // Für Click-Visualisierung merken wir Down/Up-Positionen (bei t)
            // (Wir erzeugen Pulse direkt während des Durchlaufs.)

            // Key-Intervalle: sammeln wir erst und rendern sie später lane-basiert
            var keyDownAt = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            var keyIntervals = new List<(string key, double start, double end)>(); // end wird später gefüllt

            // Letzter Mauspunkt (nur zur Node/Segment-Bildung hilfreich)
            (float x, float y)? lastPos = null;
            int nodeIdx = 0;

            foreach (var cmd in makro.Befehle)
            {
                switch (cmd)
                {
                    case MouseMoveAbsoluteBefehl m:
                        {
                            var p = ((float)m.X, (float)m.Y);
                            mousePositions.Add((p, t));
                            nodeIdx++;

                            // Nur Position erfassen für Liniendarstellung - keine Node/Punkt
                            lastPos = p;
                            break;
                        }

                    case MouseMoveRelativeBefehl mr:
                        {
                            // Relative Bewegung von letzter Position
                            var p = lastPos.HasValue 
                                ? ((float)(lastPos.Value.Item1 + mr.DeltaX), (float)(lastPos.Value.Item2 + mr.DeltaY))
                                : ((float)mr.DeltaX, (float)mr.DeltaY);
                            
                            mousePositions.Add((p, t));
                            nodeIdx++;

                            // Nur Position erfassen für Liniendarstellung - keine Node/Punkt
                            lastPos = p;
                            break;
                        }

                    case MouseDownBefehl d:
                        {
                            // Mouse Down ohne Koordinaten - verwende letzte Position
                            if (!lastPos.HasValue) lastPos = (0f, 0f); // Fallback
                            var p = lastPos.Value;

                            // Puls (timed)
                            timed.Add(new ClickPulseItem(
                                id: $"md_{t:F3}",
                                globalX: p.Item1, globalY: p.Item2,
                                isDown: true,
                                startSeconds: t,
                                durationSeconds: ClickSeconds,
                                color: new Color(255, 80, 80, 220)
                            )
                            { Transform = tr });

                            // Node ↓ (statisch)
                            stat.Add(new NodeItem(
                                id: $"node_md_{nodeIdx++}",
                                globalX: p.Item1, globalY: p.Item2,
                                radius: 7f, label: "↓",
                                fill: new Color(255, 80, 80, 140),
                                stroke: new Color(255, 80, 80, 220),
                                strokeWidth: 2f,
                                text: new Color(255, 255, 255, 255)
                            )
                            { Transform = tr });

                            break;
                        }

                    case MouseUpBefehl u:
                        {
                            // Mouse Up ohne Koordinaten - verwende letzte Position
                            if (!lastPos.HasValue) lastPos = (0f, 0f); // Fallback
                            var p = lastPos.Value;

                            timed.Add(new ClickPulseItem(
                                id: $"mu_{t:F3}",
                                globalX: p.Item1, globalY: p.Item2,
                                isDown: false,
                                startSeconds: t,
                                durationSeconds: ClickSeconds,
                                color: new Color(255, 180, 80, 220)
                            )
                            { Transform = tr });

                            stat.Add(new NodeItem(
                                id: $"node_mu_{nodeIdx++}",
                                globalX: p.Item1, globalY: p.Item2,
                                radius: 7f, label: "↑",
                                fill: new Color(255, 180, 80, 140),
                                stroke: new Color(255, 180, 80, 220),
                                strokeWidth: 2f,
                                text: new Color(255, 255, 255, 255)
                            )
                            { Transform = tr });

                            break;
                        }

                    case KeyDownBefehl kd:
                        if (!keyDownAt.ContainsKey(kd.Key ?? "")) keyDownAt[kd.Key ?? ""] = t;
                        break;

                    case KeyUpBefehl ku:
                        if (keyDownAt.TryGetValue(ku.Key ?? "", out var t0))
                        {
                            keyIntervals.Add((ku.Key ?? "", t0, t));
                            keyDownAt.Remove(ku.Key ?? "");
                        }
                        break;

                    case TimeoutBefehl to:
                        {
                            var ms = Math.Max(0, to.Duration);
                            if (ms > 0 && lastPos is { } lp)
                            {
                                // Timeout-Badge an letztem Punkt anzeigen
                                timed.Add(new TimeoutBadgeItem(
                                    id: $"to_{t:F3}",
                                    globalX: lp.Item1, globalY: lp.Item2,
                                    startSeconds: t,
                                    durationMs: ms
                                )
                                { Transform = tr });
                            }
                            t += ms / 1000.0;
                            break;
                        }
                }
            }

            // Keys, die bis zum Ende gehalten wurden, abschließen
            if (keyDownAt.Count > 0)
            {
                var end = t;
                foreach (var kv in keyDownAt)
                    keyIntervals.Add((kv.Key, kv.Value, end));
                keyDownAt.Clear();
            }

            // ------------------------------------------------------
            // Pfad (statisch) + Playhead-Segmente (timed) bauen
            // ------------------------------------------------------
            if (mousePositions.Count >= 2)
            {
                // kompletter Pfad als dünne Linie
                stat.Add(new PolylineItem(
                    id: "path_full",
                    globalPoints: mousePositions.Select(mp => (mp.p.Item1, mp.p.Item2)).ToList(),
                    thickness: 2f,
                    color: new Color(0, 180, 255, 130)
                )
                { Transform = tr });

                // optional: „Playback“-Pfad (Progress wird vom Overlay ggf. animiert)
                stat.Add(new PolylineItem(
                    id: "path_play",
                    globalPoints: mousePositions.Select(mp => (mp.p.Item1, mp.p.Item2)).ToList(),
                    thickness: 4f,
                    color: new Color(0, 220, 120, 220)
                )
                { Transform = tr, Progress = 0f });

                // Playhead-Segmente: zeitlich genau zwischen den Positionen
                var segments = new List<PlayheadItem.SegmentGlobal>(mousePositions.Count - 1);
                for (int i = 0; i < mousePositions.Count - 1; i++)
                {
                    var a = mousePositions[i];
                    var b = mousePositions[i + 1];
                    double start = a.t;
                    double end = b.t;
                    if (end <= start)
                        end = start + MinSegmentSeconds; // minimale Sichtbarkeit

                    segments.Add(new PlayheadItem.SegmentGlobal(a.p, b.p, start, end));
                }

                if (segments.Count > 0)
                {
                    timed.Add(new PlayheadItem(
                        id: "playhead",
                        segments: segments,
                        radius: 6f,
                        color: new Color(255, 255, 255, 255)
                    )
                    { Transform = tr });
                }

                // TotalSeconds: Ende des letzten Segments ODER letzte t-Position,
                // plus evtl. Key-Mindestdauer abgedeckt
                t = Math.Max(t, segments[^1].End);
            }
            else
            {
                // Kein Pfad – TotalSeconds ist aktuelles t
            }

            // ------------------------------------------------------
            // Keys lane-basiert rendern (keine Überlappung in einer Zeile)
            // ------------------------------------------------------
            if (keyIntervals.Count > 0)
            {
                // ----------------------------------------------
                // Lane-Zuordnung (nebeneinander) per Sweep-Line
                // ----------------------------------------------
                keyIntervals.Sort((a, b) => a.start.CompareTo(b.start));

                // Für jede Lane merken wir, bis wann sie belegt ist
                var laneEnd = new List<double>();         // Index = Lane-Id
                var laneOf = new int[keyIntervals.Count]; // laneOf[i] → zugewiesene Lane

                for (int i = 0; i < keyIntervals.Count; i++)
                {
                    var (key, start, end) = keyIntervals[i];
                    int lane = 0;
                    for (; lane < laneEnd.Count; lane++)
                        if (laneEnd[lane] <= start) break;

                    if (lane == laneEnd.Count) laneEnd.Add(end); else laneEnd[lane] = end;
                    laneOf[i] = lane;
                }

                int lanesUsed = laneEnd.Count;

                // ----------------------------------------------
                // Positionen unten mittig berechnen (LOCAL)
                // ----------------------------------------------
                float centerXLocal = overlayLocal.Width * 0.5f;
                float baseXLocal = centerXLocal - ((lanesUsed - 1) * KeySlotSpacingLocal) * 0.5f;
                float yLocal = overlayLocal.Height - KeyBottomOffsetLocal;

                // LOCAL → GLOBAL umrechnen
                // gx = virt.Left + localX / tr.Sx ;  gy = virt.Top + localY / tr.Sy
                float gxOf(float localX) => virtualBounds.Left + localX / (float)tr.Sx;
                float gyOf(float localY) => virtualBounds.Top + localY / (float)tr.Sy;

                // ----------------------------------------------
                // Badges anlegen: pro Intervall genau ein Item
                // ----------------------------------------------
                for (int i = 0; i < keyIntervals.Count; i++)
                {
                    var (key, start, end) = keyIntervals[i];
                    int lane = laneOf[i];

                    float xLocal = baseXLocal + lane * KeySlotSpacingLocal;
                    float gx = gxOf(xLocal);
                    float gy = gyOf(yLocal);

                    var badge = new KeyItem(
                        id: $"key_{key}_{start:F3}",
                        label: key,
                        globalX: gx,
                        globalY: gy,
                        startSeconds: start,
                        endSeconds: end
                    )
                    { Transform = tr };

                    // optional: wenn dein KeyBadgeItem eine Scale/FontSize unterstützt
                    // badge.Scale = KeyBadgeFontScaleHint;

                    timed.Add(badge);
                }
            }
            var totalSeconds = Math.Max(
            t,
            mousePositions.Count > 0 ? mousePositions[^1].t : 0.0
            );

            return new PreviewResult(stat, timed, totalSeconds);
        }
    }
}
