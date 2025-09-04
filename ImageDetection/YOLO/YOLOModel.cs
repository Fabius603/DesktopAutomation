using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageDetection.YOLO
{
    public class YOLOModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string OnnxPath { get; set; }
        public long OnnxSizeBytes { get; set; }
        public DateTime CreatedUtc { get; set; }
    }
}
