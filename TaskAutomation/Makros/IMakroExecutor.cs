using ImageHelperMethods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskAutomation.Makros
{
    public interface IMakroExecutor
    {
        /// <summary>
        /// Führt ein Makro aus./>.
        /// </summary>
        Task ExecuteMakro(Makro makro, DxgiResources dxgi, CancellationToken ct);
    }
}
