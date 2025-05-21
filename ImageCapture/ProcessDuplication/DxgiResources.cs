using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageCapture.ProcessDuplication
{
    public class DxgiResources : IDisposable
    {
        public Factory1 Factory { get; set; }
        public Dictionary<int, Adapter1> Adapters { get; set; } = new();
        public Dictionary<(int adapterIdx, int outputIdx), Output1> Outputs { get; set; } = new();

        public void Dispose()
        {
            foreach (var output in Outputs.Values)
                output.Dispose();
            foreach (var adapter in Adapters.Values)
                adapter.Dispose();
            Factory?.Dispose();
        }
    }
}
