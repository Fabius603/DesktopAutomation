using System;
using System.Collections.Generic;
using System.Drawing;

namespace TaskAutomation.Steps
{
    public sealed class PredictMovementState
    {
        public Dictionary<int, PredictMovementTrack> Tracks { get; } = new();
        public int NextTrackId { get; set; }
    }

    public sealed class PredictMovementTrack
    {
        public Queue<PredictMovementSample> Samples { get; } = new();
        public DateTime LastUpdateUtc { get; set; }
    }

    public readonly record struct PredictMovementSample(
        Point Center,
        Rectangle? BoundingBox,
        DateTime TimestampUtc);

    public readonly record struct ActiveWindowCacheEntry(
        string ProcessName,
        bool IsActive,
        RuntimeProcessReference? Process,
        long WindowHandle,
        DateTime Timestamp);
}
