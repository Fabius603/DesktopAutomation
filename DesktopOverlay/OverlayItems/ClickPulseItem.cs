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
    public sealed class ClickPulseItem : OverlayItemBase, ITimedOverlayItem
    {
        private readonly float _gx, _gy; // global
        private readonly bool _isDown;
        private readonly double _t0, _t1;
        private readonly Color _color;

        private SolidBrush _brush;

        public ClickPulseItem(string id, float globalX, float globalY, bool isDown,
                              double startSeconds, double durationSeconds, Color color)
            : base(id)
        {
            _gx = globalX; _gy = globalY; _isDown = isDown;
            _t0 = startSeconds; _t1 = startSeconds + durationSeconds;
            _color = color;
        }

        public override void Setup(Graphics gfx, bool recreate)
        {
            if (recreate) _brush?.Dispose();
            _brush = gfx.CreateSolidBrush(_color.R, _color.G, _color.B, _color.A);
        }

        public void Update(double t) => Visible = t >= _t0 && t <= _t1;

        public override void Draw(Graphics gfx)
        {
            if (!Visible) return;

            var p = Map(_gx, _gy); // global → lokal
            double phase = (System.DateTime.UtcNow.Ticks % 300_0000) / 300_0000.0; // ~300 ms
            float r = 8f + (float)(10 * phase);
            float w = _isDown ? 3f : 1.5f;

            gfx.DrawCircle(_brush, p.x, p.y, r, w);
        }

        public override void Dispose() => _brush?.Dispose();
    }
}
