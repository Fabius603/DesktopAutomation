using DesktopOverlay;
using DesktopOverlay.OverlayItems;
using ImageHelperMethods;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
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
        private long _renderVersion;
        private const int MaxDisplayedDetections = 200;

        // ── Pro-Step Text-Tracking ────────────────────────────────────────────────
        private sealed class TextEntry
        {
            public string ItemId      { get; }
            public Timer? Timer       { get; set; }
            public bool   ClearOnJobEnd { get; }
            public TextEntry(string itemId, bool clearOnJobEnd) { ItemId = itemId; ClearOnJobEnd = clearOnJobEnd; }
        }
        private readonly Dictionary<string, TextEntry> _textEntries = new();
        private const string TextItemIdPrefix = "user_text_";

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
                var version = unchecked(++_renderVersion);
                var visibleDetections = allDetections.Take(MaxDisplayedDetections).ToList();

                for (int i = 0; i < visibleDetections.Count; i++)
                {
                    var det    = visibleDetections[i];
                    bool isBest = i == 0;
                    var fillColor   = ColorTransparent;
                    var strokeColor = isBest ? ColorBestStroke  : ColorOtherStroke;
                    var nodeColor   = isBest ? ColorBestFill    : ColorOtherFill;

                    if (det.BoundingBox.HasValue)
                    {
                        var bb  = det.BoundingBox.Value;
                        var bbId = $"det_bbox_{version}_{i}";
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

                    var cpId = $"det_center_{version}_{i}";
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

        public void ShowText(string stepKey, string text, float fontSize,
                             byte r, byte g, byte b, byte a,
                             int desktopIndex, int offsetX, int offsetY,
                             int durationMs, bool clearOnJobEnd)
        {
            lock (_lock)
            {
                // Leerer Text → nur diesen Step-Text entfernen
                if (string.IsNullOrEmpty(text))
                {
                    ClearTextLocked(stepKey);
                    return;
                }

                // Step läuft erneut, Text noch sichtbar → nur Timer verlängern
                if (_textEntries.TryGetValue(stepKey, out var existing))
                {
                    existing.Timer?.Dispose();
                    existing.Timer = durationMs > 0
                        ? new Timer(_ => { lock (_lock) { ClearTextLocked(stepKey); } }, null, durationMs, Timeout.Infinite)
                        : null;
                    return;
                }

                // Neues Text-Item erstellen
                var overlay = EnsureOverlay();

                var screens = ScreenHelper.GetScreens();
                var screen  = screens.Length > 0
                    ? screens[Math.Clamp(desktopIndex, 0, screens.Length - 1)]
                    : System.Windows.Forms.Screen.PrimaryScreen!;

                float globalX = screen.Bounds.X + offsetX;
                float globalY = screen.Bounds.Y + offsetY;

                var vd = ScreenHelper.GetVirtualDesktopBounds();
                var tr = OverlayTransform.FromVirtualToOverlayLocal(
                    vd, new Rectangle(0, 0, vd.Width, vd.Height));

                var color  = new Color(r, g, b, a);
                var itemId = TextItemIdPrefix + Guid.NewGuid().ToString("N");

                overlay.AddItem(new TextItem(
                    id:       itemId,
                    fontName: "Arial",
                    fontSize: fontSize,
                    text:     text,
                    color:    color,
                    globalX:  globalX,
                    globalY:  globalY)
                { Transform = tr });

                var entry = new TextEntry(itemId, clearOnJobEnd);
                entry.Timer = durationMs > 0
                    ? new Timer(_ => { lock (_lock) { ClearTextLocked(stepKey); } }, null, durationMs, Timeout.Infinite)
                    : null;
                _textEntries[stepKey] = entry;
            }
        }

        // Muss unter _lock aufgerufen werden.
        private void ClearTextLocked(string stepKey)
        {
            if (!_textEntries.TryGetValue(stepKey, out var entry)) return;
            entry.Timer?.Dispose();
            _overlay?.RemoveItem(entry.ItemId);
            _textEntries.Remove(stepKey);
        }

        public void ClearText()
        {
            lock (_lock)
            {
                foreach (var key in _textEntries.Keys.ToList())
                    ClearTextLocked(key);
            }
        }

        public void OnJobEnded()
        {
            lock (_lock)
            {
                foreach (var key in _textEntries.Where(kv => kv.Value.ClearOnJobEnd).Select(kv => kv.Key).ToList())
                    ClearTextLocked(key);
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                foreach (var entry in _textEntries.Values)
                    entry.Timer?.Dispose();
                _textEntries.Clear();
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
