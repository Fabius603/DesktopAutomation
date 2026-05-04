using System;
using System.Drawing;
using Microsoft.Extensions.Logging;

namespace TaskAutomation.Events
{
    /// <summary>
    /// Fallback-Implementierung ohne UI-Abhängigkeit – feuert nur das Event.
    /// In der WPF-App wird stattdessen <c>WpfImageDisplayService</c> registriert.
    /// </summary>
    public class ImageDisplayService : IImageDisplayService
    {
        private readonly ILogger<ImageDisplayService> _logger;

        public event EventHandler<ImageDisplayRequestedEventArgs>? ImageDisplayRequested;

        public ImageDisplayService(ILogger<ImageDisplayService> logger)
        {
            _logger = logger;
        }

        public void DisplayImage(string windowName, Bitmap image, ImageDisplayType displayType)
        {
            try
            {
                ImageDisplayRequested?.Invoke(this, new ImageDisplayRequestedEventArgs(windowName, image, displayType));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to raise ImageDisplayRequested for '{WindowName}': {Error}", windowName, ex.Message);
            }
        }

        public void CloseWindow(string windowName) { /* no-op in fallback implementation */ }
        public void CloseAllWindows() { /* no-op in fallback implementation */ }
    }
}
