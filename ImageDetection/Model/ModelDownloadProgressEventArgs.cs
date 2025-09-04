using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageDetection.Model
{
    public sealed class ModelDownloadProgressEventArgs : EventArgs
    {
        public string ModelName { get; }
        public ModelDownloadStatus Status { get; }
        public int ProgressPercent { get; }
        public string? Message { get; }

        public ModelDownloadProgressEventArgs(
            string modelName,
            ModelDownloadStatus status,
            int progressPercent,
            string? message = null)
        {
            ModelName = modelName;
            Status = status;
            ProgressPercent = progressPercent;
            Message = message;
        }

        public override string ToString()
            => $"{ModelName}: {Status} {ProgressPercent}% {(string.IsNullOrWhiteSpace(Message) ? "" : "- " + Message)}";
    }
}
