using System;
using System.Text;
using Microsoft.SPOT;
using System.Threading;
using Microsoft.SPOT.Hardware;
using Microsoft.SPOT.Messaging;
using System.IO.Ports;
using Samraksh.SPOT.Emulator.Network;

namespace Smart_Bus
{
    public class Program
    {
        // 1 is Real Time (i.e., one second of wall time per second of simulation time)
        // Suggested values are on the order of 100 (every 10 milliseconds of wall time is one second of simulation time)
        private static readonly long TIME_MULTIPLIER = 100;

        private static readonly long currentTimeInMillis
        {
            get { return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond; }
        }

        private static long getRealDelay(long simulationMillis)
        {
            return simulationMillis / TIME_MULTIPLIER;
        }

        public static void Main()
        {
            IRequestPattern pattern = new RequestPattern_2P_2S_2B();
            long startTime = currentTimeInMillis;
            while (pattern.remainingRequests() > 0)
            {
                Request request = pattern.getNextRequest();
                
            }
        }

    }
}
