using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WindowsSshServer
{
    public class SshConnectionService : SshService
    {
        public SshConnectionService(SshClient client) : base(client)
        {
        }

        public override string Name
        {
            get { return "ssh-connection"; }
        }

        internal override bool ProcessMessage(byte[] payload)
        {
            throw new NotImplementedException();
        }

        internal override void Start()
        {
            throw new NotImplementedException();
        }
    }
}
