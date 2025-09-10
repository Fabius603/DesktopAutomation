using System.Drawing;
using Microsoft.ML.OnnxRuntime;

namespace ImageDetection.Model
{
    public sealed class YoloBuffers
    {
        public readonly int Size;
        public readonly Bitmap Canvas;          // wiederverwendete Zeichenfläche (Size x Size)
        public readonly float[] Input;          // NCHW float32
        public readonly long[] InputShape;      // {1,3,Size,Size}
        public float[]? Output;                 // nach erstem Lauf dimensionieren
        public int[]? OutputDims;               // z.B. [1,84,N] oder [1,N,84]
        public OrtIoBinding? Binding;
        public string? InputName;
        public string? OutputName;

        public YoloBuffers(int size)
        {
            Size = size;
            Canvas = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            Input = new float[1 * 3 * size * size];
            InputShape = new long[] { 1, 3, size, size };
        }
    }
}