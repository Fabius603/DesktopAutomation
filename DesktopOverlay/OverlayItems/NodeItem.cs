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
    public sealed class NodeItem : OverlayItemBase
    {
        private readonly float _gx, _gy;      // globale (virtuelle) Pixel
        private readonly float _radius;       // lokaler Radius in Overlay-Pixeln
        private readonly string _label;

        private readonly Color _fill, _stroke, _text;
        private readonly float _strokeWidth;

        private SolidBrush _fillBrush, _strokeBrush, _textBrush;
        private Font _font;

        public NodeItem(string id, float globalX, float globalY, float radius, string label,
                        Color fill, Color stroke, float strokeWidth, Color text)
            : base(id)
        {
            _gx = globalX; _gy = globalY; _radius = radius;
            _label = label;
            _fill = fill; _stroke = stroke; _text = text; _strokeWidth = strokeWidth;
        }

        public override void Setup(Graphics gfx, bool recreate)
        {
            if (recreate)
            {
                _fillBrush?.Dispose(); _strokeBrush?.Dispose(); _textBrush?.Dispose(); _font?.Dispose();
            }
            _fillBrush = gfx.CreateSolidBrush(_fill.R, _fill.G, _fill.B, _fill.A);
            _strokeBrush = gfx.CreateSolidBrush(_stroke.R, _stroke.G, _stroke.B, _stroke.A);
            _textBrush = gfx.CreateSolidBrush(_text.R, _text.G, _text.B, _text.A);
            _font = gfx.CreateFont("Consolas", 12f);
        }

        public override void Draw(Graphics gfx)
        {
            if (!Visible) return;

            var p = Map(_gx, _gy); // global → lokal
            gfx.FillCircle(_fillBrush, p.x, p.y, _radius);
            gfx.DrawCircle(_strokeBrush, p.x, p.y, _radius, _strokeWidth);

            var size = gfx.MeasureString(_font, _label);
            gfx.DrawText(_font, _textBrush, p.x - size.X / 2f, p.y - size.Y / 2f, _label);
        }

        public override void Dispose()
        {
            _fillBrush?.Dispose(); _strokeBrush?.Dispose(); _textBrush?.Dispose(); _font?.Dispose();
        }
    }
}
