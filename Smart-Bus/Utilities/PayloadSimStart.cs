using System;
using Microsoft.SPOT;

namespace Smart_Bus
{
    public class PayloadSimStart : IMessagePayload
    {
        public DateTime date;
        int numBuses;

        public PayloadSimStart(DateTime date, int numBuses)
        {
            this.date = date;
            this.numBuses = numBuses;
        }

        public PayloadSimStart(string[] messageComponents, ref int startIdx)
        {
            this.date = Utilities.ParseDateTime(messageComponents[startIdx++]);
            this.numBuses = int.Parse(messageComponents[startIdx++]);
        }

        public string BuildPayload()
        {
            return this.date.ToString(Constants.DATE_TIME_FORMAT) + " " + this.numBuses.ToString() + " ";
        }
    }
}
