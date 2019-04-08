using System;
using Microsoft.SPOT;

namespace Smart_Bus
{
    public class Request : IComparable
    {
        public int earliestServingTime;
        public int latestServingTime;
        public BusStop origin;
        public BusStop destination;

        public Request(long earliestServingTime, long latestServingTime, int origin, int destination)
        {
            this.earliestServingTime = earliestServingTime;
            this.latestServingTime = latestServingTime;
            this.origin = new BusStop(origin);
            this.destination = new BusStop(destination);
        }

        public int CompareTo(object obj)
        {
            if (obj == null) return 1;

            Request other = obj as Request;
            if (other != null)
            {
                return this.earliestServingTime - other.earliestServingTime;
            }
            else
            {
                throw new ArgumentException("obj is not a Request");
            }
        }
    }
}
