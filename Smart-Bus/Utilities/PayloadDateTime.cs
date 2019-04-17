using System;
using Microsoft.SPOT;

namespace Smart_Bus
{
    public class PayloadDateTime : IMessagePayload
    {
        public DateTime date;

        public static PayloadDateTime Now
        {
            get { return new PayloadDateTime(DateTime.Now); }
            private set { }
        }

        public PayloadDateTime(DateTime date)
        {
            this.date = date;
        }

        public PayloadDateTime(string[] payload, ref int startIdx)
        {
            this.date = Utilities.ParseDateTime(payload[startIdx++]);
        }

        public static TimeSpan operator -(PayloadDateTime pd, DateTime d)
        {
            return pd.date - d;
        }

        public static TimeSpan operator -(PayloadDateTime pd1, PayloadDateTime pd2)
        {
            return pd1.date - pd2.date;
        }

        public static TimeSpan operator -(DateTime d, PayloadDateTime pd)
        {
            return d - pd.date;
        }

        public string BuildPayload()
        {
            return this.date.ToString(Constants.DATE_TIME_FORMAT);
        }
    }
}
