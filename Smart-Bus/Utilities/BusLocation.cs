using System;
using Microsoft.SPOT;

namespace Smart_Bus
{
    struct BusLocation
    {
        // Together, these identify the location of a bus within the network.
        // timeRemainingInState is either
        //  the number of ms until the bus arrives at nextStop, or
        //  the number of ms until the bus departs from the stop at which it is currently waiting

        int previousStop;
        int nextStop;
        int timeRemainingInState;

        public BusLocation(int previousStop, int nextStop, int timeRemainingInState)
        {
            this.previousStop = previousStop;
            this.nextStop = nextStop;
            this.timeRemainingInState = timeRemainingInState;
        }
    }
}
