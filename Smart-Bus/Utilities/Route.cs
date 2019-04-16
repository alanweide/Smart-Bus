using System;
using Microsoft.SPOT;
using System.Collections;

namespace Smart_Bus
{
    public class Route
    {
        IList stops;

        public int Count
        {
            get { return this.stops.Count; }
            private set { }
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
            RouteStop removed = (RouteStop) this.stops[index];
            this.stops.RemoveAt(index);
            return removed;
        }

        public RouteStop this[int i]
        {
            get { return (RouteStop)this.stops[i]; }
            private set {}
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
