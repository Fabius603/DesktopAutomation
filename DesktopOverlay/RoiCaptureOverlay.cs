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
    /// ROI Capture Overlay zum Erfassen eines Rechtecks durch Ziehen mit der Maus.
    /// </summary>
    public class RoiCaptureOverlay : IDisposable
    {
        private DoubleBufferedForm? _overlayForm;
        private List<DoubleBufferedForm>? _overlayForms;
        private TaskCompletionSource<Rectangle>? _roiTcs;
        private Thread? _uiThread;
        private volatile bool _disposed;
        
        private bool _isDrawing;
        private Point _startPoint;
        private Point _currentPoint;
        private Rectangle _currentRect;
        private Screen? _selectedScreen;

        // Custom Form class that enables double buffering
        private class DoubleBufferedForm : Form
        {
            public DoubleBufferedForm()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | 
                         ControlStyles.UserPaint | 
                         ControlStyles.DoubleBuffer | 
                         ControlStyles.ResizeRedraw, true);
            }
        }

        public async Task<Rectangle> CaptureRoiAsync(CancellationToken cancellationToken = default)
        {
            if (_roiTcs != null)
                throw new InvalidOperationException("ROI-Erfassung läuft bereits.");

            _roiTcs = new TaskCompletionSource<Rectangle>();

            // UI-Thread für Overlay erstellen
            _uiThread = new Thread(() =>
            {
                try
                {
                    CreateOverlays();
                    Application.Run();
                }
                catch (Exception ex)
                {
                    _roiTcs?.TrySetException(ex);
                }
            })
            {
                IsBackground = false
            };

            _uiThread.SetApartmentState(ApartmentState.STA);
            _uiThread.Start();

            // Auf Ergebnis warten
            using (cancellationToken.Register(() => _roiTcs?.TrySetCanceled()))
            {
                return await _roiTcs.Task;
            }
        }

        private void CreateOverlays()
        {
            var screens = ScreenHelper.GetScreens();
            var overlayForms = new List<DoubleBufferedForm>();

            foreach (var screen in screens)
            {
                var overlay = new DoubleBufferedForm
                {
                    FormBorderStyle = FormBorderStyle.None,
                    WindowState = FormWindowState.Normal,
                    TopMost = true,
                    ShowInTaskbar = false,
                    StartPosition = FormStartPosition.Manual,
                    BackColor = Color.Black,
                    Opacity = 0.3,
                    Bounds = screen.Bounds,
                    Cursor = Cursors.Cross,
                    Tag = screen // Store screen info in Tag
                };

                // Event-Handler registrieren
                overlay.MouseDown += OnMouseDown;
                overlay.MouseMove += OnMouseMove;
                overlay.MouseUp += OnMouseUp;
                overlay.KeyDown += OnKeyDown;
                overlay.Paint += OnPaint;
                overlay.FormClosed += OnFormClosed;

                overlay.Show();
                overlay.Focus();
                overlayForms.Add(overlay);
            }

            // Store reference to all overlays for cleanup
            _overlayForms = overlayForms;
        }

        private void OnMouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && sender is DoubleBufferedForm form)
            {
                _isDrawing = true;
                _startPoint = e.Location;
                _currentPoint = e.Location;
                _currentRect = new Rectangle();
                _selectedScreen = form.Tag as Screen;
                
                // Nur das aktuelle Form wird für das Zeichnen verwendet
                _overlayForm = form;
            }
        }

        private void OnMouseMove(object? sender, MouseEventArgs e)
        {
            if (_isDrawing && sender == _overlayForm)
            {
                _currentPoint = e.Location;
                
                // Rechteck berechnen (relativ zum aktuellen Monitor)
                int x = Math.Min(_startPoint.X, _currentPoint.X);
                int y = Math.Min(_startPoint.Y, _currentPoint.Y);
                int width = Math.Abs(_currentPoint.X - _startPoint.X);
                int height = Math.Abs(_currentPoint.Y - _startPoint.Y);
                
                _currentRect = new Rectangle(x, y, width, height);
                
                // Repaint auslösen
                _overlayForm?.Invalidate();
            }
        }

        private void OnMouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && _isDrawing && sender == _overlayForm)
            {
                _isDrawing = false;
                
                // ROI erfassen und als lokale Monitor-Koordinaten zurückgeben
                if (_currentRect.Width > 10 && _currentRect.Height > 10)
                {
                    // Koordinaten sind bereits relativ zum Monitor, da jedes Overlay 
                    // seine eigenen lokalen Koordinaten hat
                    _roiTcs?.TrySetResult(_currentRect);
                    CloseAllOverlays();
                }
            }
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                _roiTcs?.TrySetCanceled();
                CloseAllOverlays();
            }
        }

        private void OnPaint(object? sender, PaintEventArgs e)
        {
            if (_isDrawing && sender == _overlayForm && _currentRect.Width > 0 && _currentRect.Height > 0)
            {
                // Helle, leuchtende Farben für bessere Sichtbarkeit
                using (var pen = new Pen(Color.Cyan, 3)) // Helles Cyan, dickerer Rand
                {
                    e.Graphics.DrawRectangle(pen, _currentRect);
                }
                
                using (var brush = new SolidBrush(Color.FromArgb(80, Color.LightBlue))) // Helles, transparentes Blau
                {
                    e.Graphics.FillRectangle(brush, _currentRect);
                }
                
                // Monitor-Info und ROI-Größe anzeigen
                string text = $"ROI: {_currentRect.Width} x {_currentRect.Height}";
                    
                using (var font = new Font("Arial", 14, FontStyle.Bold)) // Größere Schrift
                using (var textBrush = new SolidBrush(Color.Yellow)) // Helles Gelb
                using (var shadowBrush = new SolidBrush(Color.Black))
                {
                    var textLocation = new PointF(_currentRect.X + 5, _currentRect.Y + 5);
                    
                    // Schatten für bessere Lesbarkeit
                    e.Graphics.DrawString(text, font, shadowBrush, textLocation.X + 2, textLocation.Y + 2);
                    e.Graphics.DrawString(text, font, textBrush, textLocation);
                }
            }
        }

        private void OnFormClosed(object? sender, FormClosedEventArgs e)
        {
            if (_roiTcs != null && !_roiTcs.Task.IsCompleted)
            {
                _roiTcs.TrySetCanceled();
            }
        }

        private void CloseAllOverlays()
        {
            if (_overlayForms != null)
            {
                foreach (var overlay in _overlayForms)
                {
                    if (!overlay.IsDisposed && overlay.Visible)
                    {
                        overlay.Invoke(new Action(() => overlay.Close()));
                    }
                }
            }
            else if (_overlayForm != null && !_overlayForm.IsDisposed && _overlayForm.Visible)
            {
                _overlayForm.Invoke(new Action(() => _overlayForm.Close()));
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            CloseAllOverlays();

            if (_overlayForms != null)
            {
                foreach (var overlay in _overlayForms)
                {
                    overlay?.Dispose();
                }
                _overlayForms.Clear();
                _overlayForms = null;
            }

            _overlayForm?.Dispose();
            _overlayForm = null;

            if (_uiThread != null && _uiThread.IsAlive)
            {
                _uiThread.Join(1000);
            }
            
            _uiThread = null;
            _roiTcs = null;
        }
    }
}
