using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;

namespace WindowsSshServer
{
    public partial class SshService : ServiceBase
    {
        public static new string ServiceName
        {
            get { return "WindowsSshServer"; }
        }

        protected SshTcpServer _tcpServer; // TCP server for SSH connections.

        public SshService()
            : base()
        {
            // Create TCP server.
            _tcpServer = new SshTcpServer();

            this.Disposed += new EventHandler(SshServerService_Disposed);
        }

        protected override void OnStart(string[] args)
        {
            // Start TCP server.
            _tcpServer.Start();

            base.OnStart(args);
        }

        protected override void OnStop()
        {
            // Stop TCP server.
            _tcpServer.Stop();

            base.OnStop();
        }

        protected override void OnPause()
        {
            //

            base.OnPause();
        }

        protected override void OnContinue()
        {
            //

            base.OnContinue();
        }

        protected override bool OnPowerEvent(PowerBroadcastStatus powerStatus)
        {
            //

            return base.OnPowerEvent(powerStatus);
        }

        protected override void OnShutdown()
        {
            //

            base.OnShutdown();
        }

        protected override void OnSessionChange(SessionChangeDescription changeDescription)
        {
            //

            base.OnSessionChange(changeDescription);
        }

        protected override void OnCustomCommand(int command)
        {
            //

            base.OnCustomCommand(command);
        }

        private void SshServerService_Disposed(object sender, EventArgs e)
        {
            // Dispose TCP server.
            _tcpServer.Dispose();
        }
    }
}
