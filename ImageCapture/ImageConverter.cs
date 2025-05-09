using OpenCvSharp;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Device = SharpDX.Direct3D11.Device;

namespace ImageCapture
{
    public static class ImageConverter
    {
        public static Mat ToMat(Texture2D image, Device device)
        {
            using var bitmap = Texture2DToBitmap(image, device);
            var mat = OpenCvSharp.Extensions.BitmapConverter.ToMat(bitmap);
            return mat;
        }

        private static Bitmap Texture2DToBitmap(Texture2D texture, Device device)
        {
            var textureDesc = texture.Description;

            var stagingDesc = new Texture2DDescription
            {
                Width = textureDesc.Width,
                Height = textureDesc.Height,
                MipLevels = 1,
                ArraySize = 1,
                Format = textureDesc.Format,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Staging,
                BindFlags = BindFlags.None,
                CpuAccessFlags = CpuAccessFlags.Read,
                OptionFlags = ResourceOptionFlags.None
            };

            using var stagingTex = new Texture2D(device, stagingDesc);
            device.ImmediateContext.CopyResource(texture, stagingTex);

            var dataBox = device.ImmediateContext.MapSubresource(stagingTex, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None);

            var bitmap = new Bitmap(textureDesc.Width, textureDesc.Height, PixelFormat.Format32bppArgb);
            var bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.WriteOnly,
                bitmap.PixelFormat);

            unsafe
            {
                int bytesPerPixel = 4; // bei Format32bppArgb
                int rowBytes = textureDesc.Width * bytesPerPixel;

                byte* srcRow = (byte*)dataBox.DataPointer;
                byte* dstRow = (byte*)bitmapData.Scan0;

                for (int y = 0; y < bitmapData.Height; y++)
                {
                    System.Buffer.MemoryCopy(
                        srcRow,                   
                        dstRow,                   
                        bitmapData.Stride,        
                        rowBytes                  
                    );

                    srcRow += dataBox.RowPitch;
                    dstRow += bitmapData.Stride;
                }
            }

            bitmap.UnlockBits(bitmapData);
            device.ImmediateContext.UnmapSubresource(stagingTex, 0);

            return bitmap;
        }
    }
}
