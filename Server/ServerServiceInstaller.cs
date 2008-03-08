using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Configuration.Install;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;

namespace WindowsSshServer
{
    [RunInstaller(true)]
    public class ServerServiceInstaller : Installer
    {
        public ServerServiceInstaller()
            : base()
        {
            // Add service installer.
            var serviceInstaller = new ServiceInstaller();

            serviceInstaller.ServiceName = ServerService.ServiceName;
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

            // Add event log installer.
            var eventLogInstaller = new EventLogInstaller();

            //// Check if event log source does not yet exist.
            //if (!EventLog.SourceExists(SshService.EventSourceName))

            // Create event log.
            eventLogInstaller.Source = ServerService.EventSourceName;
            eventLogInstaller.Log = ServerService.EventLogName;
            //eventLogInstaller.MessageResourceFile = _eventLogMessagesFileName;
            //eventLogInstaller.CategoryResourceFile = _eventLogMessagesFileName;
            //eventLogInstaller.CategoryCount = 0;
            //eventLogInstaller.ParameterResourceFile = _eventLogMessagesFileName;
            eventLogInstaller.UninstallAction = UninstallAction.Remove;

            this.Installers.Add(eventLogInstaller);
        }

        protected override void OnBeforeInstall(System.Collections.IDictionary savedState)
        {
            base.OnBeforeInstall(savedState);
        }
        
        protected override void OnAfterInstall(System.Collections.IDictionary savedState)
        {
            // Register display name for event log.
            using (var eventLog = new EventLog(ServerService.EventLogName, ".",
                ServerService.EventSourceName))
            {
                var messagesFileName = Path.Combine(Path.GetDirectoryName(this.Context.Parameters
                    ["assemblypath"]), "EventLogMsgs.dll");

                eventLog.RegisterDisplayName(messagesFileName, 5001);
            }

            base.OnAfterInstall(savedState);
        }

        protected override void OnBeforeUninstall(System.Collections.IDictionary savedState)
        {
            base.OnBeforeUninstall(savedState);
        }

        protected override void OnAfterUninstall(System.Collections.IDictionary savedState)
        {
            base.OnAfterUninstall(savedState);
        }
    }
}
