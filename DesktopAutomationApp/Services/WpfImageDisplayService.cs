using DesktopAutomationApp.Views;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using TaskAutomation.Events;

namespace DesktopAutomationApp.Services
{
    /// <summary>
    /// WPF-Implementierung von <see cref="IImageDisplayService"/>.
    /// Zeigt Bilder in nativen WPF-Fenstern an, die per WriteableBitmap
    /// effizient aktualisiert werden – kein OpenCV-Overhead.
    ///
    /// Thread-Sicherheit: DisplayImage kann beliebig oft vom Job-Thread aufgerufen
    /// werden. Es wird immer nur genau EIN Dispatcher-Task pro Fenster ausstehend
    /// gehalten (last-frame-wins), um Dispatcher-Flooding zu verhindern.
    /// Das Bitmap wird sofort geklont, damit der Aufrufer es danach gefahrlos
    /// weiterverwendet oder disposed.
    /// </summary>
    public sealed class WpfImageDisplayService : IImageDisplayService
    {
        private readonly ILogger<WpfImageDisplayService> _logger;

        private readonly ConcurrentDictionary<string, WindowState> _states = new();

        public event EventHandler<ImageDisplayRequestedEventArgs>? ImageDisplayRequested;

        public WpfImageDisplayService(ILogger<WpfImageDisplayService> logger)
        {
            _logger = logger;
        }

        public void DisplayImage(string windowName, Bitmap image, ImageDisplayType displayType)
        {
            try { ImageDisplayRequested?.Invoke(this, new ImageDisplayRequestedEventArgs(windowName, image, displayType)); }
            catch (Exception ex) { _logger.LogError(ex, "ImageDisplayRequested-Event fehlgeschlagen für '{Name}'", windowName); }

            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null) return;

            // Bitmap sofort klonen – der Aufrufer darf es danach gefahrlos freigeben.
            Bitmap clone;
            try { clone = (Bitmap)image.Clone(); }
            catch (Exception ex) { _logger.LogError(ex, "Bitmap-Klon fehlgeschlagen für '{Name}'", windowName); return; }

            var state = _states.GetOrAdd(windowName, _ => new WindowState());

            // Nach CloseAllWindows keine neuen Frames mehr einreihen.
            if (state.IsClosed) return;

            // Altes pending Bitmap durch das neue ersetzen; altes freigeben.
            var old = Interlocked.Exchange(ref state.PendingBitmap, clone);
            old?.Dispose();

            // Nur einen Dispatcher-Task gleichzeitig pro Fenster queuen (last-frame-wins).
            if (Interlocked.CompareExchange(ref state.DispatchQueued, 1, 0) == 0)
            {
                dispatcher.InvokeAsync(() => ProcessPending(windowName, state), DispatcherPriority.Background);
            }
        }

        private void ProcessPending(string windowName, WindowState state)
        {
            // Flag zurücksetzen, damit neue Frames wieder queuen dürfen.
            Interlocked.Exchange(ref state.DispatchQueued, 0);

            // Fenster wurde zwischenzeitlich geschlossen – nichts tun.
            if (state.IsClosed)
            {
                Interlocked.Exchange(ref state.PendingBitmap, null)?.Dispose();
                return;
            }

            var bmp = Interlocked.Exchange(ref state.PendingBitmap, null);
            if (bmp == null) return;

            try
            {
                var win = state.Window;
                if (win == null || !win.IsLoaded)
                {
                    win = new ImagePreviewWindow(windowName);
                    win.Closed += (_, _) =>
                    {
                        state.Window = null;
                        _states.TryRemove(windowName, out _);
                    };
                    state.Window = win;
                    win.Show();
                }
                else if (!win.IsVisible)
                {
                    win.Show();
                }

                win.UpdateBitmap(bmp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Anzeigen von '{WindowName}'", windowName);
            }
            finally
            {
                bmp.Dispose();
            }
        }

        public void CloseWindow(string windowName)
        {
            if (!_states.TryGetValue(windowName, out var state)) return;
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null) return;

            void Close()
            {
                state.IsClosed = true;
                Interlocked.Exchange(ref state.PendingBitmap, null)?.Dispose();
                try { state.Window?.Close(); } catch { /* best-effort */ }
                _states.TryRemove(windowName, out _);
            }

            if (dispatcher.CheckAccess())
                Close();
            else
                dispatcher.InvokeAsync(Close, DispatcherPriority.Background);
        }

        public void CloseAllWindows()
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null) return;

            void Close()
            {
                foreach (var kvp in _states.ToArray())
                {
                    kvp.Value.IsClosed = true;
                    Interlocked.Exchange(ref kvp.Value.PendingBitmap, null)?.Dispose();
                    try { kvp.Value.Window?.Close(); } catch { /* best-effort */ }
                }
                _states.Clear();
            }

            if (dispatcher.CheckAccess())
                Close();
            else
                dispatcher.InvokeAsync(Close, DispatcherPriority.Background);
        }

        private sealed class WindowState
        {
            public ImagePreviewWindow? Window;
            public Bitmap? PendingBitmap;
            public int DispatchQueued;  // 0 = frei, 1 = ein Task wartet bereits
            public volatile bool IsClosed;
        }
    }
}
