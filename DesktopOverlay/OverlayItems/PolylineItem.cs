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
    public sealed class PolylineItem : OverlayItemBase
    {
        private readonly List<(float x, float y)> _ptsGlobal;
        private readonly float _thickness;
        private readonly Color _color;
        private SolidBrush _brush;

        /// <summary>0..1 – Anteil des Pfades (für Playback-Highlight). Null = kompletter Pfad.</summary>
        public float? Progress { get; set; } = null;

        public PolylineItem(string id, IEnumerable<(float x, float y)> globalPoints, float thickness, Color color)
            : base(id)
        {
            _ptsGlobal = new(globalPoints);
            _thickness = thickness;
            _color = color;
        }

        public override void Setup(Graphics gfx, bool recreate)
        {
            if (recreate) _brush?.Dispose();
            _brush = gfx.CreateSolidBrush(_color.R, _color.G, _color.B, _color.A);
        }

        public override void Draw(Graphics gfx)
        {
            if (!Visible || _ptsGlobal.Count < 2) return;

            // Lokale Punkte „on the fly“ mappen
            (float x, float y) P(int i) => Map(_ptsGlobal[i].x, _ptsGlobal[i].y);

            if (Progress is null)
            {
                for (int i = 0; i < _ptsGlobal.Count - 1; i++)
                {
                    var a = P(i);
                    var b = P(i + 1);
                    gfx.DrawLine(_brush, a.x, a.y, b.x, b.y, _thickness);
                }
                return;
            }

            // Teilpfad mit Längen in LOCAL berechnen (robust bei Skalierung)
            float total = 0f;
            var segLen = new float[_ptsGlobal.Count - 1];
            var a0 = P(0);
            var prev = a0;
            for (int i = 0; i < segLen.Length; i++)
            {
                var cur = P(i + 1);
                float dx = cur.x - prev.x, dy = cur.y - prev.y;
                float len = (float)System.Math.Sqrt(dx * dx + dy * dy);
                segLen[i] = len; total += len; prev = cur;
            }

            float p = System.Math.Clamp(Progress.Value, 0f, 1f);
            float target = total * p, acc = 0f;

            for (int i = 0; i < segLen.Length; i++)
            {
                var a = i == 0 ? a0 : P(i);
                var b = P(i + 1);
                float next = acc + segLen[i];
                if (target >= next)
                {
                    gfx.DrawLine(_brush, a.x, a.y, b.x, b.y, _thickness);
                }
                else
                {
                    float r = segLen[i] == 0 ? 0 : (target - acc) / segLen[i];
                    float x = a.x + (b.x - a.x) * r;
                    float y = a.y + (b.y - a.y) * r;
                    gfx.DrawLine(_brush, a.x, a.y, x, y, _thickness);
                    break;
                }
                acc = next;
            }
        }

        public override void Dispose() => _brush?.Dispose();
    }
}
