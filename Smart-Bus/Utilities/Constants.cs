using System;
using Microsoft.SPOT;

namespace Smart_Bus
{
    public class Constants
    {
        // Every hop between bus stops is constant; for this demo, it takes 10 simulated seconds
        public static readonly int HOP_DURATION = 10 * 1000;
        public static readonly int STOP_DURATION = 2 * 1000;

        // 1 is Real Time (i.e., one second of wall time per second of simulation time)
        // Suggested values are on the order of 100 (every 10 milliseconds of wall time is one second of simulation time)
        public static readonly int TIME_MULTIPLIER = 100;

        // DateTime format string for all messages
        public static string DATE_TIME_FORMAT = "yyyy'-'MM'-'dd'T'H':'mm':'ss.fff";
        public static char[] DATE_FORMAT_SEPARATORS = new char[] { '-', 'T', ':', '.' };
        public enum DateComponentPositions
        {
            YEAR = 0,
            MONTH = 1,
            DAY = 2,
            HOUR = 3,
            MINUTE = 4,
            SECOND = 5,
            MILLISECOND = 6
        }
    }
}
