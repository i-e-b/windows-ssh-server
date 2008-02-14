using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SshDotNet
{
    public class SshPublicKey
    {
        public static SshPublicKey FromFile(string fileName)
        {
            using (var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                return FromStream(fileStream);
            }
        }

        public static SshPublicKey FromStream(Stream stream)
        {
            var obj = new SshPublicKey();

            //

            return obj;
        }

        public SshPublicKey()
        {
            //
        }
    }
}
