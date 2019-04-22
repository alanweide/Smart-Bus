using System;
using System.Threading;
using Microsoft.SPOT;
using System.Collections;

namespace Smart_Bus
{
    public struct Bus_info
    {
        public int busId;
        public const int capacity = 5;
        public int terminusLocation;
        public int busStartTime; //ts_k: expected start time at terminus 
        public int busEndTime; //te_k: expected end time terminus
        public int NumServed;
        public Request_v[] routeInfo;
        public Request_v[] pending_routeInfo;
    }

    public enum BusStop_State
    {
        REQUEST_NULL = 0,
        REQUEST_WAIT_FOR_ROUTE_INFO = 1,
        REQUEST_READY_TO_BE_ASSIGNED = 2,
        REQUEST_WAIT_FOR_TIMER = 3,
        REQUEST_ASSIGNED_AND_SENDING_TO_BUS = 4
    }
    
    public class BusStop
    {   
        public int id;
        public BusStop_State stop_state;
        public Bus_info[] bus_list;
        public Request[] request_list;
        public int pending_request_index;
        public const int urgencyThreshold = int.MaxValue; // Requests are always "urgent"
        public DateTime SimStart;
        public int numBuses;
        public int num_route_info_rsp_rcvd;

        public BusStop(int id)
        {
            this.id = id;
            this.stop_state = BusStop_State.REQUEST_NULL;
            this.pending_request_index = -1;
            this.numBuses = 0;
            this.num_route_info_rsp_rcvd = 0;
        }

        public void Receive_request(Request new_request)
        {
            //TimeSpan elapsedTime = DateTime.Now - SimStart;
            //int simulationMillis = (int)elapsedTime.Milliseconds;

            Debug.Print("Received new request");
            Debug.Print("Request ID" + new_request.origin.requestId);
            Debug.Print("Origin: " + new_request.origin.stop.id + ", Destination: " + new_request.destination.stop.id);
            Debug.Print("EarliestPickupTime: " + new_request.origin.earliestServingTime + ", LatestPickupTime: " + new_request.origin.latestServingTime);
            Debug.Print("EarliestDeliveryTime: " + new_request.destination.earliestServingTime + ", LatestDeliveryTime: " + new_request.destination.latestServingTime);
            
            //First, add the new request to the request list
            Add_request(new_request);
        }

        private void request_lookup_timer_handler(object obj)
        {
            stop_state = BusStop_State.REQUEST_READY_TO_BE_ASSIGNED;
            Lookup_request();
        }


        /*
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
        */

        public int Lookup_request()
        {
            int simulationMillis = Utilities.ElapsedSimulationMillis();
            int min_latestServingTime = 10000000;
            int served_request_index = -1;

            if (request_list == null)
            {
                Debug.Print("No any serving request.");
                return -1;
            }


            //check request_list to find the request with minimal latestDeliveryTime
            for (int i=0; i<request_list.Length; i++)
            {
                if (request_list[i].destination.latestServingTime < min_latestServingTime)
                {
                    min_latestServingTime = request_list[i].destination.latestServingTime;
                    served_request_index = i;
                }
            }

            //check if the request is urgent
            if (request_list[served_request_index].destination.latestServingTime - simulationMillis <= urgencyThreshold)
            {
                int bus_index = Assign_request(request_list[served_request_index]);

                // if successfully assign the request, then we need to return the bus index
                if (bus_index != -1)
                {
                    pending_request_index = served_request_index;
                    return bus_index;
                }
                 
            }
            else
            {
                //There is no urgent request, start a timer to assign requests later
                stop_state = BusStop_State.REQUEST_WAIT_FOR_TIMER;
                int timerDuration = (request_list[served_request_index].destination.latestServingTime - (simulationMillis + urgencyThreshold)) / Constants.TIME_MULTIPLIER;
                new Timer(new TimerCallback(request_lookup_timer_handler), null, timerDuration, 0);
            }

            return -1;
        }

        public int Assign_request(Request request)
        {
            int max_flex, flex_tmp;
            int matched_bus_index;

            Debug.Print("Assigning request: " + request.origin.requestId);

            max_flex = -1000000;
            matched_bus_index = -1;
            //assign this requests to the best matched bus

            //check if there is any bus info
            if (bus_list != null)
            {
                for (int i = 0; i < bus_list.Length; i++)
                {
                    if (bus_list[i].routeInfo == null)
                    {
                        matched_bus_index = i;

                        //calculate the flexibility after insert the new request to the current route of target bus
                        flex_tmp = route_reschedule(bus_list[i], null, request);
                        Debug.Print("After inserting, bus " + bus_list[i].busId+ " has flexibility of " + flex_tmp);

                        break;
                    }

                    Request_v[] routeInfo_tmp = new Request_v[bus_list[i].routeInfo.Length];

                    for (int c = 0; c < bus_list[i].routeInfo.Length; c++)
                    {
                        routeInfo_tmp[c] = bus_list[i].routeInfo[c];
                    }

                    //calculate the flexibility after insert the new request to the current route of target bus
                    flex_tmp = route_reschedule(bus_list[i], routeInfo_tmp, request);
                    Debug.Print("After inserting, bus " + bus_list[i].busId + " has flexibility of " + flex_tmp);

                    if (flex_tmp > max_flex)
                    {
                        matched_bus_index = i;
                    }
                }

                if (matched_bus_index != -1)
                {
                    Debug.Print("Assign the request to bus: " + matched_bus_index);
                    
                    //assign the request to the mateched available bus
                    insert_request_to_route(bus_list[matched_bus_index], matched_bus_index, bus_list[matched_bus_index].routeInfo, request);
                    bus_list[matched_bus_index].routeInfo = bus_list[matched_bus_index].pending_routeInfo;

                    return matched_bus_index;
                }
            }
            return -1;
        }

        /*
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
        */
        public int route_reschedule(Bus_info bus, Request_v[] routeInfo, Request new_request)
        {
            Request_v[] routeInfo_tmp;

            Request_v new_origin = new Request_v(), new_destination = new Request_v();
            int flex, max_flex = -1000000;
            //int p_origin = -1, p_destination = -1; //the insertion point with maximal flexibility

            new_origin.earliestServingTime = new_request.origin.earliestServingTime;
            new_origin.latestServingTime = new_request.origin.latestServingTime;
            new_origin.is_origin = true;
            new_origin.stop = new BusStop(new_request.origin.stop.id);
            new_origin.served = false;

            new_destination.earliestServingTime = new_request.destination.earliestServingTime;
            new_destination.latestServingTime = new_request.destination.latestServingTime;
            new_destination.is_origin = false;
            new_destination.stop = new BusStop(new_request.destination.stop.id);
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

        public void insert_request_to_route(Bus_info bus, int bus_index, Request_v[] routeInfo, Request new_request)
        {
            Request_v[] routeInfo_tmp;

            Request_v new_origin = new Request_v(), new_destination = new Request_v();
            int flex, max_flex = -1000000;
            //int p_origin = -1, p_destination = -1; //the insertion point with maximal flexibility

            new_origin.earliestServingTime = new_request.origin.earliestServingTime;
            new_origin.latestServingTime = new_request.origin.latestServingTime;
            new_origin.is_origin = true;
            new_origin.stop = new BusStop(new_request.origin.stop.id);
            new_origin.served = false;

            new_destination.earliestServingTime = new_request.destination.earliestServingTime;
            new_destination.latestServingTime = new_request.destination.latestServingTime;
            new_destination.is_origin = false;
            new_destination.stop = new BusStop(new_request.destination.stop.id);
            new_destination.served = false;

            //The bus has not been assigned any request
            if (routeInfo == null)
            {
                Request_v[] routeInfo_tmp_o = Append_route(null, new_origin);
                Request_v[] routeInfo_tmp_o_d = Append_route(routeInfo_tmp_o, new_destination);
                bus_list[bus_index].pending_routeInfo = routeInfo_tmp_o_d;
                return;
            }
            else
            {
                routeInfo_tmp = new Request_v[routeInfo.Length];
            }

            for (int i = 0; i < routeInfo.Length; i++)
            {
                routeInfo_tmp[i] = routeInfo[i];
            }

            for (int i = 0; i < routeInfo_tmp.Length + 1; i++)
            {
                //Debug.Print("i: "+i);
                // check all available insertion points to find the maximal flexibility
                if (i == routeInfo_tmp.Length)
                {
                    Request_v[] routeInfo_tmp_o = Insert_route(routeInfo_tmp, new_origin, i);
                    Request_v[] routeInfo_tmp_o_d = Insert_route(routeInfo_tmp_o, new_destination, i + 1);

                    if (bus_capacity_check(routeInfo_tmp_o_d, Bus.capacity) == true)
                    {
                        flex = flexibility_calculate(bus, routeInfo_tmp_o_d);
                        if (flex > max_flex)
                        {
                            max_flex = flex;
                            bus_list[bus_index].pending_routeInfo = routeInfo_tmp_o_d;
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
                                    bus_list[bus_index].pending_routeInfo = routeInfo_tmp_o_d;
                                }
                            }
                        }
                    }
                }
            }
        }

        public int flexibility_calculate(Bus_info bus, Request_v[] routeInfo)
        {
            int F = 0;
            int t_v = 0;

            for (int i = 0; i < routeInfo.Length; i++)
            {
                //Calculate each flexible time at stop v: f_v = l_v - t_v

                if (i == 0)
                {
                    //t_0 = ts_k + travel_time(terminus, v_0)
                    t_v = bus.busStartTime + System.Math.Abs(routeInfo[i].stop.id - bus.terminusLocation);
                    //Debug.Print("t_v at location " + routeInfo[i].location + " is " + t_v);

                }
                else
                {
                    //t_v = max(t_v-1, e_v-1) + travel_time(v-1, v)
                    t_v = System.Math.Max(t_v, routeInfo[i - 1].earliestServingTime) + System.Math.Abs(routeInfo[i].stop.id - routeInfo[i - 1].stop.id);
                    //Debug.Print("t_v at location " + routeInfo[i].location + " is " + t_v);
                }

                F += routeInfo[i].latestServingTime - t_v;
                //Debug.Print("f_v at location " + routeInfo[i].location + " is " + (routeInfo[i].latestServingTime - t_v));
            }
            //add up (l_0 - te_k) to F_k
            F += bus.busEndTime - (System.Math.Max(t_v, routeInfo[routeInfo.Length - 1].earliestServingTime) + System.Math.Abs(bus.terminusLocation - routeInfo[routeInfo.Length - 1].stop.id));
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


        public void Update_busRoute(Bus_info element)
        {
            bool find_element = false;

            if (this.bus_list == null)
            {
                //create a new element
                bus_list = new Bus_info[1];
                bus_list[0] = element;

                return;
            }

            for (int i = 0; i < this.bus_list.Length; i++)
            {
                if (bus_list[i].busId == element.busId)
                {
                    //find an existing element, update the route
                    bus_list[i].routeInfo = element.routeInfo;
                    find_element = true;
                }
            }

            if (find_element == false)
            {
                //append the element from the end of bus_list
                Bus_info[] new_list = new Bus_info[bus_list.Length + 1];
                for (int i = 0; i < this.bus_list.Length; i++)
                {
                    new_list[i] = bus_list[i];
                }
                new_list[bus_list.Length] = element;
                bus_list = new_list;
            }
        }


        /*
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
        */

        public void Add_request(Request element)
        {   
            
            if (this.request_list == null)
            {
                //create a new element
                request_list = new Request[1];
                request_list[0] = element;
                return;
            }

            //append the element from the end of routeInfo_list
            Request[] new_list = new Request[request_list.Length + 1];
            for (int i = 0; i < request_list.Length; i++)
            {
                new_list[i] = request_list[i];
            }
            new_list[request_list.Length] = element;
            request_list = new_list;
        }

        public void Remove_request(int index)
        {
            if (this.request_list == null)
            {
                return;
            }

            if (this.request_list.Length == 1 && index == 0)
            {
                request_list = null;
                return;
            }

            Request[] new_list = new Request[this.request_list.Length - 1];

            for (int i = 0; i < index; i++)
            {
                new_list[i] = this.request_list[i];
            }

            for (int i = index; i < this.request_list.Length - 1; i++)
            {
                new_list[i] = this.request_list[i + 1];
            }
            request_list = new_list;
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
