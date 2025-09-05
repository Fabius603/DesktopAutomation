using System;
using System.Drawing;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace TaskAutomation.Events
{
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
                // Raise event first (for WPF or other UI handlers)
                ImageDisplayRequested?.Invoke(this, new ImageDisplayRequestedEventArgs(windowName, image, displayType));

                // Fallback to OpenCV with improved stability
                DisplayImageWithOpenCV(windowName, image);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to display image '{WindowName}': {Error}", windowName, ex.Message);
            }
        }

        private void DisplayImageWithOpenCV(string windowName, Bitmap image)
        {
            Mat? mat = null;
            Mat? resizedMat = null;

            try
            {
                mat = image.ToMat();
                resizedMat = new Mat();
                
                // Resize to reasonable size
                Cv2.Resize(mat, resizedMat, new OpenCvSharp.Size(), 0.5, 0.5);
                
                // Create window with specific properties to prevent size issues
                Cv2.NamedWindow(windowName, WindowFlags.Normal | WindowFlags.KeepRatio);
                
                // Set window properties to prevent jumping
                var windowSize = resizedMat.Size();
                Cv2.ResizeWindow(windowName, windowSize.Width, windowSize.Height);
                
                Cv2.ImShow(windowName, resizedMat);
                Cv2.WaitKey(1);
                
                _logger.LogDebug("Image displayed in OpenCV window '{WindowName}' with size {Width}x{Height}", 
                    windowName, windowSize.Width, windowSize.Height);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to display image in OpenCV window '{WindowName}': {Error}", windowName, ex.Message);
            }
            finally
            {
                mat?.Dispose();
                resizedMat?.Dispose();
            }
        }
    }
}
