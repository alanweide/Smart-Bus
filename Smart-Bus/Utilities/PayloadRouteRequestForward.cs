using System;
using Microsoft.SPOT;

namespace Smart_Bus
{
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
