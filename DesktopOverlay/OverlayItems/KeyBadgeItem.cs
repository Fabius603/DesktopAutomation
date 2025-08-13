using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Font = GameOverlay.Drawing.Font;
using SolidBrush = GameOverlay.Drawing.SolidBrush;
using Graphics = GameOverlay.Drawing.Graphics;
using Color = GameOverlay.Drawing.Color;

namespace DesktopOverlay.OverlayItems
{
    public sealed class KeyBadgeItem : OverlayItemBase, ITimedOverlayItem
    {
        private readonly float _gx, _gy; // globales Anker-LeftTop
        private readonly double _t0, _t1;
        private readonly string _label;

        private SolidBrush _bg, _fg;
        private Font _font;

        public KeyBadgeItem(string id, string label, float globalX, float globalY, double startSeconds, double endSeconds)
            : base(id)
        {
            _label = label;
            _gx = globalX; _gy = globalY;
            _t0 = startSeconds; _t1 = endSeconds;
        }

        public override void Setup(Graphics gfx, bool recreate)
        {
            if (recreate) { _bg?.Dispose(); _fg?.Dispose(); _font?.Dispose(); }
            _bg = gfx.CreateSolidBrush(30, 30, 30, 190);
            _fg = gfx.CreateSolidBrush(240, 240, 240, 240);
            _font = gfx.CreateFont("Segoe UI Semibold", 13f);
        }

        public void Update(double t) => Visible = t >= _t0 && t <= _t1;

        public override void Draw(Graphics gfx)
        {
            if (!Visible) return;

            // Anker ist linke obere Ecke in GLOBAL; in LOCAL mappen:
            var p = Map(_gx, _gy);

            var size = gfx.MeasureString(_font, _label);
            float pad = 6f, w = size.X + pad * 2, h = size.Y + pad * 2;

            gfx.FillRoundedRectangle(_bg, p.x, p.y, p.x + w, p.y + h, 6);
            gfx.DrawText(_font, _fg, p.x + pad, p.y + pad, _label);
        }

        public override void Dispose() { _bg?.Dispose(); _fg?.Dispose(); _font?.Dispose(); }
    }
}
