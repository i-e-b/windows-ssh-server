using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindowsSshServer
{
    // This exception is only used to notify the thread that receives data that the client has disconnected.
    public class SshDisconnectedException : Exception
    {
        public SshDisconnectedException()
            : base()
        {
        }
    }
}
