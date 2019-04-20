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

        private struct RouteBuilderPair
        {
            public Request_v origin;
            public int requestId;

            public RouteBuilderPair(Request_v origin, int requestId)
            {
                this.origin = origin;
                this.requestId = requestId;
            }
        }

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

        public Route(Request_v[] importantStops, int[] requestIds)
        {
            Debug.Assert(importantStops.Length == requestIds.Length);
            Debug.Assert(importantStops.Length % 2 == 0);
            Debug.Assert(importantStops.Length > 0);

            IList startedRequests = new ArrayList();

            this.stops = new ArrayList();
            this.stops.Add(new RouteStop(importantStops[0].stop.id, 0, 0));
            for (int i = 1; i < importantStops.Length; i++)
            {
                RouteStop prevStop = (RouteStop)this.stops[this.stops.Count - 1];
                Request_v thisStop = importantStops[i];
                if (prevStop.stopId == thisStop.stop.id)
                {
                    // This is a second pickup/dropoff at the same stop
                    if (thisStop.is_origin)
                    {
                        // This is the origin for a request, so add it to the "started" list
                        startedRequests.Add(new RouteBuilderPair(thisStop, requestIds[i]));
                        prevStop.capDelta--;
                    }
                    else
                    {
                        // This is the destination for a request
                        prevStop.capDelta++;
                        Request_v origin = new Request_v();

                        // Find the origin for this request, and remove it from the started list
                        int j;
                        for (j = 0; j < startedRequests.Count; j++)
                        {
                            RouteBuilderPair pair = (RouteBuilderPair)startedRequests[i];
                            if (pair.requestId == requestIds[i])
                            {
                                origin = pair.origin;
                                break;
                            }
                        }
                        startedRequests.RemoveAt(j);

                        // Add this request to the completed list
                        this.requests.Add(new Request(origin, thisStop));
                    }
                }
                else
                {
                    RouteStop newStop = new RouteStop(thisStop.stop.id, 0, Constants.STOP_DURATION);

                    // Fill in intermediate/transient stops along the route
                    if (thisStop.stop.id > prevStop.stopId)
                    {
                        for (int j = prevStop.stopId + 1; j <= thisStop.stop.id; j++)
                        {
                            this.stops.Add(new RouteStop(j, 0, 0));
                        }
                    }
                    else
                    {
                        for (int j = thisStop.stop.id - 1; j >= prevStop.stopId; j--)
                        {
                            this.stops.Add(new RouteStop(j, 0, 0));
                        }
                    }
                    if (thisStop.is_origin)
                    {
                        // This is the origin, so add it to the "started" list
                        startedRequests.Add(new RouteBuilderPair(thisStop, requestIds[i]));
                        newStop.capDelta--;
                    }
                    else
                    {
                        newStop.capDelta++;
                        Request_v origin = new Request_v();

                        // Find the origin for this request, and remove it from the started list
                        int j;
                        for (j = 0; j < startedRequests.Count; j++)
                        {
                            RouteBuilderPair pair = (RouteBuilderPair)startedRequests[i];
                            if (pair.requestId == requestIds[i])
                            {
                                origin = pair.origin;
                                break;
                            }
                        }
                        startedRequests.RemoveAt(j);

                        // Add this request to the served list
                        this.requests.Add(new Request(origin, thisStop));
                    }
                    
                    // Append this new stop to the end of this.stops
                    this.stops.Add(newStop);
                }
            }
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

        public void AddRequest(Request r)
        {
            this.requests.Add(r);
        }

        public Request RemoveEarliestRequest()
        {
            Request minRequest = null;
            for (int i = 0; i < this.requests.Count; i++)
            {
                Request thisRequest = this.requests[i] as Request;
                if (minRequest == null || thisRequest.origin.earliestServingTime < minRequest.origin.earliestServingTime)
                {
                    minRequest = thisRequest;
                }
            }
            return minRequest;
        }

        public void SetRequests(IList requests)
        {
            this.requests = requests;
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
