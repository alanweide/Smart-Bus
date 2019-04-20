using System;
using Microsoft.SPOT;

namespace Smart_Bus
{
    public class PayloadRouteChangeAckResponse: IMessagePayload
    {
        public bool didAcceptRouteChange;
        public Route latestRoute;

        public PayloadRouteChangeAckResponse(bool didAccept, Route route)
        {
            this.didAcceptRouteChange = didAccept;
            this.latestRoute = route;
        }

        public PayloadRouteChangeAckResponse(string[] messageComponents, ref int startIdx)
        {
            this.didAcceptRouteChange = (int.Parse(messageComponents[startIdx++]) != 0);
            this.latestRoute = new Route(messageComponents, ref startIdx);
        }

        public string BuildPayload()
        {
            return (didAcceptRouteChange ? "1 " : "0 ") + latestRoute.ToString();
        }
    }
}
