using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskAutomation.Scripts
{
    public interface IScriptExecutor
    {
        Task ExecuteScriptFile(string scriptPath, CancellationToken ct);
    }
}
