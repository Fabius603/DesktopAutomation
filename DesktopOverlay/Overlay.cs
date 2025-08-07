using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using GameOverlay.Windows;
using GameOverlay.Drawing;
using Vortice.Mathematics;
using Graphics = GameOverlay.Drawing.Graphics;
using SolidBrush = GameOverlay.Drawing.SolidBrush;
using System.Runtime.InteropServices;

namespace DesktopOverlay
{
    /// <summary>
    /// Manager für das Overlay-Fenster und die Item-Events.
    /// </summary>
    public class Overlay : IDisposable
    {
        private readonly GraphicsWindow _window;
        private readonly ConcurrentDictionary<string, IOverlayItem> _items = new();
        private SolidBrush _backgroundBrush;
        private int _desktopId;
        public IntPtr WindowHandle => _window.Handle;
        private GameOverlay.Drawing.Graphics _gfxContext;

        public Overlay(int x, int y, int width, int height, int desktopId = 1, Graphics gfxSettings = null)
        {
            _desktopId = desktopId;

            var gfx = gfxSettings ?? new Graphics
            {
                MeasureFPS = true,
                PerPrimitiveAntiAliasing = true,
                TextAntiAliasing = true
            };

            _window = new GraphicsWindow(x, y, width, height, gfx)
            {
                FPS = 60,
                IsTopmost = true,
                IsVisible = true
            };

            _window.SetupGraphics += OnSetup;
            _window.DrawGraphics += OnDraw;
            _window.DestroyGraphics += OnDestroy;
        }

        public int DesktopID
        {
            get => _desktopId;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "Desktop ID must be non-negative.");
                _desktopId = value;
                MoveToMonitor(_desktopId);
            }
        }

        public void AddItem(IOverlayItem item)
        {
            _items[item.Id] = item;

            // 2) Falls das Device schon steht, sofort Ressourcen anlegen
            if (_gfxContext != null)
            {
                item.Setup(_gfxContext, recreate: false);
            }
        }

        public bool RemoveItem(string id)
        {
            if (_items.TryRemove(id, out var removed))
            {
                removed.Dispose();
                return true;
            }
            return false;
        }

        private void OnSetup(object sender, SetupGraphicsEventArgs e)
        {
            _gfxContext = e.Graphics;
            // Hintergrundbrush einmalig anlegen
            if (e.RecreateResources || _backgroundBrush == null)
            {
                _backgroundBrush?.Dispose();
                _backgroundBrush = _gfxContext.CreateSolidBrush(0, 0, 0, 0);
            }

            // Jedes Item richtet sich selbst ein
            foreach (var item in _items.Values)
                item.Setup(_gfxContext, e.RecreateResources);
        }

        private void OnDraw(object sender, DrawGraphicsEventArgs e)
        {
            var gfx = e.Graphics;
            // transparente Szene
            gfx.ClearScene(_backgroundBrush);

            // Alle Items zeichnen
            foreach (var item in _items.Values)
                if (item.Visible)
                    item.Draw(gfx);
        }

        private void OnDestroy(object sender, DestroyGraphicsEventArgs e)
        {
            // Alle Ressourcen freigeben
            _backgroundBrush?.Dispose();
            foreach (var item in _items.Values)
                item.Dispose();
        }

        public void CreateWindow()
        {
            _window.Create();
        }

        public void JoinWindow()
        {
            _window.Join();
        }

        #region WinAPI SetWindowPos
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOZORDER = 0x0004;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int X,
            int Y,
            int cx,
            int cy,
            uint uFlags);
        #endregion

        /// <summary>
        /// Verschiebt das Overlay auf den 0-basierten Monitor-Index.
        /// </summary>
        /// <param name="monitorIndex">Index von 0 bis Screen.AllScreens.Length−1</param>
        public void MoveToMonitor(int monitorIndex)
        {
            var screens = Screen.AllScreens;
            if (monitorIndex < 0 || monitorIndex >= screens.Length)
                throw new ArgumentOutOfRangeException(nameof(monitorIndex),
                    $"Monitore nur von 0 bis {screens.Length - 1}.");

            var bounds = screens[monitorIndex].Bounds;
            SetWindowPos(
                _window.Handle,
                IntPtr.Zero,
                bounds.X,
                bounds.Y,
                0, 0,
                SWP_NOSIZE | SWP_NOZORDER
            );
        }

        public void RunInNewThread()
        {
            var thread = new Thread(() => Run());
            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();
        }

        public void Run()
        {
            CreateWindow();
            MoveToMonitor(_desktopId);
            JoinWindow();
        }

        public void Dispose()
        {
            _window.Dispose();
        }
    }
}
