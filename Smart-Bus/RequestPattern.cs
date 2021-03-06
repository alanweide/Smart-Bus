using System;
using Microsoft.SPOT;
using Smart_Bus;

namespace Smart_Bus
{
    interface IRequestPattern
    {

        // Returns the number of requests remaining
        int remainingRequests();

        // Gets the next request in order, and advances the cursor
        Request NextRequest();

        // Returns the total number of requests in this pattern
        int numberOfPassengers();

        int numberOfStops();
        int numberOfBuses();
    }
}
