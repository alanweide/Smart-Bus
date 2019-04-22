using System;
using Microsoft.SPOT;

namespace Smart_Bus
{
    // Used primarily to mimic an "empty" payload.
    public class PayloadSimpleString: IMessagePayload
    {

        public string str;

        public PayloadSimpleString(string s = "")
        {
            this.str = s;
        }

        public PayloadSimpleString(string[] messageComponents, ref int startIdx)
        {
            this.str = messageComponents[startIdx++];
        }

        public string BuildPayload()
        {
            return this.str + " ";
        }
    }
}
