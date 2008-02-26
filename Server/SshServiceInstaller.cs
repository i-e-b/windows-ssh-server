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
        public SshServiceInstaller()
            : base()
        {
            // Add service installer.
            var serviceInstaller = new ServiceInstaller();
            
            serviceInstaller.ServiceName = SshService.ServiceName;
            serviceInstaller.DisplayName = "Windows SSH Server";
            serviceInstaller.Description = "Provides SSH (Secure Shell) access to the computer.";
            serviceInstaller.StartType = ServiceStartMode.Automatic;
            serviceInstaller.ServicesDependedOn = new string[] { "tcpip" };
            
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
