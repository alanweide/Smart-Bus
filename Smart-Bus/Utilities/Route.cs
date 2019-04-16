using System;
using Microsoft.SPOT;
using System.Collections;

namespace Smart_Bus
{
    public class Route : IMessagePayload
    {
        IList stops;

        public int Count
        {
            get { return this.stops.Count; }
            private set { }
        }

        public Route()
        { 
            this.stops = new ArrayList(); 
        }

        public Route(string[] messageComponents, int headerLength)
        {
            this.stops = new ArrayList();
            for (int i = headerLength; i < messageComponents.Length; i+=3)
            {
                int stopId = int.Parse(messageComponents[i]);
                int duration = int.Parse(messageComponents[i+1]);
                int capDelta = int.Parse(messageComponents[i+2]);
                RouteStop newStop = new RouteStop(stopId, duration, capDelta);
                this.stops.Add(newStop);
            }
        }

        public void AddStop(int stopId, int duration, int capDelta)
        {
            RouteStop newStop = new RouteStop(stopId, duration, capDelta);
            this.stops.Add(newStop);
        }

        public void InsertStop(int stopId, int duration, int capDelta, int routeIdx)
        {
            RouteStop newStop = new RouteStop(stopId, duration, capDelta);
            this.stops.Insert(routeIdx, newStop);
        }

        public RouteStop RemoveStop(int index)
        {
            RouteStop removed = (RouteStop)this.stops[index];
            this.stops.RemoveAt(index);
            return removed;
        }

        public RouteStop this[int i]
        {
            get { return (RouteStop)this.stops[i]; }
            private set { }
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

    public struct RouteStop
    {
        public int stopId;
        public int duration;
        public int capDelta;

        public RouteStop(int stopId, int duration, int capDelta)
        {
            this.stopId = stopId;
            this.duration = duration;
            this.capDelta = capDelta;
        }
    }
}
