using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskAutomation.Hotkeys
{
    public class HotkeyPressedEventArgs : EventArgs
    {
        public JobReference Job { get; }
        public HotkeyPressedEventArgs(JobReference job) => Job = job;
    }
}
