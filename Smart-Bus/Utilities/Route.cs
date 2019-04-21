using System;
using Microsoft.SPOT;
using System.Collections;
using System.Text;

namespace Smart_Bus
{
    public class Route : IMessagePayload
    {
        IList importantStops;

        // NumServed marks served requests as such, in addition to
        //  computing the number of served requests.
        public int NumServed
        {
            get
            {
                int simElapsedMillis = Utilities.ElapsedSimulationMillis();
                int computeTime = Bus.START_TIME;
                int i = 0;
                int servedCount = 0;
                while (i < this.importantStops.Count - 1 && computeTime < simElapsedMillis)
                {
                    Request_v stop = this[i];
                    computeTime = System.Math.Max(computeTime, stop.earliestServingTime) + Constants.STOP_DURATION;
                    if (computeTime < simElapsedMillis)
                    {
                        // We still haven't gotten to the last request we've served, 
                        //  so compute how long it will take to get to the next stop
                        servedCount++;
                        stop.served = true;
                        Request_v nextStop = this[i + 1];
                        computeTime += Utilities.TravelTime(stop.stop, nextStop.stop);
                    }
                    i++;
                }
                return servedCount;
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

        public Route(Request_v[] arr)
        {
            this.importantStops = new ArrayList();
            foreach(Request_v stop in arr)
            {
                this.importantStops.Add(stop);
            }
        }

        public Route(string[] messageComponents, ref int startIdx)
        {
            this.importantStops = new ArrayList();
            while (startIdx < messageComponents.Length)
            {
                Request_v stop = new Request_v(messageComponents, ref startIdx);
                this.importantStops.Add(stop);
            }

            int foo = this.NumServed;
        }

        public Request_v this[int i]
        {
            get { return (Request_v)this.importantStops[i]; }
            set { this.importantStops[i] = value; }
        }

        public Request_v[] ToArray()
        {
            Request_v[] arr = new Request_v[this.importantStops.Count];
            for (int i = 0; i < this.importantStops.Count; i++)
            {
                arr[i] = this[i];
            }
            return arr;
        }

        public void InsertStop(int idx, Request_v stop)
        {
            this.importantStops.Insert(idx, stop);
        }

        public Request_v RemoveStopAt(int i)
        {
            Request_v removed = this[i];
            this.importantStops.RemoveAt(i);
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
            StringBuilder payload = new StringBuilder();
            int numServed = this.NumServed;
            for (int i = 0; i < this.importantStops.Count; i++)
            {
                Request_v stop = this[i];
                if (i < numServed)
                {
                    stop.served = true;
                }
                payload.Append(stop.BuildPayload());
            }
            return payload.ToString();
        }
    }
}
