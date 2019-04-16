using System;
using Microsoft.SPOT;
using System.Collections;

namespace Smart_Bus
{
    public class BusStop
    {
        public int id;
        public Bus[] busInfo_list;
        public int busInfo_list_count;
        public Request[] request_list;
        public int request_list_count;
        public Request[] urgent_request_list;
        public int urgent_request_list_count;
        public const int urgencyThreshold = 10000;

        public BusStop(int id)
        {
            this.id = id;
            this.busInfo_list = new Bus[2];
            this.busInfo_list_count = 0;
            this.request_list = new Request[100];
            this.request_list_count = 0;
            this.urgent_request_list = new Request[100];
            this.urgent_request_list_count = 0;
        }

        public void request_receive(Request new_request)
        {
            request_lookup();

            //First, add the new request to the request list
            if (request_list_count < 100)
            {
                request_list[request_list_count] = new_request;
                request_list_count++;


                //Second, add the new request to the urgent list if (latestDeliveryTime - current time) < urgencyThreshold
                //if (new_request.latestDeliveryTime - Global_Timer.C_Time < urgencyThreshold)
                if (true)
                {
                    urgent_request_list[urgent_request_list_count] = new_request;
                    urgent_request_list_count++;
                }
            }

        }

        public void update_receive(Bus bus, bool is_upadate)
        {
            for (int i = 0; i < busInfo_list_count; i++)
            {
                //remove the bus info
                if (busInfo_list[i].id == bus.id)
                {
                    if (i + 1 < busInfo_list_count)
                    {
                        busInfo_list[i] = busInfo_list[i + 1];
                    }
                    busInfo_list_count--;
                }
            }

            if (is_upadate)
            {
                busInfo_list[busInfo_list_count] = bus;
                busInfo_list_count++;
            }
        }

        public void request_lookup()
        {
            //Copy the requests to urgent list if (latestDeliveryTime - current time) < urgencyThreshold
            if (request_list_count > 0)
            {
                for (int i = 0; i < request_list_count; i++)
                {
                    //if (request_list[i].latestDeliveryTime - Global_Timer.C_Time < urgencyThreshold)
                    if (true)
                    {
                        urgent_request_list[urgent_request_list_count] = request_list[i];
                        urgent_request_list_count++;
                    }
                }
            }
        }

        public void request_assign()
        {
            int max_flex, flex_tmp;
            int matched_bus_index;

            //check if there is any urgent request
            if (urgent_request_list_count > 0)
            {
                //assign urgent requests to the best matched bus
                for (int i = 0; i < urgent_request_list_count; i++)
                {
                    max_flex = -1000000;
                    matched_bus_index = -1;

                    //check if there is any bus info
                    if (busInfo_list_count > 0)
                    {
                        for (int j = 0; j < busInfo_list_count; j++)
                        {
                            Request_v[] routeInfo_tmp = new Request_v[200];
                            int routeInfo_tmp_count;

                            for (int c = 0; c < busInfo_list[j].routeInfo_count; c++)
                            {
                                routeInfo_tmp[c] = busInfo_list[j].route[c];
                            }
                            routeInfo_tmp_count = busInfo_list[j].routeInfo_count;

                            flex_tmp = route_reschedule(busInfo_list[j], routeInfo_tmp, routeInfo_tmp_count, urgent_request_list[i]);
                            if (flex_tmp > max_flex)
                            {
                                matched_bus_index = j;
                            }
                        }

                        if (matched_bus_index != -1)
                        {
                            //assign the request to the mateched available bus
                            route_reschedule(busInfo_list[matched_bus_index], busInfo_list[matched_bus_index].route, busInfo_list[matched_bus_index].routeInfo_count, urgent_request_list[i]);
                            busInfo_list[matched_bus_index].routeInfo_count++;
                        }
                    }
                }
            }

        }

        public int route_reschedule(Bus bus, Request_v[] routeInfo, int routeInfo_count, Request new_request)
        {
            Request_v[] routeInfo_tmp = new Request_v[200];
            int routeInfo_tmp_count;
            Request_v new_origin, new_destination;
            int flex, max_flex = -1000000;
            int p_origin = -1, p_destination = -1; //the insertion point with maximal flexibility

            new_origin.earliestServingTime = new_request.earliestPickupTime;
            new_origin.latestServingTime = new_request.latestPickupTime;
            new_origin.is_origin = true;
            new_origin.location = new_request.origin.id;
            new_origin.served = false;

            new_destination.earliestServingTime = new_request.earliestDeliveryTime;
            new_destination.latestServingTime = new_request.latestDeliveryTime;
            new_destination.is_origin = false;
            new_destination.location = new_request.destination.id;
            new_destination.served = false;

            for (int i = 0; i < routeInfo_count; i++)
            {
                routeInfo_tmp[i] = routeInfo[i];
            }
            routeInfo_tmp_count = routeInfo_count;

            for (int i = 0; i < routeInfo_count; i++)
            {
                // check all available insertion points to find the maximal flexibility
                if (routeInfo[i].served == false)
                {
                    route_insert(routeInfo_tmp, routeInfo_tmp_count, new_origin, i);
                    routeInfo_tmp_count++;

                    for (int j = i + 1; j < routeInfo_tmp_count; j++)
                    {
                        route_insert(routeInfo_tmp, routeInfo_tmp_count, new_destination, j);
                        routeInfo_tmp_count++;

                        if (bus_capacity_check(routeInfo_tmp, routeInfo_tmp_count, Bus.capacity) == true)
                        {
                            flex = flexibility_calculate(bus, routeInfo_tmp, routeInfo_tmp_count);
                            if (flex > max_flex)
                            {
                                max_flex = flex;
                                p_origin = i;
                                p_destination = j;
                            }
                        }

                        route_remove(routeInfo_tmp, routeInfo_tmp_count, j);
                        routeInfo_tmp_count--;
                    }

                    route_remove(routeInfo_tmp, routeInfo_tmp_count, i);
                    routeInfo_tmp_count--;
                }
            }

            //update to the routeInfo if we find any available insertion points with maxmal flexibility
            if (p_origin != -1 && p_destination != -1)
            {
                route_insert(routeInfo, routeInfo_count, new_origin, p_origin);
                routeInfo_count++;
                route_insert(routeInfo, routeInfo_count, new_destination, p_destination);
                routeInfo_count++;
            }
            return max_flex;
        }

        public Request_v[] route_insert(Request_v[] routeInfo, int routeInfo_count, Request_v new_request, int insert_index)
        {
            for (int i = 0; i < routeInfo_count; i++)
            {
                if (i == insert_index)
                {
                    // shift the elements of i, i+1, ..., n to i+1, i+2, ..., n+1
                    for (int j = routeInfo_count + 1; j > insert_index; j--)
                    {
                        routeInfo[j] = routeInfo[j - 1];
                    }
                    // insert the new request to ith element
                    routeInfo[insert_index] = new_request;
                }
            }
            return routeInfo;
        }
        public Request_v[] route_remove(Request_v[] routeInfo, int routeInfo_count, int remove_index)
        {
            for (int i = 0; i < routeInfo_count; i++)
            {
                if (i == remove_index)
                {
                    // shift the elements of i+1, i+2, ..., n to i, i+1, ..., n-1
                    for (int j = i + 1; j < routeInfo_count; j++)
                    {
                        routeInfo[j - 1] = routeInfo[j];
                    }
                }
            }
            return routeInfo;
        }

        public int flexibility_calculate(Bus bus, Request_v[] routeInfo, int routeInfo_count)
        {
            int F = 0;
            int t_v = 0;

            for (int i = 0; i < routeInfo_count; i++)
            {
                //Calculate each flexible time at stop v: f_v = l_v - t_v

                if (i == 0)
                {
                    //t_0 = ts_k + travel_time(terminus, v_0)
                    t_v = bus.busStartTime + System.Math.Abs(routeInfo[i].location - bus.terminusLocation);

                }
                else
                {
                    //t_v = max(t_v-1, e_v-1) + travel_time(v-1, v)
                    t_v = System.Math.Max(t_v, routeInfo[i - 1].earliestServingTime) + System.Math.Abs(routeInfo[i].location - routeInfo[i - 1].location);
                }

                F += routeInfo[i].latestServingTime - t_v;
            }
            //add up (l_0 - te_k) to F_k
            F += bus.busEndTime - System.Math.Abs(bus.terminusLocation - routeInfo[routeInfo_count - 1].location);

            return F;
        }

        public bool bus_capacity_check(Request_v[] routeInfo, int routeInfo_count, int capacity)
        {
            int avail_capacity = capacity;

            for (int i = 0; i < routeInfo_count; i++)
            {
                if (routeInfo[i].is_origin == true)
                {
                    avail_capacity--;
                }
                else
                {
                    avail_capacity++;
                }

                if (avail_capacity < 0)
                {
                    return false;
                }
            }
            return true;
        }
    }

}