using System;
using Microsoft.SPOT;

namespace Smart_Bus
{
    public class Bus
    {
        public int id;
        public const int capacity = 5;
        public int avail_capacity;
        public int terminus_location;
        public int bus_start_time; //ts_k: expected start time at terminus 
        public int bus_end_time; //te_k: expected end time terminus
        public Request_v[] routeInfo;
        public int routeInfo_count;
        public BusStop[] updateList;
        public int updateList_count;

        public Bus(int id, int terminus_id, int bus_start_time, int bus_end_time)
        {
            this.id = id;
            this.avail_capacity = capacity;
            this.terminus_location = terminus_id;
            this.bus_start_time = bus_start_time;
            this.bus_end_time = bus_end_time;
            this.routeInfo = new Request_v[200];
            this.routeInfo_count = 0;
            this.updateList = new BusStop[2];
            this.updateList_count = 0;
        }



        public bool futureRouteContains(int stopId)
        {
            // TODO: Implement this
            return true;
        }
    }
}