using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageDetection.Model
{
    public sealed class ModelEntry
    {
        public string Url { get; set; } = "";
        public string Sha256 { get; set; } = "";
        public long Size { get; set; }
    }
}
