using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageCapture.DesktopDuplication
{
    public class DesktopDuplicationException : Exception
    {
        public DesktopDuplicationException() { }
        public DesktopDuplicationException(string message) : base(message) { }
        public DesktopDuplicationException(string message, Exception inner) : base(message, inner) { }
    }
}
