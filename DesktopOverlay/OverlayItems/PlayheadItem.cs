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
    public sealed class PlayheadItem : OverlayItemBase, ITimedOverlayItem
    {
        public sealed record SegmentGlobal((float x, float y) A, (float x, float y) B, double Start, double End);

        private readonly List<SegmentGlobal> _segments; // alle in GLOBALEN Pixeln
        private readonly float _radius;                  // lokaler Radius
        private readonly Color _color;

        private SolidBrush _brush;
        private float _cx, _cy; // lokale Position

        public PlayheadItem(string id, IEnumerable<SegmentGlobal> segments, float radius, Color color)
            : base(id)
        {
            _segments = new List<SegmentGlobal>(segments);
            _radius = radius; _color = color;
        }

        public override void Setup(Graphics gfx, bool recreate)
        {
            if (recreate) _brush?.Dispose();
            _brush = gfx.CreateSolidBrush(_color.R, _color.G, _color.B, _color.A);
        }

        public void Update(double t)
        {
            Visible = _segments.Count > 0;
            if (!Visible) return;

            foreach (var s in _segments)
            {
                if (t < s.Start || t > s.End) continue;

                double r = (s.End - s.Start) <= 0 ? 1.0 : (t - s.Start) / (s.End - s.Start);
                float gx = s.A.x + (float)((s.B.x - s.A.x) * r);
                float gy = s.A.y + (float)((s.B.y - s.A.y) * r);

                var p = Map(gx, gy); // global → lokal
                _cx = p.x; _cy = p.y;
                return;
            }

            var last = _segments[^1];
            var pend = Map(last.B.x, last.B.y);
            _cx = pend.x; _cy = pend.y;
        }

        public override void Draw(Graphics gfx)
        {
            if (!Visible) return;
            gfx.FillCircle(_brush, _cx, _cy, _radius);
        }

        public override void Dispose() => _brush?.Dispose();
    }
}
