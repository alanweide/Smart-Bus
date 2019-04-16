using System;
using Microsoft.SPOT;

namespace Smart_Bus
{
    public struct BusStatus
    {
        // Together, these identify the location of a bus within the network.
        // timeRemainingInState is either
        //   ms until the bus arrives at nextStop (previousStop != nextStop), or
        //   ms until the bus departs from the stop at which it is currently waiting (previousStop = nextStop)
        // routeIdx is the index of the location on the bus's route; useful for computing future stops
        //   It is the idx of *previousStop* in the route.

        public int previousStop;
        public int nextStop;
        public int timeRemainingInState;
        public int capacity;
        public int routeIdx;
    }
}
