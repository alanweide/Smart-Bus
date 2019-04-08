using System;
using Microsoft.SPOT;

namespace Smart_Bus
{
    interface IRequestPattern
    {

        // Returns the number of requests remaining
        int remainingRequests();

        // Gets the next request in order, and advances the cursor
        Request getNextRequest();

        int numberOfPassengers();
        int numberOfStops();
        int numberOfBuses();
    }
}
