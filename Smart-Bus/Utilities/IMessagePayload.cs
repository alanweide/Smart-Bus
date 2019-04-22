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
        // This interface provides a way to store (semi)arbitrary
        //  data types in a message, and to standardize message
        //  formats without specifying it explicitly.

        string BuildPayload();
    }
}
