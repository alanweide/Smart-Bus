using System;
using System.Text;
using Microsoft.SPOT;

namespace Smart_Bus
{
    public class Utilities
    {
        // 1 is Real Time (i.e., one second of wall time per second of simulation time)
        // Suggested values are on the order of 100 (every 10 milliseconds of wall time is one second of simulation time)
        public static readonly int TIME_MULTIPLIER = 1;

        public static void Main(String[] args)
        {
            Debug.Print("Utilities started");
        }

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
