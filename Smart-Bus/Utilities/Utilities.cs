using System;
using System.Text;
using Microsoft.SPOT;

namespace Smart_Bus
{
    public class Utilities
    {
        public static DateTime simStart;

        public int ElapsedMillis()
        {
            TimeSpan elapsedTime = DateTime.Now - simStart;
            return (int)elapsedTime.Milliseconds;
        }

        public static void Main(String[] args)
        {
            Debug.Print("Use this for simple testing of Utilities methods");

            DateTime foo = Utilities.ParseDateTime("2019-04-16T16:24:01.234");
            Debug.Print(foo.ToString());
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

        public static DateTime ParseDateTime(string payload)
        {
            // Unfortunately there are no built-in parsers for DateTime in the MicroFramework, so I made my own.

            string[] components = payload.Split(Constants.DATE_FORMAT_SEPARATORS);
            int year = Int32.Parse(components[(int)Constants.DateComponentPositions.YEAR]);
            int month = Int32.Parse(components[(int)Constants.DateComponentPositions.MONTH]);
            int day = Int32.Parse(components[(int)Constants.DateComponentPositions.DAY]);
            int hour = Int32.Parse(components[(int)Constants.DateComponentPositions.HOUR]);
            int minute = Int32.Parse(components[(int)Constants.DateComponentPositions.MINUTE]);
            int second = Int32.Parse(components[(int)Constants.DateComponentPositions.SECOND]);
            int millisecond = Int32.Parse(components[(int)Constants.DateComponentPositions.MILLISECOND]);
            return new DateTime(year, month, day, hour, minute, second, millisecond);
        }
    }
}
