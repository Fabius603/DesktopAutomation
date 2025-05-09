using System;
using System.Runtime.InteropServices;
using OpenCvSharp;
using SharpDX.DXGI;
using SharpDX.Direct3D11;
using Device = SharpDX.Direct3D11.Device;
using System.Drawing;

namespace ImageCapture
{
    public static class ScreenHelper
    {
        private const uint MONITOR_DEFAULTTONEAREST = 2;

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        /// <summary>
        /// Schneidet ein Rechteck so zu, dass es nur innerhalb des Monitors liegt,
        /// auf dem sein Mittelpunkt ist.
        /// </summary>
        public static Rect ClampRectToCurrentMonitor(Rect rect)
        {
            var m = GetMonitorBoundsForWindow(rect);
            int x = Math.Max(rect.X, m.X);
            int y = Math.Max(rect.Y, m.Y);
            int r = Math.Min(rect.X + rect.Width, m.X + m.Width);
            int b = Math.Min(rect.Y + rect.Height, m.Y + m.Height);
            return new Rect(x, y, Math.Max(0, r - x), Math.Max(0, b - y));
        }

        /// <summary>
        /// Liest die Monitor-Bounds (globale Koordinaten) aus, auf dem der Fenstermittelpunkt liegt.
        /// </summary>
        public static Rect GetMonitorBoundsForWindow(Rect windowRect)
        {
            // 1) Mittelpunkt
            var center = new POINT
            {
                X = windowRect.X + windowRect.Width / 2,
                Y = windowRect.Y + windowRect.Height / 2
            };
            IntPtr hMon = MonitorFromPoint(center, MONITOR_DEFAULTTONEAREST);

            var mi = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
            if (!GetMonitorInfo(hMon, ref mi))
                throw new InvalidOperationException("GetMonitorInfo fehlgeschlagen");

            var m = mi.rcMonitor;
            int width = m.Right - m.Left;
            int height = m.Bottom - m.Top;
            return new Rect(m.Left, m.Top, width, height);
        }

        /// <summary>
        /// Ermittelt für das übergebene Fenster-Rect den passenden Output-Index (Monitor).
        /// Liefert -1, wenn kein Monitor trifft.
        /// </summary>
        public static int FindOutputIndexForWindow(Rect windowRect, Adapter1 adapter)
        {
            int count = adapter.GetOutputCount();
            for (int i = 0; i < count; i++)
            {
                using var output = adapter.GetOutput(i).QueryInterface<Output1>();
                var d = output.Description;

                if (DescriptionIsOverlapping(d, windowRect))
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Findet den Index des DXGI-Adapters (GPU), auf dem das Fenster liegt.
        /// Gibt 0 zurück, wenn kein passender Adapter gefunden wird.
        /// </summary>
        public static int FindAdapterIndexForWindow(Rect windowRect, Factory1 factory)
        {
            int adapterCount = factory.GetAdapterCount1();
            for (int a = 0; a < adapterCount; a++)
            {
                using var adapter = factory.GetAdapter1(a);
                int outputCount = adapter.GetOutputCount();
                for (int o = 0; o < outputCount; o++)
                {
                    using var output = adapter.GetOutput(o).QueryInterface<Output1>();
                    var d = output.Description;
                    if (DescriptionIsOverlapping(d, windowRect))
                        return a;
                }
            }
            // Fallback auf ersten Adapter
            return 0;
        }
        /// <summary>
        /// Ermittelt, ob eine Description mit eine Rect überlappt
        /// </summary>
        /// <param name="d"></param>
        /// <param name="rect"></param>
        /// <returns></returns>
        public static bool DescriptionIsOverlapping(OutputDescription d, Rect rect)
        {
            var bounds = new Rect(
                d.DesktopBounds.Left,
                d.DesktopBounds.Top,
                d.DesktopBounds.Right - d.DesktopBounds.Left,
                d.DesktopBounds.Bottom - d.DesktopBounds.Top);
            if (bounds.IntersectsWith(rect))
            {
                return true;
            }
            return false;
        }
    }
}
