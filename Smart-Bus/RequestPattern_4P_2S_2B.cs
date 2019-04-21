using System;
using Microsoft.SPOT;
using Smart_Bus;

namespace Smart_Bus
{
    class RequestPattern_4P_2S_2B: IRequestPattern
    {

        private Request[] requests = 
        {
            new Request(1, 1 * 1000, 11 * 1000, 1, 2), 
            new Request(2, 2 * 1000, 12 * 1000, 2, 1),
            new Request(3, 3 * 1000, 13 * 1000, 1, 2), 
            new Request(4, 4 * 1000, 14 * 1000, 2, 1) 
        };
        
        private int currentRequest = 0;
        private int numStops = 2;
        private int numBuses = 2;

        public RequestPattern_4P_2S_2B()
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
