using ImageDetection.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageDetection.YOLO
{
    public interface IYoloManager : IAsyncDisposable, IDisposable
    {
        event Action<string, ModelDownloadStatus, int, string?>? DownloadProgressChanged;

        /// <summary> Lädt/initialisiert das Modell (einmalig, threadsicher). </summary>
        Task EnsureModelAsync(string modelKey, CancellationToken ct = default);

        /// <summary>
        /// Sucht das gegebene Objekt im Bitmap. Optional ROI (in Bildpixeln), Confidence-Threshold (0..1).
        /// </summary>
        Task<IDetectionResult?> DetectAsync(
            string modelKey,
            string objectName,
            Bitmap bitmap,
            float threshold,
            Rectangle? roi = null,
            CancellationToken ct = default);
    }
}
