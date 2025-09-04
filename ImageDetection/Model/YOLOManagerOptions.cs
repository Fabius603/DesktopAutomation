using ImageDetection.YOLO;
using Microsoft.ML.OnnxRuntime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageDetection.Model
{
    public sealed class YoloManagerOptions
    {
        public int InputSize { get; set; } = 640; // Ultralytics-Standard
        public float NmsIou { get; set; } = 0.45f;
        public YoloGpuBackend GpuBackend { get; set; } = YoloGpuBackend.DirectML;
        public bool UseGpuIfAvailable { get; set; } = false; // für CPU belassen
        public GraphOptimizationLevel Optimization { get; set; } = GraphOptimizationLevel.ORT_ENABLE_ALL;
    }
}
