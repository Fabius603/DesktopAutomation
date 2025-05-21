using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.Mathematics.Interop;

namespace ImageCapture.DesktopDuplication
{
    public class PointerInfo
    {
        public byte[] PtrShapeBuffer;
        public int BufferSize;
        public OutputDuplicatePointerShapeInformation ShapeInfo;
        public RawPoint Position;
        public bool Visible;
        public long LastTimeStamp;
        public int WhoUpdatedPositionLast = -1; // To track which output updated the position last
    }
}
