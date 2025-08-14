using DesktopOverlay.OverlayItems;
using DesktopOverlay;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms; // Screen
using ImageHelperMethods;
using Color = GameOverlay.Drawing.Color;

namespace ImageCapture.DesktopDuplication.RecordingIndicator
{
    public sealed class RecordingIndicatorOverlay : IRecordingIndicatorOverlay, IDisposable
    {
        private readonly string _borderId = "rec_border";
        private readonly string _dotId = "rec_dot";
        private readonly string _labelId = "rec_label";

        private Overlay _overlay;
        
        public bool IsRunning { get; private set; }

        // P/Invoke: Overlay von Aufnahme ausschließen
        [DllImport("user32.dll")] private static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);
        private const uint WDA_EXCLUDEFROMCAPTURE = 0x11;

        public void Build(Rectangle virtualBounds, Rectangle overlayBounds, RecordingIndicatorOptions? options = null)
        {
            var overlayLocal = new Rectangle(0, 0, overlayBounds.Width, overlayBounds.Height);
            var tr = OverlayTransform.FromVirtualToOverlayLocal(virtualBounds, overlayLocal);
            var monitorBounds = ScreenHelper.GetDesktopBounds(options.MonitorIndex);
            _overlay.ClearItems();

            switch (options.Mode)
            {
                case RecordingIndicatorMode.RedBorder:
                    ShowRedBorder(monitorBounds, tr, options);
                    break;

                case RecordingIndicatorMode.CornerBadge:
                    ShowCornerBadge(monitorBounds, tr, options);
                    break;
            }
        }

        public void Start(RecordingIndicatorOptions? options = null)
        {
            options ??= new RecordingIndicatorOptions();

            EnsureOverlayCreated();

            var virtualBounds = ScreenHelper.GetVirtualDesktopBounds();
            Build(virtualBounds, virtualBounds, options);

            IsRunning = true;
        }

        public void Stop()
        {
            _overlay?.ClearItems();
            IsRunning = false;
        }

        public void Dispose()
        {
            Stop();
            _overlay?.Dispose();
            _overlay = null;
        }

        // --- Helpers ---

        private void EnsureOverlayCreated()
        {
            if (_overlay != null) return;

            var v = ScreenHelper.GetVirtualDesktopBounds();
            _overlay = new Overlay(v.Left, v.Top, v.Width, v.Height);
            _overlay.RunInNewThread();
        }

        private static (Rectangle vdesk, Rectangle monitor) GetBounds(int monitorIndex)
        {
            var screens = ScreenHelper.GetScreens();
            if (monitorIndex < 0 || monitorIndex >= screens.Length)
                throw new ArgumentOutOfRangeException(nameof(monitorIndex));

            // Virtual Desktop extents
            int left = screens.Min(s => s.Bounds.Left);
            int top = screens.Min(s => s.Bounds.Top);
            int right = screens.Max(s => s.Bounds.Right);
            int bottom = screens.Max(s => s.Bounds.Bottom);

            var vdesk = new Rectangle(left, top, right - left, bottom - top);
            var monitor = screens[monitorIndex].Bounds;

            return (vdesk, monitor);
        }

        private void MoveOverlayToVirtualDesktop(Rectangle virtualBounds)
        {
            _overlay.MoveToVirtualDesktop();
        }

        private void ShowRedBorder(Rectangle monitor, OverlayTransform tr, RecordingIndicatorOptions opt)
        {
            var half = opt.BorderThickness / 2.0f;

            var border = new RectangleItem(
                id: _borderId,
                globalLeft: monitor.Left + half,
                globalTop: monitor.Top + half,
                globalRight: monitor.Right - half,
                globalBottom: monitor.Bottom - half,
                fillColor: new Color(0, 0, 0, 0),
                strokeColor: opt.Color,
                strokeWidth: opt.BorderThickness)
            { Transform = tr };

            _overlay.AddItem(border);
        }

        private void ShowCornerBadge(Rectangle monitor, OverlayTransform tr, RecordingIndicatorOptions opt)
        {
            const int pad = 20;               // Abstand zum Monitor-Rand
            const float fontSize = 20f;       // Größere Schrift
            const float dotRadius = 14f;      // Größerer Punkt
            const int gap = 10;               // Abstand Punkt <-> Text
            const int outline = 1;            // Pixel-Offset für Outline

            // --- Eck-Position (Basis) in globalen Koordinaten ---
            int baseX = opt.BadgeCorner switch
            {
                Corner.TopLeft => monitor.Left + pad,
                Corner.BottomLeft => monitor.Left + pad,
                Corner.TopRight => monitor.Right - pad,
                Corner.BottomRight => monitor.Right - pad,
                _ => monitor.Left + pad
            };
            int baseY = opt.BadgeCorner switch
            {
                Corner.TopLeft => monitor.Top + pad,
                Corner.TopRight => monitor.Top + pad,
                Corner.BottomLeft => monitor.Bottom - pad,
                Corner.BottomRight => monitor.Bottom - pad,
                _ => monitor.Top + pad
            };

            // --- Textbreite grob schätzen, um Clamping zu ermöglichen ---
            // Faustregel: ~0,6 * fontSize pro Zeichen (UI-typische Sans-Serif)
            float estimatedTextWidth = Math.Max(40f, opt.Label.Length * fontSize * 0.6f);
            float estimatedTextHeight = fontSize * 1.4f; // Zeilenhöhe grob

            // --- Ankerpunkt so wählen, dass Text vollständig im Monitor bleibt ---
            // Wir platzieren den Text je Ecke standardmäßig „inwards“ und clampen anschließend.

            // Vorab: Text-Startkoordinaten (oben links) bestimmen
            int textX = baseX;
            int textY = baseY;

            bool rightSide = opt.BadgeCorner is Corner.TopRight or Corner.BottomRight;
            bool bottomSide = opt.BadgeCorner is Corner.BottomLeft or Corner.BottomRight;

            // Für rechte Ecken: Text nach links ausrichten (damit er ins Bild fällt)
            if (rightSide)
                textX = (int)(baseX - estimatedTextWidth);

            // Für untere Ecken: Text oberhalb des baseY anordnen
            if (bottomSide)
                textY = (int)(baseY - estimatedTextHeight);
            else
                textY = baseY - (int)(estimatedTextHeight * 0.75f); // leicht oberhalb für optische Zentrierung

            // Clamping innerhalb des Monitors (mit pad)
            textX = Math.Max(monitor.Left + pad, Math.Min(textX, (int)(monitor.Right - pad - estimatedTextWidth)));
            textY = Math.Max(monitor.Top + pad, Math.Min(textY, (int)(monitor.Bottom - pad - estimatedTextHeight)));

            // --- Punkt links neben dem Text ---
            int dotCenterY = (int)(textY + estimatedTextHeight * 0.55f); // vertikal ungefähr mittig der Textzeile
            int dotCenterX = (int)(textX - gap - dotRadius);

            // Falls links nicht genug Platz ist, nach innen schieben (selten nötig durch Clamping, aber sicherheitshalber)
            int minDotX = monitor.Left + pad + (int)dotRadius;
            if (dotCenterX < minDotX)
            {
                int shift = minDotX - dotCenterX;
                dotCenterX += shift;
                textX += shift + gap + (int)dotRadius; // Text nach rechts schieben, damit Abstand bleibt
            }

            // --- Elemente erzeugen ---
            // 1) Punkt mit kräftiger schwarzer Umrandung
            var dot = new NodeItem(_dotId, dotCenterX, dotCenterY, radius: dotRadius, label: "",
                                   fill: opt.Color,
                                   stroke: new Color(0, 0, 0, 220),
                                   strokeWidth: 3f,
                                   text: new Color(0, 0, 0, 0))
            { Transform = tr };

            // 2) Text mit Outline (vier Richtungen + diagonalen Offsets für vollere Kontur)
            var textColor = new Color(opt.Color.R, opt.Color.G, opt.Color.B, 255);
            var outlineColor = new Color(0, 0, 0, 255);

            // Acht Offsets für „volle“ Outline
            var outlineOffsets = new (int dx, int dy)[]
            {
        (-outline, 0), (outline, 0), (0, -outline), (0, outline),
        (-outline, -outline), (-outline, outline), (outline, -outline), (outline, outline)
            };

            // Outline-Items anlegen
            int i = 0;
            foreach (var (dx, dy) in outlineOffsets)
            {
                var outlineItem = new TextItem($"{_labelId}_ol{i++}", "Segoe UI Semibold", fontSize,
                                               opt.Label, outlineColor, textX + dx, textY + dy)
                { Transform = tr };
                _overlay.AddItem(outlineItem);
            }

            // Vordergrund-Text
            var label = new TextItem(_labelId, "Segoe UI Semibold", fontSize,
                                     opt.Label, textColor, textX, textY)
            { Transform = tr };

            // --- Hinzufügen ---
            _overlay.AddItem(dot);
            _overlay.AddItem(label);
        }

        private static int FindMonitorIndex(Rectangle monitor)
        {
            var arr = ScreenHelper.GetScreens();
            for (int i = 0; i < arr.Length; i++)
                if (arr[i].Bounds == monitor) return i;
            return 0;
        }
    }
}
