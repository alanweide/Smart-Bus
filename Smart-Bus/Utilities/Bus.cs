using System;
using Microsoft.SPOT;
using System.Collections;

namespace Smart_Bus
{
    public class Bus
    {
        public int id;
        public DateTime simStartTime;

        public const int capacity = 5;
        public const int NearbyThreshold = 1;
        public int availCapacity;
        public int terminusLocation;
        public int busStartTime; //ts_k: expected start time at terminus 
        public int busEndTime; //te_k: expected end time terminus
        public Route route;
        //public Request_v[] routeInfo;

        public Bus(int id, int terminusId, int busStartTime, int busEndTime)
        {
            this.id = id;
            this.availCapacity = capacity;
            this.terminusLocation = terminusId;
            this.busStartTime = busStartTime;
            this.busEndTime = busEndTime;
            this.route = new Route();
        }

        public int StopsUntilEncounter(int stopId)
        {
            BusStatus currLoc = this.StatusAtTime(DateTime.Now);
            int stopsUntilEncounter = -1;
            for (int i = currLoc.routeIdx; i < this.route.StopCount && stopsUntilEncounter < 0; i++)
            {
                if (this.route[i].stopId == stopId)
                {
                    stopsUntilEncounter = i - currLoc.routeIdx;
                }
            }
            return stopsUntilEncounter;
        }

        public BusStatus StatusAtTime(DateTime time)
        {
            BusStatus currStatus = new BusStatus();
            TimeSpan elapsedTime = time - this.simStartTime;
            int realElapsedMillis = (int)elapsedTime.Milliseconds;
            int simElapsedMillis = realElapsedMillis / Constants.TIME_MULTIPLIER;
            int computeTime = busStartTime;
            int computeCap = capacity;
            int i = 0;
            while (computeTime < simElapsedMillis)
            {
                RouteStop stop = this.route[i];
                computeTime += stop.duration;
                computeCap += stop.capDelta;

                if (computeTime > simElapsedMillis)
                {
                    // We're currently waiting at a stop
                    currStatus.previousStop = stop.stopId;
                    currStatus.nextStop = stop.stopId;
                    currStatus.timeRemainingInState = computeTime - simElapsedMillis;
                    currStatus.routeIdx = i;
                    currStatus.capacity = computeCap;
                }
                else
                {
                    computeTime += Constants.HOP_DURATION;
                    if (computeTime > simElapsedMillis)
                    {
                        // We're currently in transit to a stop
                        RouteStop nextStop = this.route[i + 1];
                        currStatus.previousStop = stop.stopId;
                        currStatus.nextStop = nextStop.stopId;
                        currStatus.timeRemainingInState = computeTime - simElapsedMillis;
                        currStatus.routeIdx = i;
                        currStatus.capacity = computeCap;
                    }
                }
                i++;
            }
            return currStatus;
        }

        public bool IsNearbyStop(int stopId)
        {
            //int stopsUntilEncounter = this.StopsUntilEncounter(stopId);
            //return 0 <= stopsUntilEncounter && stopsUntilEncounter < NearbyThreshold;
            return true;
        }

        // Updates this Bus's route information by marking requests as "served" if their latestServingTime has passed.
        // This is imprecise (it might take a while for a request to be marked served after it was actually served),
        // but it will never mark an unserved request as served, and it is relatively fast.
        public void UpdateServedRequests()
        {
            IList temp = new ArrayList();
            while (this.route.RequestCount > 0)
            {
                Request earliest = this.route.RemoveEarliestRequest();
                earliest.origin.served = (Utilities.ElapsedMillis() > earliest.origin.latestServingTime);
                earliest.destination.served = (Utilities.ElapsedMillis() > earliest.destination.latestServingTime);
                temp.Add(earliest);
            }
            this.route.SetRequests(temp);
        }
    }
}
