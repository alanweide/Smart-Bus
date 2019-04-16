using System;
using Microsoft.SPOT;

namespace Smart_Bus
{
    public class Bus
    {
        public int id;
        public DateTime simStartTime;

        public const int capacity = 5;
        public int availCapacity;
        public int terminusLocation;
        public int busStartTime; //ts_k: expected start time at terminus 
        public int busEndTime; //te_k: expected end time terminus
        public Route route;
        public Request_v[] routeInfo;

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
            for (int i = currLoc.routeIdx; i < this.route.Count && stopsUntilEncounter < 0; i++)
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
                    computeTime += Constants.BUS_HOP_TIME;
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
    }
}
