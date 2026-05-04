using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DesktopAutomationApp.Views
{
    public partial class ImagePreviewWindow : Window
    {
        private WriteableBitmap? _wbm;

        public ImagePreviewWindow(string title)
        {
            InitializeComponent();
            Title = title;
        }

        /// <summary>
        /// Aktualisiert das angezeigte Bild möglichst effizient per <see cref="WriteableBitmap.WritePixels"/>.
        /// Muss auf dem UI-Thread aufgerufen werden.
        /// </summary>
        public void UpdateBitmap(Bitmap bmp)
        {
            if (bmp == null) return;

            int w = bmp.Width;
            int h = bmp.Height;

            // WriteableBitmap einmalig anlegen bzw. bei Bildgrößenänderung neu erstellen.
            if (_wbm == null || _wbm.PixelWidth != w || _wbm.PixelHeight != h)
            {
                _wbm = new WriteableBitmap(w, h, 96, 96, PixelFormats.Bgra32, null);
                PreviewImage.Source = _wbm;
            }

            // Pixel direkt aus GDI-Bitmap in WriteableBitmap schreiben – keine Zwischenkopie.
            BitmapData? data = null;
            try
            {
                data = bmp.LockBits(
                    new System.Drawing.Rectangle(0, 0, w, h),
                    ImageLockMode.ReadOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                _wbm.Lock();
                _wbm.WritePixels(
                    new Int32Rect(0, 0, w, h),
                    data.Scan0,
                    Math.Abs(data.Stride) * h,
                    data.Stride);
                _wbm.Unlock();
            }
            finally
            {
                if (data != null) bmp.UnlockBits(data);
            }
        }
    }
}
