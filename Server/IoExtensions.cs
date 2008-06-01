using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SshDotNet
{
    public static class IoExtensions
    {
        public static void WriteDigits(this Stream stream, int value)
        {
            foreach (char digit in value.ToString())
                stream.WriteByte((byte)digit);
        }
    }
}
