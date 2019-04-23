using System;
using Microsoft.SPOT;
using Smart_Bus;

namespace Smart_Bus
{
    // Request Pattern with 4 passengers, 4 stops, and 2 buses
    class RequestPattern_4P_4S_2B : IRequestPattern
    {

        private Request[] requests = 
        { 
            new Request(1, 5 * 60 * 1000, 15 * 60 * 1000, 1, 2),
            new Request(2, 10 * 60 * 1000, 20 * 60 * 1000, 4, 3),
            new Request(3, 7 * 60 * 1000, 17 * 60 * 1000, 1, 4),
            new Request(4, 12 * 60 * 1000, 22 * 60 * 1000, 3, 2)
        };
        
        private int currentRequest = 0;
        private int numStops = 4;
        private int numBuses = 2;

        public RequestPattern_4P_4S_2B()
        {
            // Sort the requests array in order of earliestServingTime on startup
            // This is in-place selection sort so it's inefficient, but there 
            // won't be many requests during testing so it's fine
            for (int i = 0; i < requests.Length - 1; i++)
            {
                int minIdx = i;
                for (int j = i + 1; j < requests.Length; j++)
                {
                    if (requests[j].CompareTo(requests[minIdx]) < 0)
                    {
                        minIdx = j;
                    }
                }
                if (minIdx != i)
                {
                    var temp = requests[i];
                    requests[i] = requests[minIdx];
                    requests[minIdx] = temp;
                }
            }
        }

        public int numberOfPassengers()
        {
            return requests.Length;
        }

        public int numberOfStops()
        {
            return numStops;
        }

        public int numberOfBuses()
        {
            return numBuses;
        }

        public int remainingRequests()
        {
            return requests.Length - currentRequest;
        }

        public Request NextRequest()
        {
            currentRequest++;
            return requests[currentRequest - 1];
        }
    }
}
