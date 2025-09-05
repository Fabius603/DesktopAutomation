using System;
using System.Drawing;

namespace TaskAutomation.Events
{
    public class ImageDisplayRequestedEventArgs : EventArgs
    {
        public string WindowName { get; }
        public Bitmap Image { get; }
        public ImageDisplayType DisplayType { get; }

        public ImageDisplayRequestedEventArgs(string windowName, Bitmap image, ImageDisplayType displayType)
        {
            WindowName = windowName;
            Image = image;
            DisplayType = displayType;
        }
    }

    public enum ImageDisplayType
    {
        Raw,
        Processed
    }

    public interface IImageDisplayService
    {
        event EventHandler<ImageDisplayRequestedEventArgs>? ImageDisplayRequested;
        void DisplayImage(string windowName, Bitmap image, ImageDisplayType displayType);
    }
}
