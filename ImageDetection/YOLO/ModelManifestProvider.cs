using ImageDetection.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageDetection.YOLO
{
    public static class ModelManifestProvider
    {
        public static ModelRegistry LoadEmbedded()
        {
            var asm = typeof(ModelManifestProvider).Assembly;
            using var s = asm.GetManifestResourceStream("DesktopAutomation.yolo-models.json");
            using var r = new StreamReader(s!);
            var json = r.ReadToEnd();
            return ModelRegistry.FromJson(json);
        }
    }
}
