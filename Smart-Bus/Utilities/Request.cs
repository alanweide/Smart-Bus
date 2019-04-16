using System;
using System.Threading;
using Microsoft.SPOT;

namespace Smart_Bus
{
    public class Request : IComparable, IMessagePayload
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
            this.origin = new BusStop(origin);
            this.destination = new BusStop(destination);
            this.earliestDeliveryTime = earliestPickupTime + TravelTime(this.origin, this.destination);
            this.latestDeliveryTime = latestDeliveryTime;
            this.latestPickupTime = latestDeliveryTime - TravelTime(this.origin, this.destination);
            this.served = false;
        }

        public Request(string[] messageComponents, int headerLength)
        {
            // After the header, the array is organized as follows:
            //  [earliestPickupTime, latestDeliveryTime, origin, destination]
            // where the times are in ms since simulation start

            int earliestPickupTime = int.Parse(messageComponents[headerLength]);
            int latestDeliveryTime = int.Parse(messageComponents[headerLength + 1]);
            int originId = int.Parse(messageComponents[headerLength + 2]);
            int destinationId = int.Parse(messageComponents[headerLength + 3]);

            this.requestSendTime = earliestPickupTime;
            this.earliestPickupTime = earliestPickupTime;
            this.origin = new BusStop(originId);
            this.destination = new BusStop(destinationId);
            this.earliestDeliveryTime = earliestPickupTime + TravelTime(this.origin, this.destination);
            this.latestDeliveryTime = latestDeliveryTime;
            this.latestPickupTime = latestDeliveryTime - TravelTime(origin, destination);
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

        public void FromStringArray(string[] payload, int headerLength)
        {
            throw new NotImplementedException();
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