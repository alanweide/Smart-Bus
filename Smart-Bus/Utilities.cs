using System;
using System.Text;
using Microsoft.SPOT;

namespace Smart_Bus
{
    class Utilities
    {
        public static byte[] StringToByteArray(String s)
        {
            byte[] sBytes = new byte[s.Length];
            for (int i = 0; i < sBytes.Length; i++)
            {
                sBytes[i] = (byte)s[i];
            }
            return sBytes;
        }

        public static String ByteArrayToString(byte[] b)
        {
            StringBuilder s = new StringBuilder();
            for (int i = 0; i < b.Length; i++)
            {
                s.Append((char)b[i]);
            }
            return s.ToString();
        }
    }
}
