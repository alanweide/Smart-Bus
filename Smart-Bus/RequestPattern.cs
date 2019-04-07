using System;
using Microsoft.SPOT;

namespace Smart_Bus
{
    interface IRequestPattern
    {
        public class Request : IComparable
        {
            public int earliestServingTime;
            public int latestServingTime;
            public int origin;
            public int destination;

            public Request(int earliestServingTime, int latestServingTime, int origin, int destination)
            {
                this.earliestServingTime = earliestServingTime;
                this.latestServingTime = latestServingTime;
                this.origin = origin;
                this.destination = destination;
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

        // Returns the number of requests remaining
        public int remainingRequests();

        // Gets the next request in order, and advances the cursor
        public Request getNextRequest();

        public int numberOfPassengers();
        public int numberOfStops();
        public int numberOfBuses();
    }
}
