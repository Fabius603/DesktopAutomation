using DesktopOverlay;
using DesktopOverlay.OverlayItems;
using ImageHelperMethods;
using System.Collections.Generic;
using System.Drawing;
using TaskAutomation.Events;
using Color = GameOverlay.Drawing.Color;

namespace DesktopAutomationApp.Services
{
    /// <summary>
    /// Zeichnet alle Erkennungsergebnisse eines Detection-Steps
    /// auf ein transparentes Desktop-Overlay-Fenster, das den gesamten virtuellen Desktop abdeckt.
    /// Index 0 = bestes Ergebnis (OrangeRed), alle weiteren in Grün.
    /// Wird als Singleton registriert; das Overlay-Fenster wird beim ersten Aufruf lazy gestartet.
    /// </summary>
    public sealed class WpfDesktopResultOverlay : IDesktopResultOverlay
    {
        private Overlay? _overlay;
        private readonly object _lock = new();
        private readonly List<string> _currentItemIds = new();

        // Farben: bestes Ergebnis = OrangeRed, weitere = Grün
        private static readonly Color ColorBestFill    = new(255,  0,   0, 255);   // knalliges Rot-Orange, voll opak
        private static readonly Color ColorOtherFill   = new(  0, 230,   0, 255);   // knalliges Grün, voll opak
        private static readonly Color ColorBestStroke  = new(255, 0,   0, 255);   // helles Orange für BoundingBox
        private static readonly Color ColorOtherStroke = new(  0, 230,   0, 255);   // wie Fill
        private static readonly Color ColorTransparent = new(  0,   0,   0,   0);
        private static readonly Color ColorWhiteAlpha  = new(255, 255, 255, 230);   // kräftiger weißer Ring

        public void ShowResult(IReadOnlyList<(Point Center, Rectangle? BoundingBox)> allDetections)
        {
            var overlay = EnsureOverlay();

            lock (_lock)
            {
                foreach (var id in _currentItemIds)
                    overlay.RemoveItem(id);
                _currentItemIds.Clear();

                if (allDetections.Count == 0) return;

                var vd = ScreenHelper.GetVirtualDesktopBounds();
                var tr = OverlayTransform.FromVirtualToOverlayLocal(
                    vd, new Rectangle(0, 0, vd.Width, vd.Height));

                for (int i = 0; i < allDetections.Count; i++)
                {
                    var det    = allDetections[i];
                    bool isBest = i == 0;
                    var fillColor   = ColorTransparent;
                    var strokeColor = isBest ? ColorBestStroke  : ColorOtherStroke;
                    var nodeColor   = isBest ? ColorBestFill    : ColorOtherFill;

                    if (det.BoundingBox.HasValue)
                    {
                        var bb  = det.BoundingBox.Value;
                        var bbId = $"det_bbox_{i}";
                        overlay.AddItem(new RectangleItem(
                            id:           bbId,
                            globalLeft:   bb.Left,
                            globalTop:    bb.Top,
                            globalRight:  bb.Right,
                            globalBottom: bb.Bottom,
                            fillColor:    fillColor,
                            strokeColor:  strokeColor,
                            strokeWidth:  2f)
                        { Transform = tr });
                        _currentItemIds.Add(bbId);
                    }

                    var cpId = $"det_center_{i}";
                    overlay.AddItem(new NodeItem(
                        id:          cpId,
                        globalX:     det.Center.X,
                        globalY:     det.Center.Y,
                        radius:      6f,
                        label:       "",
                        fill:        nodeColor,
                        stroke:      ColorWhiteAlpha,
                        strokeWidth: 1.5f,
                        text:        ColorTransparent)
                    { Transform = tr });
                    _currentItemIds.Add(cpId);
                }
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                if (_overlay == null) return;
                foreach (var id in _currentItemIds)
                    _overlay.RemoveItem(id);
                _currentItemIds.Clear();
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _overlay?.Dispose();
                _overlay = null;
            }
        }

        private Overlay EnsureOverlay()
        {
            lock (_lock)
            {
                if (_overlay != null) return _overlay;

                var vd = ScreenHelper.GetVirtualDesktopBounds();
                _overlay = new Overlay(vd.Left, vd.Top, vd.Width, vd.Height);
                _overlay.RunInNewThread();
                return _overlay;
            }
        }
    }
}
