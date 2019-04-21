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
        public const int START_TIME = 0;
        public const int END_TIME = 100000;
        public const int CAPACITY = 5;
        public const int TERMINUS = -1;
        //public Request_v[] routeInfo;

        public Bus(int id)
        {
            this.id = id;
            this.availCapacity = CAPACITY;
            this.terminusLocation = TERMINUS;
            this.busStartTime = START_TIME;
            this.busEndTime = END_TIME;
            this.route = new Route();
        }

        public bool IsNearbyStop(int stopId)
        {
            //int stopsUntilEncounter = this.StopsUntilEncounter(stopId);
            //return 0 <= stopsUntilEncounter && stopsUntilEncounter < NearbyThreshold;
            return true;
        }

        public bool HasCapacityNow()
        {
            return this.route.CurrentCapacity() > 0;
        }
    }
}
