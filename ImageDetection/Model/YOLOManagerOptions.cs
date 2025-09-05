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
        /// <summary>
        /// GPU Backend preference. Auto will try CUDA -> DirectML -> CPU fallback.
        /// </summary>
        public YoloGpuBackend GpuBackend { get; set; } = YoloGpuBackend.Auto;
        public GraphOptimizationLevel Optimization { get; set; } = GraphOptimizationLevel.ORT_ENABLE_ALL;
    }
}
