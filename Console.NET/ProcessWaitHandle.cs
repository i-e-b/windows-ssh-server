using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace ConsoleDotNet
{
    internal class ProcessWaitHandle : WaitHandle
    {
        public ProcessWaitHandle(SafeWaitHandle proccessWaitHandle) : base()
        {
            this.SafeWaitHandle = proccessWaitHandle;
        }
    }
}
