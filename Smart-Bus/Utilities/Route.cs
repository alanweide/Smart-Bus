using System;
using Microsoft.SPOT;
using System.Collections;

namespace Smart_Bus
{
    class Route
    {
        IList stops;

        public void addStop(int stopId, int duration)
        {
            RouteStop newStop = new RouteStop(stopId, duration);
            stops.Add(newStop);
        }

        public void InsertStop(int stopId, int duration, int routeIdx)
        {
            RouteStop newStop = new RouteStop(stopId, duration);
            stops.Insert(routeIdx, newStop);
        }

    }

    struct RouteStop
    {
        int stopId;
        int duration;

        public RouteStop(int stopId, int duration)
        {
            this.stopId = stopId;
            this.duration = duration;
        }
    }
}
