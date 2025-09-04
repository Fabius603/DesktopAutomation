using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageHelperMethods
{
    public static class ClassConverter
    {
        // -------- System.Drawing -> OpenCvSharp --------

        public static OpenCvSharp.Point ToCv(System.Drawing.Point p) => new(p.X, p.Y);
        public static OpenCvSharp.Point? ToCv(System.Drawing.Point? p) => p.HasValue ? new OpenCvSharp.Point(p.Value.X, p.Value.Y) : (OpenCvSharp.Point?)null;

        public static OpenCvSharp.Size ToCv(System.Drawing.Size s) => new(s.Width, s.Height);
        public static OpenCvSharp.Size? ToCv(System.Drawing.Size? s) => s.HasValue ? new OpenCvSharp.Size(s.Value.Width, s.Value.Height) : (OpenCvSharp.Size?)null;

        public static OpenCvSharp.Rect ToCv(Rectangle r) => new(r.X, r.Y, r.Width, r.Height);
        public static OpenCvSharp.Rect? ToCv(Rectangle? r) => r.HasValue ? new OpenCvSharp.Rect(r.Value.X, r.Value.Y, r.Value.Width, r.Value.Height) : (OpenCvSharp.Rect?)null;

        /// <summary>Color (ARGB) → Scalar(B,G,R,A)</summary>
        public static Scalar ToCv(Color c) => new(c.B, c.G, c.R, c.A);

        // -------- OpenCvSharp -> System.Drawing --------

        public static System.Drawing.Point ToDrawing(OpenCvSharp.Point p) => new(p.X, p.Y);
        public static System.Drawing.Point? ToDrawing(OpenCvSharp.Point? p) => p.HasValue ? new System.Drawing.Point(p.Value.X, p.Value.Y) : (System.Drawing.Point?)null;

        public static System.Drawing.Size ToDrawing(OpenCvSharp.Size s) => new(s.Width, s.Height);
        public static System.Drawing.Size? ToDrawing(OpenCvSharp.Size? s) => s.HasValue ? new System.Drawing.Size(s.Value.Width, s.Value.Height) : (System.Drawing.Size?)null;

        public static Rectangle ToDrawing(OpenCvSharp.Rect r) => new(r.X, r.Y, r.Width, r.Height);
        public static Rectangle? ToDrawing(OpenCvSharp.Rect? r) => r.HasValue ? new Rectangle(r.Value.X, r.Value.Y, r.Value.Width, r.Value.Height) : (Rectangle?)null;

        /// <summary>Scalar(B,G,R,A) → Color(ARGB). Werte werden in 0..255 geklammert.</summary>
        public static Color ToDrawing(Scalar s)
        {
            byte Clamp(double v) => (byte)Math.Max(0, Math.Min(255, Math.Round(v)));
            return Color.FromArgb(Clamp(s.Val3), Clamp(s.Val2), Clamp(s.Val1)); // A optional separat
        }
    }
}
