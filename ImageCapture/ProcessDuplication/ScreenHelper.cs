using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Drawing;
using OpenCvSharp;
using SharpDX.DXGI;
using SharpDX.Direct3D11;
using Device = SharpDX.Direct3D11.Device;

namespace ImageCapture.ProcessDuplication
{
    public static class ScreenHelper
    {
        private const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

        [DllImport("user32.dll")]
        private static extern nint MonitorFromPoint(POINT pt, uint dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

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

        // MONITORINFOEX structure - used to get detailed monitor information
        // It's crucial to set the cbSize field to the size of the structure.
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MONITORINFOEX
        {
            public int cbSize;
            public RECT rcMonitor;       // Monitor coordinates
            public RECT rcWork;          // Work area coordinates (excluding taskbar)
            public uint dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szDevice;      // Monitor device name

            public void Init()
            {
                this.cbSize = Marshal.SizeOf(typeof(MONITORINFOEX));
            }
        }

        /// <summary>
        /// Ermittelt das Rechteck (Position und Größe) eines Fensters relativ zum oberen linken Rand des Bildschirms,
        /// auf dem es sich befindet.
        /// </summary>
        /// <param name="hWnd">Das Handle (IntPtr) des Fensters.</param>
        /// <returns>Ein System.Drawing.Rectangle-Objekt, das die Position und Größe des Fensters relativ zu seinem aktuellen Monitor darstellt.
        /// Gibt Rectangle.Empty zurück, wenn das Fenster nicht gefunden wird oder ein Fehler auftritt.</returns>
        public static Rectangle GetWindowRectangleOnCurrentMonitor(IntPtr hWnd)
        {
            Rectangle globalWindowRect = GetWindowGlobalRectangle(hWnd);

            if (globalWindowRect.IsEmpty)
            {
                return Rectangle.Empty;
            }

            Rectangle monitorBounds = GetMonitorBoundsForWindow(hWnd);

            if (monitorBounds.IsEmpty)
            {
                // Fallback: If monitor bounds can't be determined, return global rect as is
                // Or handle as an error if monitor bounds are strictly required
                return Rectangle.Empty;
            }

            // Calculate the window's rectangle relative to the found monitor's bounds
            int relativeX = globalWindowRect.X - monitorBounds.Left;
            int relativeY = globalWindowRect.Y - monitorBounds.Top;

            return new Rectangle(relativeX, relativeY, globalWindowRect.Width, globalWindowRect.Height);
        }

        /// <summary>
        /// Ermittelt das Rechteck (Position und Größe) eines Fensters in globalen Bildschirmkoordinaten.
        /// </summary>
        /// <param name="hWnd">Das Handle (IntPtr) des Fensters.</param>
        /// <returns>Ein System.Drawing.Rectangle-Objekt, das die Position und Größe des Fensters darstellt.
        /// Gibt Rectangle.Empty zurück, wenn das Fenster nicht gefunden wird oder ein Fehler auftritt.</returns>
        public static Rectangle GetWindowGlobalRectangle(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
            {
                return Rectangle.Empty;
            }

            RECT rect;
            if (GetWindowRect(hWnd, out rect))
            {
                return new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
            }
            else
            {
                return Rectangle.Empty;
            }
        }

        /// <summary>
        /// Beschneidet ein globales Fensterrechteck, sodass es nur den Teil darstellt, der auf einem bestimmten Monitor sichtbar ist.
        /// Die Koordinaten des zurückgegebenen Rechtecks sind relativ zum oberen linken Rand des Monitors.
        /// </summary>
        /// <param name="windowGlobalRect">Das globale Rechteck des Fensters.</param>
        /// <param name="monitorGlobalBounds">Die globalen Begrenzungen des Monitors.</param>
        /// <returns>Ein Rechteck, das den sichtbaren Teil des Fensters auf dem Monitor darstellt,
        /// mit Koordinaten relativ zum Monitor. Gibt Rectangle.Empty zurück, wenn das Fenster
        /// nicht mit dem Monitor überlappt.</returns>
        public static Rectangle ClampRectToMonitor(Rectangle windowGlobalRect, Rectangle monitorGlobalBounds)
        {
            // Calculate the intersection of the window and the monitor in global coordinates
            Rectangle intersection = Rectangle.Intersect(windowGlobalRect, monitorGlobalBounds);

            if (intersection.IsEmpty)
            {
                // No overlap between window and monitor
                return Rectangle.Empty;
            }

            // Now, convert the intersection rectangle to be relative to the monitor's top-left corner
            int clampedX = intersection.X - monitorGlobalBounds.Left;
            int clampedY = intersection.Y - monitorGlobalBounds.Top;
            int clampedWidth = intersection.Width;
            int clampedHeight = intersection.Height;

            return new Rectangle(clampedX, clampedY, clampedWidth, clampedHeight);
        }

        /// <summary>
        /// Ermittelt das Rechteck des Monitors, auf dem sich das angegebene Fenster hauptsächlich befindet.
        /// </summary>
        /// <param name="hWnd">Das Handle (IntPtr) des Fensters.</param>
        /// <returns>Ein System.Drawing.Rectangle-Objekt, das die globalen Koordinaten des Monitors darstellt.
        /// Gibt Rectangle.Empty zurück, wenn kein Monitor gefunden wird oder ein Fehler auftritt.</returns>
        public static Rectangle GetMonitorBoundsForWindow(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
            {
                return Rectangle.Empty;
            }

            // Get a handle to the monitor that contains the window
            IntPtr hMonitor = MonitorFromWindow(hWnd, MONITOR_DEFAULTTONEAREST);

            if (hMonitor == IntPtr.Zero)
            {
                return Rectangle.Empty; // No monitor found for this window
            }

            // Get detailed information about the monitor
            MONITORINFOEX monitorInfo = new MONITORINFOEX();
            monitorInfo.Init(); // Important: Initialize cbSize

            if (GetMonitorInfo(hMonitor, ref monitorInfo))
            {
                // The rcMonitor field contains the global bounds of the monitor
                return new Rectangle(
                    monitorInfo.rcMonitor.Left,
                    monitorInfo.rcMonitor.Top,
                    monitorInfo.rcMonitor.Right - monitorInfo.rcMonitor.Left,
                    monitorInfo.rcMonitor.Bottom - monitorInfo.rcMonitor.Top
                );
            }
            else
            {
                return Rectangle.Empty; // Failed to get monitor info
            }
        }


        public static int FindOutputIndexForWindow(Rectangle windowRect, int adapterIdx, DxgiResources dxgi)
        {
            foreach (var kvp in dxgi.Outputs)
            {
                if (kvp.Key.adapterIdx != adapterIdx)
                    continue;

                var outputDesc = kvp.Value.Description;
                var bounds = new Rectangle(
                    outputDesc.DesktopBounds.Left,
                    outputDesc.DesktopBounds.Top,
                    outputDesc.DesktopBounds.Right - outputDesc.DesktopBounds.Left,
                    outputDesc.DesktopBounds.Bottom - outputDesc.DesktopBounds.Top);

                if (bounds.IntersectsWith(windowRect))
                    return kvp.Key.outputIdx;
            }

            return -1;
        }

        public static int FindAdapterIndexForWindow(Rectangle windowRect, DxgiResources dxgi)
        {
            foreach (var adapter in dxgi.Adapters)
            {
                int outputIdx = FindOutputIndexForWindow(windowRect, adapter.Key, dxgi);
                if (outputIdx >= 0)
                    return adapter.Key;
            }
            return -1; // Rückgabe -1 statt 0 zur klaren Fehlererkennung
        }

        public static (int adapterIdx, int outputIdx) GetAdapterAndOutputForWindowHandle(
            IntPtr hWnd,
            DxgiResources dxgi)
        {
            if (!GetWindowRect(hWnd, out RECT rect))
                throw new InvalidOperationException("GetWindowRect fehlgeschlagen");

            var windowRect = new Rectangle(
                rect.Left, rect.Top,
                rect.Right - rect.Left,
                rect.Bottom - rect.Top);

            int adapterIdx = FindAdapterIndexForWindow(windowRect, dxgi);
            if (!dxgi.Adapters.ContainsKey(adapterIdx))
                throw new InvalidOperationException($"Adapter {adapterIdx} wurde nicht gefunden.");

            int outputIdx = FindOutputIndexForWindow(windowRect, adapterIdx, dxgi);
            if (!dxgi.Outputs.ContainsKey((adapterIdx, outputIdx)))
                throw new InvalidOperationException($"Output {outputIdx} für Adapter {adapterIdx} wurde nicht gefunden.");

            return (adapterIdx, outputIdx);
        }
    }
}
