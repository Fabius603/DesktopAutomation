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
    public sealed class TimeoutBadgeItem : OverlayItemBase, ITimedOverlayItem
    {
        private readonly float _gx, _gy; // globales Ankerzentrum
        private readonly double _t0, _t1;
        private readonly int _ms;

        private SolidBrush _bg, _fg;
        private Font _font;

        public TimeoutBadgeItem(string id, float globalX, float globalY, double startSeconds, int durationMs)
            : base(id)
        {
            _gx = globalX; _gy = globalY;
            _t0 = startSeconds; _t1 = startSeconds + durationMs / 1000.0;
            _ms = durationMs;
        }

        public override void Setup(Graphics gfx, bool recreate)
        {
            if (recreate) { _bg?.Dispose(); _fg?.Dispose(); _font?.Dispose(); }
            _bg = gfx.CreateSolidBrush(0, 0, 0, 160);
            _fg = gfx.CreateSolidBrush(255, 255, 255, 240);
            _font = gfx.CreateFont("Segoe UI", 12f);
        }

        public void Update(double t) => Visible = t >= _t0 && t <= _t1;

        public override void Draw(Graphics gfx)
        {
            if (!Visible) return;

            var p = Map(_gx, _gy);
            string text = $"+{_ms} ms";

            var size = gfx.MeasureString(_font, text);
            float pad = 6f, w = size.X + pad * 2, h = size.Y + pad * 2;

            gfx.FillRoundedRectangle(_bg, p.x - w / 2, p.y - h - 20, p.x + w / 2, p.y - 20, 6);
            gfx.DrawText(_font, _fg, p.x - size.X / 2, p.y - h + pad - 20, text);
        }

        public override void Dispose() { _bg?.Dispose(); _fg?.Dispose(); _font?.Dispose(); }
    }
}
