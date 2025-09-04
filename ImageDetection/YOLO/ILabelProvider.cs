using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageDetection.YOLO
{
    public interface ILabelProvider
    {
        /// <summary> Liefert die Labels (Index = Klassen-ID). </summary>
        IReadOnlyList<string> GetLabels(string modelKey, string onnxPath);
    }
}
