using System;
using Microsoft.SPOT;

namespace Smart_Bus
{
    public class SimplePayloadString: IMessagePayload
    {

        public string str;

        public SimplePayloadString(string s = "")
        {
            this.str = s;
        }

        public void FromStringArray(string[] payload, int headerLength)
        {
            this.str = payload[headerLength];
        }

        public string BuildPayload()
        {
            throw new NotImplementedException();
        }
    }
}
