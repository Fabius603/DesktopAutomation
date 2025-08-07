using GameOverlay.Drawing;
using Font = GameOverlay.Drawing.Font;
using SolidBrush = GameOverlay.Drawing.SolidBrush;
using Graphics = GameOverlay.Drawing.Graphics;
using Color = GameOverlay.Drawing.Color;


namespace DesktopOverlay
{
    public class TextItem : OverlayItemBase
    {
        private readonly string _fontName;
        private readonly float _fontSize;
        private readonly string _text;
        private readonly Color _color;

        private Font _font;
        private SolidBrush _brush;

        public float X { get; set; }
        public float Y { get; set; }

        public TextItem(string id, string fontName, float fontSize, string text, Color color, float x, float y)
            : base(id)
        {
            _fontName = fontName;
            _fontSize = fontSize;
            _text = text;
            _color = color;
            X = x;
            Y = y;
        }

        public override void Setup(Graphics gfx, bool recreate)
        {
            if (recreate)
            {
                _font?.Dispose();
                _brush?.Dispose();
            }
            _font = gfx.CreateFont(_fontName, _fontSize);
            _brush = gfx.CreateSolidBrush(_color.R, _color.G, _color.B, _color.A);
        }

        public override void Draw(Graphics gfx)
        {
            if (!Visible) return;
            gfx.DrawText(_font, _brush, X, Y, _text);
        }

        public override void Dispose()
        {
            _font?.Dispose();
            _brush?.Dispose();
        }
    }

}
