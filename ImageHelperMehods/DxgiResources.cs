using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageHelperMethods 
{
    public class DxgiResources : IDisposable
    {
        public Factory1 Factory { get; private set; }
        public Dictionary<int, Adapter1> Adapters { get; private set; } = new();
        public Dictionary<(int adapterIdx, int outputIdx), Output1> Outputs { get; private set; } = new();

        private static readonly Lazy<DxgiResources> _instance = new Lazy<DxgiResources>(() =>
        {
            var instance = new DxgiResources();
            instance.Initialize();
            return instance;
        });

        public static DxgiResources Instance => _instance.Value;

        private DxgiResources() { }

        private void Initialize()
        {
            Factory = new Factory1();

            for (int adapterIdx = 0; ; adapterIdx++)
            {
                Adapter1 adapter;
                try
                {
                    adapter = Factory.GetAdapter1(adapterIdx);
                }
                catch (SharpDX.SharpDXException)
                {
                    break;
                }

                Adapters.Add(adapterIdx, adapter);

                for (int outputIdx = 0; ; outputIdx++)
                {
                    try
                    {
                        var output = adapter.GetOutput(outputIdx).QueryInterface<Output1>();
                        Outputs.Add((adapterIdx, outputIdx), output);
                    }
                    catch (SharpDX.SharpDXException)
                    {
                        break;
                    }
                }
            }
        }

        public void Dispose()
        {
            foreach (var output in Outputs.Values)
                output.Dispose();
            Outputs.Clear();

            foreach (var adapter in Adapters.Values)
                adapter.Dispose();
            Adapters.Clear();

            Factory?.Dispose();
            Factory = null;
        }
    }
}
