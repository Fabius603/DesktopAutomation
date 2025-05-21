using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Device = SharpDX.Direct3D11.Device;


namespace ImageCapture.ProcessDuplication
{
    public class DuplicationContext : IDisposable
    {
        public Adapter1 Adapter { get; }
        public Output1 OutputInterface { get; }
        public Device Device { get; }
        public OutputDuplication Duplication { get; }

        public DuplicationContext(Adapter1 adapter, Output1 output, Device device, OutputDuplication duplication)
        {
            Adapter = adapter;
            OutputInterface = output;
            Device = device;
            Duplication = duplication;
        }

        public void Dispose()
        {
            Duplication.Dispose();
            OutputInterface.Dispose();
            Adapter.Dispose();
            Device.Dispose();
        }
    }
}
