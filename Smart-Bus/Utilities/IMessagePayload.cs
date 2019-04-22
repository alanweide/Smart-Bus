using System;
using Microsoft.SPOT;

namespace Smart_Bus
{
    public interface IMessagePayload
    {
        // Implemented by:
        //  Request
        //  Request_v
        //  Route
        //  PayloadDateTime
        //  PayloadSimpleString
        //  PayloadRouteRequestForward
        //  PayloadRouteChangeAckResponse

        string BuildPayload();
    }
}
