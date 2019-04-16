using System;
using System.Threading;
using Microsoft.SPOT;
using System.Collections;

namespace Smart_Bus
{
    public class BusStop
    {
        public int id;
        public Bus[] busInfo_list;
        public Request[] request_list;
        public const int urgencyThreshold = 100;

        public BusStop(int id)
        {
            this.id = id;
        }
        
        public void request_receive(Request new_request)
        {
            Debug.Print("Received new request");
            Debug.Print("Origin: " + new_request.origin.id + ", Destination: " + new_request.destination.id);
            Debug.Print("EarliestPickupTime: " + new_request.earliestPickupTime + ", LatestPickupTime: " + new_request.latestPickupTime);
            Debug.Print("EarliestDeliveryTime: " + new_request.earliestDeliveryTime + ", LatestDeliveryTime: " + new_request.latestDeliveryTime);

            //First, add the new request to the request list
            this.request_list = Append_request(new_request);

            //Second, assign the new request immediately if (latestDeliveryTime - current time) < urgencyThreshold
            //if (new_request.latestDeliveryTime - Global_Timer.C_Time < urgencyThreshold)
            {
                request_assign(new_request);
            }
            //otherwise, start a timer with the period of latestDeliveryTime - (current time + urgencyThreshold),
            // assign the new request when the timer timeout
            //else
            {
                request_assign(new_request);
                //requestTimer = new Timer(new TimerCallback(requestHandler), null, 0, (new_request.latestDeliveryTime - (Global_Timer.C_Time + urgencyThreshold)) * 1000);
            }

        }

        public void update_receive(Bus bus, bool is_update)
        {
            Debug.Print("Received route update from bus: " + bus.id + ", is_update: " + is_update);

            if (busInfo_list == null && is_update == true)
            {
                this.busInfo_list = Append_busInfo(bus);
            }
            
            
            for (int i = 0; i < busInfo_list.Length; i++)
            {
                //remove the bus info
                if (busInfo_list[i].id == bus.id)
                {
                    this.busInfo_list = Remove_busInfo(i);
                }
            }

            if (is_update)
            {
                this.busInfo_list = Append_busInfo(bus);
            }
        }

        public void request_assign(Request new_request)
        {
            int max_flex, flex_tmp;
            int matched_bus_index;

            Debug.Print("Assigning request: " + new_request);

            max_flex = -1000000;
            matched_bus_index = -1;
            //assign this requests to the best matched bus
            //check if there is any bus info
            if (busInfo_list != null)
            {
                for (int i = 0; i < busInfo_list.Length; i++)
                {
                    if (busInfo_list[i].routeInfo == null)
                    {
                        matched_bus_index = i;

                        //calculate the flexibility after insert the new request to the current route of target bus
                        flex_tmp = route_reschedule(busInfo_list[i], null, new_request);
                        Debug.Print("After inserting, bus " + busInfo_list[i].id + " has flexibility of " + flex_tmp);

                        break;
                    }

                    Request_v[] routeInfo_tmp = new Request_v[busInfo_list[i].routeInfo.Length];

                    for (int c = 0; c < busInfo_list[i].routeInfo.Length; c++)
                    {
                        routeInfo_tmp[c] = busInfo_list[i].routeInfo[c];
                    }
                    
                    //calculate the flexibility after insert the new request to the current route of target bus
                    flex_tmp = route_reschedule(busInfo_list[i], routeInfo_tmp, new_request);
                    Debug.Print("After inserting, bus " + busInfo_list[i].id + " has flexibility of " + flex_tmp);

                    if (flex_tmp > max_flex)
                    {
                        matched_bus_index = i;
                    }
                }

                if (matched_bus_index != -1)
                {
                    //assign the request to the mateched available bus
                    //route_reschedule(busInfo_list[matched_bus_index], busInfo_list[matched_bus_index].routeInfo, new_request);
                    
                    Debug.Print("Assign the request to bus: " + matched_bus_index);
                }
            }
        }

        public int route_reschedule(Bus bus, Request_v[] routeInfo, Request new_request)
        {
            Request_v[] routeInfo_tmp;
            //int routeInfo_tmp_count;
            Request_v new_origin, new_destination;
            int flex, max_flex = -1000000;
            //int p_origin = -1, p_destination = -1; //the insertion point with maximal flexibility

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

            //The bus has not been assigned any request
            if (routeInfo == null)
            {
                Request_v[] routeInfo_tmp_o = Append_route(null, new_origin);
                Request_v[] routeInfo_tmp_o_d = Append_route(routeInfo_tmp_o, new_destination);
                return flexibility_calculate(bus, routeInfo_tmp_o_d);
            }
            else
            {
                routeInfo_tmp = new Request_v[routeInfo.Length];
            }

            for (int i = 0; i < routeInfo.Length; i++)
            {
                routeInfo_tmp[i] = routeInfo[i];
            }

            for (int i = 0; i < routeInfo_tmp.Length+1; i++)
            {
                //Debug.Print("i: "+i);
                // check all available insertion points to find the maximal flexibility
                if (i == routeInfo_tmp.Length)
                {
                    Request_v[] routeInfo_tmp_o = Insert_route(routeInfo_tmp, new_origin, i);
                    Request_v[] routeInfo_tmp_o_d = Insert_route(routeInfo_tmp_o, new_destination, i+1);

                    if (bus_capacity_check(routeInfo_tmp_o_d, Bus.capacity) == true)
                    {
                        flex = flexibility_calculate(bus, routeInfo_tmp_o_d);
                        if (flex > max_flex)
                        {
                            max_flex = flex;
                        }
                    }
                }
                else
                {
                    if (routeInfo_tmp[i].served == false)
                    {
                        Request_v[] routeInfo_tmp_o = Insert_route(routeInfo_tmp, new_origin, i);

                        for (int j = i + 1; j < routeInfo_tmp_o.Length + 1; j++)
                        {
                            //Debug.Print("j: " + j);
                            Request_v[] routeInfo_tmp_o_d = Insert_route(routeInfo_tmp_o, new_destination, j);

                            if (bus_capacity_check(routeInfo_tmp_o_d, Bus.capacity) == true)
                            {
                                flex = flexibility_calculate(bus, routeInfo_tmp_o_d);
                                if (flex > max_flex)
                                {
                                    max_flex = flex;
                                }
                            }
                        }
                    }
                }
            }

            return max_flex;
        }

        public int flexibility_calculate(Bus bus, Request_v[] routeInfo)
        {
            int F = 0;
            int t_v = 0;

            for (int i = 0; i < routeInfo.Length; i++)
            {
                //Calculate each flexible time at stop v: f_v = l_v - t_v

                if (i == 0)
                {
                    //t_0 = ts_k + travel_time(terminus, v_0)
                    t_v = bus.busStartTime + System.Math.Abs(routeInfo[i].location - bus.terminusLocation);
                    //Debug.Print("t_v at location " + routeInfo[i].location + " is " + t_v);

                }
                else
                {
                    //t_v = max(t_v-1, e_v-1) + travel_time(v-1, v)
                    t_v = System.Math.Max(t_v, routeInfo[i - 1].earliestServingTime) + System.Math.Abs(routeInfo[i].location - routeInfo[i - 1].location);
                    //Debug.Print("t_v at location " + routeInfo[i].location + " is " + t_v);
                }

                F += routeInfo[i].latestServingTime - t_v;
                //Debug.Print("f_v at location " + routeInfo[i].location + " is " + (routeInfo[i].latestServingTime - t_v));
            }
            //add up (l_0 - te_k) to F_k
            F += bus.busEndTime - (System.Math.Max(t_v, routeInfo[routeInfo.Length - 1].earliestServingTime) + System.Math.Abs(bus.terminusLocation - routeInfo[routeInfo.Length - 1].location));
            //Debug.Print("t_v at terminus is " + (System.Math.Max(t_v, routeInfo[routeInfo.Length - 1].earliestServingTime) + System.Math.Abs(bus.terminus_location - routeInfo[routeInfo.Length - 1].location)));
            //Debug.Print("f_v at terminus is " + (bus.bus_end_time - (System.Math.Max(t_v, routeInfo[routeInfo.Length - 1].earliestServingTime) + System.Math.Abs(bus.terminus_location - routeInfo[routeInfo.Length - 1].location))));

            /*
            Debug.Print("Route: ");
            for (int i = 0; i < routeInfo.Length; i++)
            {
                Debug.Print("[" + routeInfo[i].location + "]");
            }
            Debug.Print("Flexibility: " +F);
            */
            return F;
        }

        public bool bus_capacity_check(Request_v[] routeInfo, int capacity)
        {
            int avail_capacity = capacity;

            for (int i = 0; i < routeInfo.Length; i++)
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

        public Bus[] Append_busInfo(Bus element)
        {
            Bus[] new_list;

            if (this.busInfo_list == null)
            {
                new_list = new Bus[1];
                new_list[0] = element;
                return new_list;
            }
            else
            {
                new_list = new Bus[this.busInfo_list.Length + 1];
            }

            for (int i = 0; i < this.busInfo_list.Length; i++)
            {
                new_list[i] = this.busInfo_list[i];
            }
            new_list[this.busInfo_list.Length] = element;

            return new_list;
        }

        public Bus[] Remove_busInfo(int index)
        {
            if (this.busInfo_list == null)
            {
                return null;
            }

            if (this.busInfo_list.Length == 0 && index == 0)
            {
                return null;
            }

            Bus[] new_list = new Bus[this.busInfo_list.Length - 1];

            for (int i = 0; i < index; i++)
            {
                new_list[i] = this.busInfo_list[i];
            }

            for (int i = index; i < this.busInfo_list.Length-1; i++)
            {
                new_list[i] = this.busInfo_list[i+1];
            }

            return new_list;
        }

        public Request[] Append_request(Request element)
        {   
            Request[] new_list;
            
            if (this.request_list == null)
            {
                new_list = new Request[1];
                new_list[0] = element;
                return new_list;
            }
            else
            {
                new_list = new Request[this.request_list.Length + 1];
            }

            for (int i = 0; i < this.request_list.Length; i++)
            {
                new_list[i] = this.request_list[i];
            }
            new_list[this.request_list.Length] = element;

            return new_list;
        }

        public Request[] Remove_request(int index)
        {
            if (this.request_list == null)
            {
                return null;
            }

            if (this.request_list.Length == 1 && index == 0)
            {
                return null;
            }

            Request[] new_list = new Request[this.request_list.Length - 1];

            for (int i = 0; i < index; i++)
            {
                new_list[i] = this.request_list[i];
            }

            for (int i = index; i < this.busInfo_list.Length - 1; i++)
            {
                new_list[i] = this.request_list[i + 1];
            }

            return new_list;
        }

        public Request_v[] Append_route(Request_v[] routeInfo_list, Request_v element)
        {
            Request_v[] new_list;
               
            if (routeInfo_list == null)
            {
                new_list = new Request_v[1];
                new_list[0] = element;
                return new_list;
            }
            else
            {
                new_list = new Request_v[routeInfo_list.Length + 1];
            }

            for (int i = 0; i < routeInfo_list.Length; i++)
            {
                new_list[i] = routeInfo_list[i];
            }
            new_list[routeInfo_list.Length] = element;

            return new_list;
        }

        public Request_v[] Insert_route(Request_v[] routeInfo, Request_v element, int insert_index)
        {
            Request_v[] new_list = new Request_v[routeInfo.Length + 1];

            for (int i = 0; i < insert_index; i++)
            {
                new_list[i] = routeInfo[i];
            }

            new_list[insert_index] = element;

            for (int i = insert_index; i < routeInfo.Length; i++)
            {
                new_list[i + 1] = routeInfo[i];
            }

            return new_list;
        }

        public Request_v[] Remove_route(Request_v[] routeInfo, int remove_index)
        {
            if (routeInfo.Length == 0)
            {
                return null;
            }

            Request_v[] new_list = new Request_v[routeInfo.Length - 1];

            for (int i = 0; i < remove_index; i++)
            {
                new_list[i] = routeInfo[i];
            }

            for (int i = remove_index; i < routeInfo.Length - 1; i++)
            {
                new_list[i] = routeInfo[i + 1];
            }

            return new_list;
        }
    }

}
