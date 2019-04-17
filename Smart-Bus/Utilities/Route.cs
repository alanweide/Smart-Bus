using System;
using Microsoft.SPOT;
using System.Collections;
using System.Text;

namespace Smart_Bus
{
    public class Route : IMessagePayload
    {
        IList stops;
        IList requests;

        public int StopCount
        {
            get { return this.stops.Count; }
            private set { }
        }

        public int RequestCount
        {
            get { return this.requests.Count; }
            private set { }
        }

        public Route()
        {
            this.stops = new ArrayList();
            this.requests = new ArrayList();
        }

        public Route(string[] messageComponents, ref int startIdx)
        {
            this.stops = new ArrayList();
            this.requests = new ArrayList();
            int numStops = int.Parse(messageComponents[startIdx]);
            int i = startIdx + 1;
            while (i < numStops)
            {
                // WARNING: This RouteStop constructor updates i to the index
                // in the array of the element immediately following this stop
                RouteStop newStop = new RouteStop(messageComponents, ref i);

                this.stops.Add(newStop);
            }
            while (i < messageComponents.Length)
            {
                // WARNING: This Request_v constructor updates i to the index
                // in the array of the element immediately following this request
                Request_v request = new Request_v(messageComponents, ref i);

                this.requests.Add(request);
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
            StringBuilder payload = new StringBuilder();
            for (int i = 0; i < this.stops.Count; i++)
            {
                payload.Append(this.stops[i].ToString() + " ");
            }
            return payload.ToString();
        }

        public override bool Equals(object obj)
        {
            if (obj == this) { return true; }
            Route other = obj as Route;
            if (other == null) { return false; }
            if (other.stops.Count != this.stops.Count) { return false; }
            bool areEqual = true;
            for (int i = 0; areEqual && i < this.stops.Count; i++)
            {
                areEqual = this.stops[i].Equals(other.stops[i]);
            }
            return areEqual;
        }

        public bool IsRequestSubsetOf(Route other)
        {
            bool requestsMatch = true;
            for (int i = 0; i < this.requests.Count && requestsMatch; i++)
            {
                bool otherContainsThisRequest = false;
                for (int j = 0; j < other.requests.Count && !otherContainsThisRequest; j++)
                {
                    otherContainsThisRequest = other.requests[j] == this.requests[i];
                }
                requestsMatch = otherContainsThisRequest;
            }
            return requestsMatch;
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

        public RouteStop(string[] messageComponents, ref int startIdx)
        {
            this.stopId = int.Parse(messageComponents[startIdx++]);
            this.duration = int.Parse(messageComponents[startIdx++]);
            this.capDelta = int.Parse(messageComponents[startIdx++]);
        }

        public override string ToString()
        {
            return this.stopId.ToString() + " " + this.duration.ToString() + " " + this.capDelta.ToString();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is RouteStop))
                return false;

            RouteStop other = (RouteStop)obj;
            return this.stopId == other.stopId && this.duration == other.duration && this.capDelta == other.capDelta;
        }
    }
}
