using System;
using Microsoft.SPOT;

namespace Smart_Bus
{
    class RequestPattern_2P_2S: IRequestPattern
    {

        private Request[] requests = 
        { 
            new Request(5000, 6000, 0, 1), 
            new Request(10000, 11000, 1, 0)
        };
        
        private int currentRequest = 0;
        private int numStops = 2;
        private int numBuses = 2;

        public RequestPattern_2P_2S()
        {
            // Sort the requests array in order of earliestServingTime on startup
            // This is in-place selection sort so it's inefficient, but there won't be many requests so it's fine
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

        public Request getNextRequest()
        {
            currentRequest++;
            return requests[currentRequest - 1];
        }
    }
}
