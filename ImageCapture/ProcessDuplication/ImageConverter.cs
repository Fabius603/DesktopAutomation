using System;
using System.Drawing;
using System.Drawing.Imaging;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using OpenCvSharp;
using Device = SharpDX.Direct3D11.Device;

namespace ImageCapture.ProcessDuplication
{
    public static class ImageConverter
    {
        /// <summary>
        /// Konvertiert Texture2D direkt zu Bitmap - effizienter als über Mat
        /// </summary>
        public static Bitmap ToBitmap(Texture2D image, Device device)
        {
            return Texture2DToBitmap(image, device);
        }

        /// <summary>
        /// Legacy-Methode - konvertiert über Bitmap für bessere Performance
        /// </summary>
        [Obsolete("Diese Methode ist weniger effizient. Verwenden Sie ToBitmap() für bessere Performance.")]
        public static Mat ToMat(Texture2D image, Device device)
        {
            // Temporäre Bitmap erzeugen und in Mat konvertieren
            using var bitmap = Texture2DToBitmap(image, device);
            return OpenCvSharp.Extensions.BitmapConverter.ToMat(bitmap);
        }

        private static Bitmap Texture2DToBitmap(Texture2D texture, Device device)
        {
            var desc = texture.Description;
            var stagingDesc = new Texture2DDescription
            {
                Width = desc.Width,
                Height = desc.Height,
                MipLevels = 1,
                ArraySize = 1,
                Format = desc.Format,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Staging,
                BindFlags = BindFlags.None,
                CpuAccessFlags = CpuAccessFlags.Read,
                OptionFlags = ResourceOptionFlags.None
            };

            using var stagingTex = new Texture2D(device, stagingDesc);
            device.ImmediateContext.CopyResource(texture, stagingTex);

            // Map/Unmap stets in try/finally
            DataBox dataBox = default;
            try
            {
                dataBox = device.ImmediateContext.MapSubresource(
                    stagingTex,
                    0,
                    MapMode.Read,
                    SharpDX.Direct3D11.MapFlags.None);

                // Bitmap erstellen
                var bmp = new Bitmap(desc.Width, desc.Height, PixelFormat.Format32bppArgb);

                // LockBits/UnlockBits stets in try/finally
                var bmpData = bmp.LockBits(
                    new Rectangle(0, 0, bmp.Width, bmp.Height),
                    ImageLockMode.WriteOnly,
                    bmp.PixelFormat);
                try
                {
                    unsafe
                    {
                        var srcPtr = (byte*)dataBox.DataPointer;
                        var dstPtr = (byte*)bmpData.Scan0;
                        int rowBytes = desc.Width * 4;

                        for (int y = 0; y < bmpData.Height; y++)
                        {
                            System.Buffer.MemoryCopy(
                                srcPtr,
                                dstPtr,
                                bmpData.Stride,
                                rowBytes);
                            srcPtr += dataBox.RowPitch;
                            dstPtr += bmpData.Stride;
                        }
                    }
                }
                finally
                {
                    bmp.UnlockBits(bmpData);
                }

                return bmp;
            }
            finally
            {
                // Nur unmap wenn gemappt
                if (dataBox.DataPointer != nint.Zero)
                    device.ImmediateContext.UnmapSubresource(stagingTex, 0);
            }
        }
    }
}
