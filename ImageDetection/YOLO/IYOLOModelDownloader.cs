using ImageDetection.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageDetection.YOLO
{
    public interface IYOLOModelDownloader
    {
        event EventHandler<ModelDownloadProgressEventArgs>? DownloadProgressChanged;
        Task<YOLOModel> DownloadModelAsync(string modelKey, CancellationToken ct = default);
    }
}
