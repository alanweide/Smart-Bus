using System;
using Microsoft.SPOT;

namespace Smart_Bus
{
    public class Constants
    {
        // Every hop between bus stops is constant; for this demo, it takes 1 simulated minute
        public static readonly int BUS_HOP_TIME = 60000;

        // 1 is Real Time (i.e., one second of wall time per second of simulation time)
        // Suggested values are on the order of 100 (every 10 milliseconds of wall time is one second of simulation time)
        public static readonly int TIME_MULTIPLIER = 1;
    }
}
