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
        //  SimplePayloadInt

        void FromStringArray(string[] payload, int headerLength);
        string BuildPayload();
    }
}
