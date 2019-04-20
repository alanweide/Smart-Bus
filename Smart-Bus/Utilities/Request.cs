using System;
using System.Threading;
using Microsoft.SPOT;

namespace Smart_Bus
{
    public class Request : IComparable, IMessagePayload
    {
        public int requestSendTime;
        public Request_v origin;
        public Request_v destination;
        public bool delay;

        public Request(int earliestPickupTime, int latestDeliveryTime, int origin, int destination)
        {
            BusStop originStop = new BusStop(origin);
            BusStop destStop = new BusStop(destination);
            int latestPickupTime = latestDeliveryTime - TravelTime(originStop, destStop);
            int earliestDeliveryTime = earliestPickupTime + TravelTime(originStop, destStop);

            this.requestSendTime = earliestPickupTime;
            this.origin = new Request_v(earliestPickupTime, latestPickupTime, true, originStop, false);
            this.destination = new Request_v(earliestDeliveryTime, latestDeliveryTime, false, destStop, false);
        }

        public Request(string[] messageComponents, ref int startIdx)
        {
            // After the header, the array is organized as follows:
            //  [earliestPickupTime, latestDeliveryTime, origin, destination]
            // where the times are in ms since simulation start

            int earliestPickupTime = int.Parse(messageComponents[startIdx++]);
            int latestDeliveryTime = int.Parse(messageComponents[startIdx++]);
            int originId = int.Parse(messageComponents[startIdx++]);
            int destinationId = int.Parse(messageComponents[startIdx++]);

            BusStop originStop = new BusStop(originId);
            BusStop destStop = new BusStop(destinationId);
            int latestPickupTime = latestDeliveryTime - TravelTime(originStop, destStop);
            int earliestDeliveryTime = earliestPickupTime + TravelTime(originStop, destStop);

            this.requestSendTime = earliestPickupTime;
            this.origin = new Request_v(earliestPickupTime, latestPickupTime, true, originStop, false);
            this.destination = new Request_v(earliestDeliveryTime, latestDeliveryTime, false, destStop, false);
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

        public int TravelTime(BusStop origin, BusStop destination)
        {
            // travel time = hopcount * Constants.HOP_DURATION (ms)
            // TODO: compute this from graph topology -- BFS?

            return System.Math.Abs(origin.id - destination.id) * Constants.HOP_DURATION;
        }

        public string BuildPayload()
        {
            throw new NotImplementedException();
        }
    }

    public struct Request_v
    {
        public int earliestServingTime;
        public int latestServingTime;
        public bool is_origin;
        public BusStop stop;
        public bool served;

        public Request_v(int earliestServingTime, int latestServingTime, bool is_origin, BusStop stop, bool served)
        {
            this.earliestServingTime = earliestServingTime;
            this.latestServingTime = latestServingTime;
            this.is_origin = is_origin;
            this.stop = stop;
            this.served = served;
        }

        public Request_v(string[] messageComponents, ref int startIdx)
        {
            this.earliestServingTime = int.Parse(messageComponents[startIdx++]);
            this.latestServingTime = int.Parse(messageComponents[startIdx++]);
            this.is_origin = (int.Parse(messageComponents[startIdx++]) != 0);
            this.stop = new BusStop(int.Parse(messageComponents[startIdx++]));
            this.served = (int.Parse(messageComponents[startIdx++]) != 0);
        }
    }
}
