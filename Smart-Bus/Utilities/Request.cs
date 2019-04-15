using System;
using System.Threading;
using Microsoft.SPOT;

namespace Smart_Bus
{
    public class Request : IComparable
    {
        public int requestSendTime;
        public int earliestPickupTime;
        public int latestPickupTime;
        public int earliestDeliveryTime;
        public int latestDeliveryTime;
        public BusStop origin;
        public BusStop destination;
        public bool served;

        public Request(int earliestPickupTime, int latestDeliveryTime, int origin, int destination)
        {
            this.requestSendTime = earliestPickupTime;
            this.earliestPickupTime = earliestPickupTime;
            this.earliestDeliveryTime = earliestPickupTime + travelTime(origin, destination);
            this.latestDeliveryTime = latestDeliveryTime;
            this.latestPickupTime = latestDeliveryTime - travelTime(origin, destination);
            this.origin = new BusStop(origin);
            this.destination = new BusStop(destination);
            this.served = false;
        }

        public int CompareTo(object obj)
        {
            if (obj == null) return 1;

            Request other = obj as Request;
            if (other != null)
            {
                return System.Math.Sign(this.earliestPickupTime - other.earliestPickupTime);
            }
            else
            {
                throw new ArgumentException("obj is not a Request");
            }
        }

        public int travelTime(int origin, int destination)
        {
            //travel time = hopcount * 10000 (ticks)
            return System.Math.Abs(origin - destination) * 1000;
        }
    }

    public struct Request_v
    {
        public int earliestServingTime;
        public int latestServingTime;
        public bool is_origin;
        public int location;
        public bool served;
    }
}