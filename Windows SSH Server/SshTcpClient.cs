using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Text;

namespace WindowsSshServer
{
    public class SshTcpClient : SshClient
    {
        public SshTcpClient(TcpClient tcpClient)
            : base(tcpClient)
        {
        }

        //
    }
}
