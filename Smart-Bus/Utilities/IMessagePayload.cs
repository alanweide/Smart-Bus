using System;
using Microsoft.SPOT;

namespace Smart_Bus
{
    public interface IMessagePayload
    {
        // Implemented by:
        //  Request
        //  Route
        //  PayloadDateTime
        //  PayloadSimpleString
        //  PayloadRouteRequestForward

        string BuildPayload();
    }
}
