using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SshDotNet
{
    public interface IConnection : IDisposable
    {
        Stream GetStream();

        void Disconnect(bool remotely);
    }
}
