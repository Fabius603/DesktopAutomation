using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageCapture.Video
{
    public class FPSManager
    {
        private readonly int targetFps;
        private readonly double targetFrameDurationMs;
        private readonly Stopwatch frameStopwatch;
        private int frameCount;

        public FPSManager(int fps)
        {
            if (fps <= 0) throw new ArgumentOutOfRangeException(nameof(fps), "FPS must be greater than zero.");
            targetFps = fps;
            targetFrameDurationMs = 1000.0 / targetFps;
            frameStopwatch = new Stopwatch();
            frameCount = 0;
        }

        public void Start()
        {
            frameStopwatch.Restart();
            frameCount = 0;
        }

        public void Reset()
        {
            frameStopwatch.Reset();
            frameCount = 0;
        }

        public void WaitForNextFrame()
        {
            double elapsedMs = frameStopwatch.Elapsed.TotalMilliseconds;

            double idealFrameEndTimeMs = (frameCount + 1) * targetFrameDurationMs;

            double timeToWait = idealFrameEndTimeMs - elapsedMs;

            if (timeToWait > 1)
            {
                Thread.Sleep((int)(timeToWait - 1)); 
            }

            while (frameStopwatch.Elapsed.TotalMilliseconds < idealFrameEndTimeMs)
            {
                Thread.SpinWait(10);
            }

            frameCount++;
        }

        public void WaitAndPaceFrame(double frameProcessingDurationMs)
        {
            double timeToWait = targetFrameDurationMs - frameProcessingDurationMs;

            if (timeToWait > 1) 
            {
                Thread.Sleep((int)(timeToWait - 1)); 
            }
        }

        public void WaitForNextFrameProperly()
        {
            double targetTimeForCurrentFrame = frameCount * targetFrameDurationMs;

            double actualElapsed = frameStopwatch.Elapsed.TotalMilliseconds;

            int waitTime = (int)(targetTimeForCurrentFrame - actualElapsed);

            if (waitTime > 1) 
            {
                Thread.Sleep(waitTime - 1); 
            }

            while (frameStopwatch.Elapsed.TotalMilliseconds < targetTimeForCurrentFrame)
            {
                Thread.SpinWait(10); 
            }

            frameCount++;
        }
    }
}
