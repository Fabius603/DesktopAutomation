using System;
using DesktopOverlay.OverlayItems;
using Graphics = GameOverlay.Drawing.Graphics;
using Font = GameOverlay.Drawing.Font;
using SolidBrush = GameOverlay.Drawing.SolidBrush;
using Color = GameOverlay.Drawing.Color;

namespace DesktopOverlay.OverlayItems
{
    /// <summary>
    /// Zeigt einen Key groß an einer festen globalen Position.
    /// Größe so skaliert, dass der Text sehr gut lesbar ist.
    /// </summary>
    public sealed class KeyItem : OverlayItemBase, ITimedOverlayItem
    {
        private readonly string _label;
        private readonly float _gx, _gy; // globale Koordinaten
        private readonly double _t0, _t1;

        private SolidBrush _bg, _fg;
        private Font _font;
        private float _fontSize;
        private float _cornerRadius;

        public KeyItem(
            string id,
            string label,
            float globalX,
            float globalY,
            double startSeconds,
            double endSeconds) : base(id)
        {
            _label = label ?? "";
            _gx = globalX;
            _gy = globalY;
            _t0 = startSeconds;
            _t1 = endSeconds;
        }

        public void Update(double t) => Visible = t >= _t0 && t <= _t1;

        public override void Setup(Graphics gfx, bool recreate)
        {
            if (recreate)
            {
                _bg?.Dispose();
                _fg?.Dispose();
                _font?.Dispose();
            }

            _bg = gfx.CreateSolidBrush(30, 30, 30, 210);
            _fg = gfx.CreateSolidBrush(255, 255, 255, 245);

            // Basisgröße für Font
            _fontSize = 100f; // Ausgangswert
            _font = gfx.CreateFont("Segoe UI Semibold", _fontSize, bold: true);
            _cornerRadius = 12f;
        }

        public override void Draw(Graphics gfx)
        {
            if (!Visible) return;

            // Position in LOCAL berechnen
            var p = Map(_gx, _gy);

            // Textgröße messen
            var size = gfx.MeasureString(_font, _label);

            // Boxgröße anpassen
            float padX = MathF.Max(12f, size.X * 0.06f);
            float padY = MathF.Max(8f, size.Y * 0.20f);
            float w = size.X + padX * 2;
            float h = size.Y + padY * 2;

            // So zeichnen, dass (_gx, _gy) der Mittelpunkt ist
            float x = p.x - w * 0.5f;
            float y = p.y - h * 0.5f;

            gfx.FillRoundedRectangle(_bg, x, y, x + w, y + h, _cornerRadius);
            gfx.DrawText(_font, _fg, x + padX, y + padY, _label);
        }

        public override void Dispose()
        {
            _bg?.Dispose();
            _fg?.Dispose();
            _font?.Dispose();
        }
    }
}
