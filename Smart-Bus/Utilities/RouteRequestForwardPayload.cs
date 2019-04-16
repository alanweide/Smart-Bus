using System;
using Microsoft.SPOT;

namespace Smart_Bus
{
    public class RouteRequestForwardPayload: IMessagePayload
    {
        int originalSource;
        int numHops;

        public RouteRequestForwardPayload(int origSrc, int numHops)
        {
            this.originalSource = origSrc;
            this.numHops = numHops;
        }

        public void FromStringArray(string[] payload, int headerLength)
        {
            throw new NotImplementedException();
        }

        public string BuildPayload()
        {
            throw new NotImplementedException();
        }
    }
}
