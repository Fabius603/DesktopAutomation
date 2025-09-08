using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ImageHelperMethods;

namespace DesktopOverlay
{
    /// <summary>
    /// Spezielles Overlay zum Erfassen eines einzelnen Mausklicks.
    /// Blockiert alle Eingaben außer dem zu erfassenden Klick.
    /// </summary>
    public class ClickCaptureOverlay : IDisposable
    {
        private Form? _overlayForm;
        private TaskCompletionSource<Point>? _clickTcs;
        private Thread? _uiThread;
        private volatile bool _disposed;

        public async Task<Point> CaptureClickAsync(CancellationToken cancellationToken = default)
        {
            if (_clickTcs != null)
                throw new InvalidOperationException("Klick-Erfassung läuft bereits.");

            _clickTcs = new TaskCompletionSource<Point>();

            // Registriere Cancellation
            if (cancellationToken.CanBeCanceled)
            {
                cancellationToken.Register(() =>
                {
                    _clickTcs?.TrySetCanceled(cancellationToken);
                    CloseOverlay();
                });
            }

            // Starte UI-Thread und zeige Overlay
            _uiThread = new Thread(ShowOverlay)
            {
                IsBackground = true
            };
            _uiThread.SetApartmentState(ApartmentState.STA);
            _uiThread.Start();

            return await _clickTcs.Task;
        }

        private void ShowOverlay()
        {
            try
            {
                var virtualBounds = ScreenHelper.GetVirtualDesktopBounds();
                
                // Debug: Zeige virtuelle Desktop-Bounds
                System.Diagnostics.Debug.WriteLine($"Virtual Desktop Bounds: X={virtualBounds.Left}, Y={virtualBounds.Top}, Width={virtualBounds.Width}, Height={virtualBounds.Height}");

                _overlayForm = new Form
                {
                    FormBorderStyle = FormBorderStyle.None,
                    TopMost = true,
                    ShowInTaskbar = false,
                    BackColor = Color.Black,
                    Opacity = 0.2, // Fast transparent, aber nicht vollständig
                    StartPosition = FormStartPosition.Manual,
                    Location = new Point(virtualBounds.Left, virtualBounds.Top),
                    Size = new Size(virtualBounds.Width, virtualBounds.Height),
                    Cursor = Cursors.Cross,
                    WindowState = FormWindowState.Normal // Normal statt Maximized
                };

                // Event-Handler für Mausklick
                _overlayForm.MouseClick += OnMouseClick;
                _overlayForm.KeyDown += OnKeyDown;

                _overlayForm.Show();

                // Verhindere Capture durch andere Anwendungen
                SetWindowDisplayAffinity(_overlayForm.Handle, WDA_EXCLUDEFROMCAPTURE);

                // Explizit über alle Monitore positionieren
                SetWindowPos(_overlayForm.Handle, 
                           new IntPtr(HWND_TOPMOST), 
                           virtualBounds.Left, virtualBounds.Top, 
                           virtualBounds.Width, virtualBounds.Height, 
                           SWP_SHOWWINDOW);

                _overlayForm.Activate();
                _overlayForm.Focus();

                // Message Loop starten
                Application.Run(_overlayForm);
            }
            catch (Exception ex)
            {
                _clickTcs?.TrySetException(ex);
            }
        }

        private void OnMouseClick(object? sender, MouseEventArgs e)
        {
            if (_overlayForm == null) return;

            // Umrechnung auf virtuelle Desktop-Koordinaten
            var virtualBounds = ScreenHelper.GetVirtualDesktopBounds();
            var absoluteX = _overlayForm.Location.X + e.X;
            var absoluteY = _overlayForm.Location.Y + e.Y;

            var clickPoint = new Point(absoluteX, absoluteY);
            
            _clickTcs?.TrySetResult(clickPoint);
            CloseOverlay();
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            // ESC zum Abbrechen
            if (e.KeyCode == Keys.Escape)
            {
                _clickTcs?.TrySetCanceled();
                CloseOverlay();
            }
        }

        private void CloseOverlay()
        {
            if (_overlayForm?.InvokeRequired == true)
            {
                _overlayForm.Invoke(new Action(CloseOverlay));
                return;
            }

            if (_overlayForm != null)
            {
                _overlayForm.Close();
                _overlayForm.Dispose();
                _overlayForm = null;
            }
        }

        const uint WDA_EXCLUDEFROMCAPTURE = 0x11;

        [DllImport("user32.dll")]
        static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private const int HWND_TOPMOST = -1;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_SHOWWINDOW = 0x0040;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _clickTcs?.TrySetCanceled();
            CloseOverlay();
            
            _uiThread?.Join(1000); // Warte max. 1 Sekunde
        }
    }
}
