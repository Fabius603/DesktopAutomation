using GameOverlay.Drawing;
using Font = GameOverlay.Drawing.Font;
using SolidBrush = GameOverlay.Drawing.SolidBrush;
using Graphics = GameOverlay.Drawing.Graphics;
using Color = GameOverlay.Drawing.Color;


namespace DesktopOverlay.OverlayItems
{
    public sealed class TextItem : OverlayItemBase
    {
        private readonly string _fontName;
        private readonly float _fontSize;
        private readonly string _text;

        private readonly float _gx, _gy; // globaler Anker (linke obere Ecke)

        private Font _font;
        private SolidBrush _brush;
        private readonly Color _color;

        public TextItem(string id, string fontName, float fontSize, string text, Color color,
                        float globalX, float globalY)
            : base(id)
        {
            _fontName = fontName; _fontSize = fontSize;
            _text = text; _color = color;
            _gx = globalX; _gy = globalY;
        }

        public override void Setup(Graphics gfx, bool recreate)
        {
            if (recreate) { _font?.Dispose(); _brush?.Dispose(); }
            _font = gfx.CreateFont(_fontName, _fontSize);
            _brush = gfx.CreateSolidBrush(_color.R, _color.G, _color.B, _color.A);
        }

        public override void Draw(Graphics gfx)
        {
            if (!Visible) return;

            var p = Map(_gx, _gy); // global → lokal
            gfx.DrawText(_font, _brush, p.x, p.y, _text);
        }

        public override void Dispose()
        {
            _font?.Dispose(); _brush?.Dispose();
        }
    }
}
