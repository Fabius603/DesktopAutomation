using System;
using System.Diagnostics;
using System.Threading;

namespace ImageCapture.DesktopDuplication
{
    /// <summary>Tracks the image presentation, independently of pointer updates and bitmap copies.</summary>
    public sealed class DesktopFrameMetadata
    {
        private static long _nextVersion;
        private readonly long _clockOrigin;
        private readonly DateTime _utcOrigin;

        public DesktopFrameMetadata() : this(Stopwatch.GetTimestamp(), DateTime.UtcNow) { }

        public DesktopFrameMetadata(long clockOrigin, DateTime utcOrigin)
        {
            _clockOrigin = clockOrigin;
            _utcOrigin = utcOrigin;
        }

        public long FrameVersion { get; private set; }
        public long FrameTimestamp { get; private set; }
        public DateTime CaptureTimestampUtc { get; private set; }

        /// <summary>Zero identifies a pointer-only update or timeout, not a new desktop image.</summary>
        public bool Update(long presentationTimestamp)
        {
            if (presentationTimestamp <= 0 || presentationTimestamp <= FrameTimestamp)
                return false;

            FrameTimestamp = presentationTimestamp;
            FrameVersion = Interlocked.Increment(ref _nextVersion);
            CaptureTimestampUtc = _utcOrigin.AddSeconds(
                (presentationTimestamp - _clockOrigin) / (double)Stopwatch.Frequency);
            return true;
        }
    }
}
