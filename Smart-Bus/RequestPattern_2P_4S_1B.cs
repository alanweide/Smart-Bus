using System;
using Microsoft.SPOT;
using Smart_Bus;

namespace Smart_Bus
{
    // Request Pattern with 2 passengers, 4 stops, and 1 bus
    class RequestPattern_2P_4S_1B : IRequestPattern
    {

        private Request[] requests = 
        { 
            new Request(1, 5 * 60 * 1000, 15 * 60 * 1000, 1, 3), 
            new Request(2, 10 * 60 * 1000, 20 * 60 * 1000, 4, 2)
        };
        
        private int currentRequest = 0;
        private int numStops = 4;
        private int numBuses = 1;

        public RequestPattern_2P_4S_1B()
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
