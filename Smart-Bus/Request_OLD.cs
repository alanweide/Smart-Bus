using System;
using System.Threading;
using Microsoft.SPOT;
using Smart_Bus;

namespace Smart_Bus
{
    public class Request_OLD : IComparable
    {
        public int earliestServingTime;
        public int latestServingTime;
        public BusStop origin;
        public BusStop destination;

        public Request_OLD(int earliestServingTime, int latestServingTime, int origin, int destination)
        {
            this.earliestServingTime = earliestServingTime;
            this.latestServingTime = latestServingTime;
            this.origin = new BusStop(origin);
            this.destination = new BusStop(destination);
        }

        public int CompareTo(object obj)
        {
            if (obj == null) return 1;

            Request_OLD other = obj as Request_OLD;
            if (other != null)
            {
                return System.Math.Sign(this.earliestServingTime - other.earliestServingTime);
            }
            else
            {
                throw new ArgumentException("obj is not a Request");
            }
        }
    }
}
