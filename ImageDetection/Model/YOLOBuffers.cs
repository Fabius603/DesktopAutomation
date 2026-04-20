using Microsoft.ML.OnnxRuntime;

namespace ImageDetection.Model
{
    public sealed class YoloBuffers
    {
        public readonly int Size;
        public readonly float[] Input;          // NCHW float32 (Preprocessing immer in float32)
        public readonly long[] InputShape;      // {1,3,Size,Size}
        public Float16[]? InputFp16;            // nur befüllt wenn Modell Float16 erwartet
        public bool IsFloat16Input;
        public float[]? Output;                 // nach erstem Lauf dimensionieren (immer float32)
        public Float16[]? OutputFp16;           // nur befüllt wenn Modell Float16 ausgibt
        public bool IsFloat16Output;
        public int[]? OutputDims;               // z.B. [1,84,N] oder [1,N,84]
        public OrtIoBinding? Binding;
        public string? InputName;
        public string? OutputName;

        public YoloBuffers(int size)
        {
            Size = size;
            Input = new float[1 * 3 * size * size];
            InputShape = new long[] { 1, 3, size, size };
        }
    }
}