using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;
using System.Text;

namespace WindowsSshServer
{
    [RunInstaller(true)]
    public class SshServiceInstaller : Installer
    {
        // To Do: make installer specify dependencies in registry.
        // See bottom of http://www.montgomerysoftware.com/CreatingWindowsServiceInCSharp.aspx.
        public SshServiceInstaller()
            : base()
        {
            // Add service installer.
            var serviceInstaller = new ServiceInstaller();

            serviceInstaller.ServiceName = SshService.ServiceName;
            serviceInstaller.DisplayName = "Windows SSH Server";

            this.Installers.Add(serviceInstaller);

            // Add service process installer.
            var serviceProcInstaller = new ServiceProcessInstaller();

            serviceProcInstaller.Account = ServiceAccount.LocalSystem;
            serviceProcInstaller.Username = null;
            serviceProcInstaller.Password = null;

            this.Installers.Add(serviceProcInstaller);
        }

        public override void Install(System.Collections.IDictionary stateSaver)
        {
            base.Install(stateSaver);
        }

        public override void Uninstall(System.Collections.IDictionary savedState)
        {
            base.Uninstall(savedState);
        }
    }
}
