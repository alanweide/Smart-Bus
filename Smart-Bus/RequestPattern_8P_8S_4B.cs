using System;
using Microsoft.SPOT;
using Smart_Bus;

namespace Smart_Bus
{
    class RequestPattern_8P_8S_4B: IRequestPattern
    {

        private Request[] requests = 
        {
            new Request(1, 1 * 1000, 11 * 1000, 1, 5), 
            new Request(2, 2 * 1000, 12 * 1000, 3 ,8),
            new Request(3, 3 * 1000, 13 * 1000, 6, 2), 
            new Request(4, 4 * 1000, 14 * 1000, 2, 1),
            new Request(5, 5 * 1000, 15 * 1000, 7, 5), 
            new Request(6, 6 * 1000, 16 * 1000, 1, 4),
            new Request(7, 7 * 1000, 17 * 1000, 3, 2), 
            new Request(8, 8 * 1000, 18 * 1000, 7, 8) 
        };
        
        private int currentRequest = 0;
        private int numStops = 8;
        private int numBuses = 4;

        public RequestPattern_8P_8S_4B()
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
