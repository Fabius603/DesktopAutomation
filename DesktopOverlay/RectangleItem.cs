using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameOverlay.Drawing;
using SolidBrush = GameOverlay.Drawing.SolidBrush;
using Color = GameOverlay.Drawing.Color;
using Graphics = GameOverlay.Drawing.Graphics;

namespace DesktopOverlay
{
    /// <summary>
    /// Item zum Zeichnen von Rechtecken.
    /// </summary>
    public class RectangleItem : OverlayItemBase
    {
        private readonly Color _fillColor;
        private readonly Color _strokeColor;
        private readonly float _strokeWidth;

        private SolidBrush _fillBrush;
        private SolidBrush _strokeBrush;

        public float Left { get; set; }
        public float Right { get; set; }
        public float Top { get; set; }
        public float Bottom { get; set; }

        public RectangleItem(
            string id,
            Color fillColor,
            Color strokeColor,
            float strokeWidth,
            float left, float right, float top, float bottom)
            : base(id)
        {
            _fillColor = fillColor;
            _strokeColor = strokeColor;
            _strokeWidth = strokeWidth;
            Left = left; Right = right;
            Top = top; Bottom = bottom;
        }

        public override void Setup(Graphics gfx, bool recreate)
        {
            if (recreate)
            {
                _fillBrush?.Dispose();
                _strokeBrush?.Dispose();
            }
            _fillBrush = gfx.CreateSolidBrush(_fillColor.R, _fillColor.G, _fillColor.B, _fillColor.A);
            _strokeBrush = gfx.CreateSolidBrush(_strokeColor.R, _strokeColor.G, _strokeColor.B, _strokeColor.A);
        }

        public override void Draw(Graphics gfx)
        {
            if (!Visible) return;
            gfx.FillRectangle(_fillBrush, Left, Top, Right, Bottom);
            if (_strokeWidth > 0)
                gfx.DrawRectangle(_strokeBrush, Left, Top, Right, Bottom, _strokeWidth);
        }

        public override void Dispose()
        {
            _fillBrush?.Dispose();
            _strokeBrush?.Dispose();
        }
    }
}
