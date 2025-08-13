using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameOverlay.Drawing;
using SolidBrush = GameOverlay.Drawing.SolidBrush;
using Color = GameOverlay.Drawing.Color;
using Graphics = GameOverlay.Drawing.Graphics;

namespace DesktopOverlay.OverlayItems
{
    public sealed class RectangleItem : OverlayItemBase
    {
        private readonly float _gLeft, _gTop, _gRight, _gBottom; // globale Kanten
        private readonly float _strokeWidth;
        private readonly Color _fillColor, _strokeColor;

        private SolidBrush _fillBrush, _strokeBrush;

        public RectangleItem(string id, float globalLeft, float globalTop, float globalRight, float globalBottom,
                             Color fillColor, Color strokeColor, float strokeWidth)
            : base(id)
        {
            _gLeft = globalLeft; _gTop = globalTop; _gRight = globalRight; _gBottom = globalBottom;
            _fillColor = fillColor; _strokeColor = strokeColor; _strokeWidth = strokeWidth;
        }

        public override void Setup(Graphics gfx, bool recreate)
        {
            if (recreate)
            {
                _fillBrush?.Dispose(); _strokeBrush?.Dispose();
            }
            _fillBrush = gfx.CreateSolidBrush(_fillColor.R, _fillColor.G, _fillColor.B, _fillColor.A);
            _strokeBrush = gfx.CreateSolidBrush(_strokeColor.R, _strokeColor.G, _strokeColor.B, _strokeColor.A);
        }

        public override void Draw(Graphics gfx)
        {
            if (!Visible) return;

            var p1 = Map(_gLeft, _gTop);
            var p2 = Map(_gRight, _gBottom);

            float l = System.Math.Min(p1.x, p2.x);
            float r = System.Math.Max(p1.x, p2.x);
            float t = System.Math.Min(p1.y, p2.y);
            float b = System.Math.Max(p1.y, p2.y);

            gfx.FillRectangle(_fillBrush, l, t, r, b);
            if (_strokeWidth > 0)
                gfx.DrawRectangle(_strokeBrush, l, t, r, b, _strokeWidth);
        }

        public override void Dispose()
        {
            _fillBrush?.Dispose(); _strokeBrush?.Dispose();
        }
    }
}
