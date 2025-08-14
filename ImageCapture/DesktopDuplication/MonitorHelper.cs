using ImageHelperMethods;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageCapture.DesktopDuplication
{
    public static class MonitorHelper
    {
        public static Screen GetScreenByMousePos(Point p)
        {
            Screen screen = ScreenHelper.GetScreens().FirstOrDefault(s => s.Bounds.Contains(p))
                     ?? Screen.PrimaryScreen;

            return screen;
        }

        public static int GetGlobalMonitorIndex(Screen screen)
            => Array.IndexOf(ScreenHelper.GetScreens(), screen);


        public static bool TryResolveAdapterOutput(Screen screen, out int adapterIndex, out int outputIndex)
        {
            adapterIndex = -1;
            outputIndex = -1;

            string target = screen.DeviceName; // z.B. "\\.\DISPLAY2"

            using (var factory = new Factory1())
            {
                var adapters = factory.Adapters1; // Adapter1[]
                for (int ai = 0; ai < adapters.Length; ai++)
                {
                    using (var adapter = adapters[ai])
                    {
                        var outs = adapter.Outputs; // Output[]
                        for (int oi = 0; oi < outs.Length; oi++)
                        {
                            using (var output = outs[oi])
                            {
                                var od = output.Description;
                                if (string.Equals(od.DeviceName, target, StringComparison.OrdinalIgnoreCase))
                                {
                                    adapterIndex = ai;
                                    outputIndex = oi;
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }
    }
}
