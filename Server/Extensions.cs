using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SshDotNet
{
    public static class Extensions
    {
        public static string GetName(this AuthenticationMethod method)
        {
            switch (method)
            {
                case AuthenticationMethod.PublicKey:
                    return "public key";
                case AuthenticationMethod.Password:
                    return "password";
                case AuthenticationMethod.HostBased:
                    return "host-based";
                case AuthenticationMethod.KeyboardInteractive:
                    return "keyboard-interactive";
            }

            return "";
        }
    }
}
