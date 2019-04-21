using System;
using System.Threading;
using Microsoft.SPOT;
using System.Text;

namespace Smart_Bus
{
    public class Request : IComparable, IMessagePayload
    {
        public int requestSendTime;
        public Request_v origin;
        public Request_v destination;
        public bool delay;

        public Request(int requestId, int earliestPickupTime, int latestDeliveryTime, int origin, int destination)
        {
            BusStop originStop = new BusStop(origin);
            BusStop destStop = new BusStop(destination);
            int latestPickupTime = latestDeliveryTime - Utilities.TravelTime(originStop, destStop);
            int earliestDeliveryTime = earliestPickupTime + Utilities.TravelTime(originStop, destStop);

            this.requestSendTime = earliestPickupTime;
            this.origin = new Request_v(
                requestId,
                earliestPickupTime,
                latestPickupTime,
                true,
                originStop,
                false);
            this.destination = new Request_v(
                requestId,
                earliestDeliveryTime,
                latestDeliveryTime,
                false,
                destStop,
                false);
        }

        public Request(Request_v origin, Request_v destination)
        {
            this.requestSendTime = origin.earliestServingTime;
            this.origin = origin;
            this.destination = destination;
        }

        public Request(string[] messageComponents, ref int startIdx)
        {
            // After the header, the array is organized as follows:
            //  [earliestPickupTime, latestDeliveryTime, origin, destination]
            // where the times are in ms since simulation start

            int requestId = int.Parse(messageComponents[startIdx++]);
            int earliestPickupTime = int.Parse(messageComponents[startIdx++]);
            int latestDeliveryTime = int.Parse(messageComponents[startIdx++]);
            int originId = int.Parse(messageComponents[startIdx++]);
            int destinationId = int.Parse(messageComponents[startIdx++]);

            BusStop originStop = new BusStop(originId);
            BusStop destStop = new BusStop(destinationId);
            int latestPickupTime = latestDeliveryTime - Utilities.TravelTime(originStop, destStop);
            int earliestDeliveryTime = earliestPickupTime + Utilities.TravelTime(originStop, destStop);

            this.requestSendTime = earliestPickupTime;
            this.origin = new Request_v(requestId, earliestPickupTime, latestPickupTime, true, originStop, false);
            this.destination = new Request_v(requestId, earliestDeliveryTime, latestDeliveryTime, false, destStop, false);
        }

        public int CompareTo(object obj)
        {
            if (obj == null) return 1;

            Request other = obj as Request;
            if (other != null)
            {
                return System.Math.Sign(this.origin.earliestServingTime - other.origin.earliestServingTime);
            }
            else
            {
                throw new ArgumentException("obj is not a Request");
            }
        }

        public string BuildPayload()
        {
            // After the header, the array is organized as follows:
            //  [earliestPickupTime, latestDeliveryTime, origin, destination]
            // where the times are in ms since simulation start

            StringBuilder payload = new StringBuilder();
            payload.Append(this.origin.earliestServingTime.ToString() + " ");
            payload.Append(this.destination.latestServingTime.ToString() + " ");
            payload.Append(this.origin.stop.id.ToString() + " ");
            payload.Append(this.destination.stop.id.ToString() + " ");
            return payload.ToString();
        }
    }

    public class Request_v : IMessagePayload
    {
        public int requestId;
        public int earliestServingTime;
        public int latestServingTime;
        public bool is_origin;
        public BusStop stop;
        public bool served;

        public Request_v()
        {

        }

        public Request_v(int id, int earliestServingTime, int latestServingTime, bool is_origin, BusStop stop, bool served)
        {
            this.requestId = id;
            this.earliestServingTime = earliestServingTime;
            this.latestServingTime = latestServingTime;
            this.is_origin = is_origin;
            this.stop = stop;
            this.served = served;
        }

        public Request_v(string[] messageComponents, ref int startIdx)
        {
            this.requestId = int.Parse(messageComponents[startIdx++]);
            this.earliestServingTime = int.Parse(messageComponents[startIdx++]);
            this.latestServingTime = int.Parse(messageComponents[startIdx++]);
            this.is_origin = (int.Parse(messageComponents[startIdx++]) != 0);
            this.stop = new BusStop(int.Parse(messageComponents[startIdx++]));
            this.served = (int.Parse(messageComponents[startIdx++]) != 0);
        }

        public string BuildPayload()
        {
            StringBuilder payload = new StringBuilder();
            payload.Append(this.requestId.ToString() + " ");
            payload.Append(this.earliestServingTime.ToString() + " ");
            payload.Append(this.latestServingTime.ToString() + " ");
            payload.Append(this.is_origin ? "1 " : "0 ");
            payload.Append(this.stop.id);
            payload.Append(this.served ? "1 " : "0 ");
            return payload.ToString();
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;

            Request_v other = obj as Request_v;
            if (other != null)
            {
                return
                    this.requestId == other.requestId &&
                    this.earliestServingTime == other.earliestServingTime &&
                    this.latestServingTime == other.latestServingTime &&
                    this.is_origin == other.is_origin &&
                    this.stop == other.stop;
            }
            else
            {
                throw new ArgumentException("obj is not a Request");
            }
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
