using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindowsSshServer
{
    public class SshLanguage
    {
        public SshLanguage(string tag)
        {
            this.Tag = tag;
        }

        public SshLanguage()
        {
        }

        public string Tag
        {
            get;
            set;
        }
    }
}
