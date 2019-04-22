using System;
using Microsoft.SPOT;

namespace Smart_Bus
{
    // Not used. Payload class for ROUTE_REQUEST_FORWARD messages,
    //  to be used in the future as part of the BFS operation.
    public class PayloadRouteRequestForward: IMessagePayload
    {
        int originalSource;
        int numHops;

        public PayloadRouteRequestForward(string[] payload, ref int startIdx)
        {
            this.originalSource = int.Parse(payload[startIdx++]);
            this.numHops = int.Parse(payload[startIdx++]);
        }

        public string BuildPayload()
        {
            return this.originalSource.ToString() + " " + this.numHops.ToString();
        }
    }
}
