using System;
using Microsoft.SPOT;
using System.Collections;
using System.Text;

namespace Smart_Bus
{
    public class Route : IMessagePayload
    {
        IList importantStops;

        public int NumServed
        {
            get
            {
                int simElapsedMillis = Utilities.ElapsedMillis();
                int computeTime = Bus.START_TIME;
                int i = 0;
                while (i < this.importantStops.Count && computeTime < simElapsedMillis)
                {
                    Request_v stop = this[i];
                    computeTime = System.Math.Max(computeTime, stop.earliestServingTime) + Constants.STOP_DURATION;
                    if (i < this.importantStops.Count - 1 && computeTime < simElapsedMillis)
                    {
                        // We still haven't gotten to the last request we've served, 
                        //  so compute how long it will take to get to the next stop
                        Request_v nextStop = this[i + 1];
                        computeTime += Utilities.TravelTime(stop.stop, nextStop.stop);
                    }
                    i++;
                }
                return i - 1;
            }
        }

        public int Count
        {
            get { return this.importantStops.Count; }
        }

        public Route()
        {
            this.importantStops = new ArrayList();
            BusStop terminusStop = new BusStop(Bus.TERMINUS);
            Request_v terminusStart = new Request_v(-1, Bus.START_TIME, Bus.START_TIME, true, terminusStop, false);
            Request_v terminusEnd = new Request_v(-1, Bus.END_TIME, Bus.END_TIME, false, terminusStop, false);
            this.importantStops.Add(terminusStart);
            this.importantStops.Add(terminusEnd);
        }

        public Route(string[] messageComponents, ref int startIdx)
        {
            throw new NotImplementedException();
        }

        public Request_v this[int i]
        {
            get { return (Request_v)this.importantStops[i]; }
            set { this.importantStops[i] = value; }
        }

        public void InsertStop(int idx, Request_v stop)
        {
            this.importantStops.Insert(idx, stop);
        }

        public Request_v RemoveStopAt(int idx)
        {
            Request_v removed = this[idx];
            this.importantStops.RemoveAt(idx);
            return removed;
        }

        public bool IsRequestSubsetOf(Route other)
        {
            bool requestsMatch = true;
            for (int i = 0; i < this.importantStops.Count && requestsMatch; i++)
            {
                bool otherContainsThisRequest = false;
                for (int j = 0; j < other.importantStops.Count && !otherContainsThisRequest; j++)
                {
                    otherContainsThisRequest = other[j].Equals(this[i]);
                }
                requestsMatch = otherContainsThisRequest;
            }
            return requestsMatch;
        }

        public String BuildPayload()
        {
            throw new NotImplementedException();
        }
    }
}
