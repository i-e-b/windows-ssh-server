using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindowsSshServer
{
    public static class BasicExtensions
    {
        public static bool Equals(this byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;

            for (int i = 0; i < a.Length; i++)
                if (a[i] != b[i]) return false;

            return true;
        }
    }
}
