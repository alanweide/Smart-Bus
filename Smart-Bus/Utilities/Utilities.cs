using System;
using System.Text;
using Microsoft.SPOT;
using System.Threading;

namespace Smart_Bus
{
    public class Utilities
    {
        public static DateTime SimStart;

        public static int ElapsedMillis()
        {
            return (int)((DateTime.Now.Ticks - SimStart.Ticks) / TimeSpan.TicksPerMillisecond);
            //TimeSpan elapsedTime = DateTime.Now - SimStart;
            //return (int)elapsedTime.TotalMilliseconds;
        }

        public static int ElapsedSimulationMillis()
        {
            return ElapsedMillis() * Constants.TIME_MULTIPLIER;
        }

        public static void Main(String[] args)
        {
            Debug.Print("Use this for simple testing of Utilities methods");

            //SimStart = DateTime.Now;
            //Debug.Print(SimStart.ToString(Constants.DATE_TIME_FORMAT));
            //Thread.Sleep(1500);
            //Debug.Print("Milliseconds btwn " + SimStart.ToString(Constants.DATE_TIME_FORMAT) + " to " + DateTime.Now.ToString(Constants.DATE_TIME_FORMAT) + ": " + ElapsedMillis());
            //Thread.Sleep(Timeout.Infinite);

            SBMessage message = new SBMessage("11 1 -1 2 1 1 1000 15000 1 2 ");
            Debug.Print(message.header.type.ToString());
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

        public static int TravelTime(BusStop origin, BusStop destination)
        {
            // travel time = hopcount * Constants.HOP_DURATION (ms)
            // TODO: compute this from graph topology -- BFS?

            return System.Math.Abs(origin.id - destination.id) * Constants.HOP_DURATION;
        }


        public static DateTime ParseDateTime(string payload)
        {
            // Unfortunately there are no built-in parsers for DateTime in the MicroFramework, so I made my own.
            // This parses dates in the format yyyy-MM-ddTHH:mm:ss.fff

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
