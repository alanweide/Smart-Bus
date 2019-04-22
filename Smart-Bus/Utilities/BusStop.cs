using System;
using System.Threading;
using Microsoft.SPOT;
using System.Collections;

namespace Smart_Bus
{
    public struct Bus_info
    {
        public int busId;
        public const int capacity = 5; //the maximal seats for passenger
        public int terminusLocation; //the id of bus terminus
        public int busStartTime; //ts_k: expected start time at terminus 
        public int busEndTime; //te_k: expected end time terminus
        public int NumServed; //the number of served location in the route
        public Request_v[] routeInfo; //the current route info
        public Request_v[] pending_routeInfo; //the temprorary route info when adding a new request to current route
    }

    public enum BusStop_State
    {
        REQUEST_NULL = 0,
        /* There is no request in request_list to assign */

        REQUEST_WAIT_FOR_ROUTE_INFO = 1,
        /* Whenever BusStop send ROUTE_INFO_REQUEST to all buses, */
        /* BusStop should wait all ROUTE_INFO_RESPONSE message to synchronize the newest route info of all buses */

        REQUEST_READY_TO_BE_ASSIGNED = 2,
        /* There is no pending request assignment, BusStop can start to assign a urgent request from request_list */

        REQUEST_WAIT_FOR_TIMER = 3,
        /* There is no pending request assignment and no urgent requests, BusStop starts a timer to wait to assign the closest urgent request */

        REQUEST_ASSIGNED_AND_SENDING_TO_BUS = 4
        /* There is a pending request sent to matched bus, BusStop need to wait for ROUTE_CHANGE_ACK */
    }
    
    public class BusStop
    {   
        public int id;
        public BusStop_State stop_state;
        public Bus_info[] bus_list;
        public Request[] request_list;
        public int pending_request_index; //the index of pending request in request_list
        public const int urgencyThreshold = int.MaxValue; // Requests are always "urgent"
        public DateTime SimStart;
        public int numBuses;  //number of buses in the system
        public int num_route_info_rsp_rcvd; //number of ROUTE_INFO_RESPONSE received so far

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
            Debug.Print("Received new request");
            Debug.Print("Request ID" + new_request.origin.requestId);
            Debug.Print("Origin: " + new_request.origin.stop.id + ", Destination: " + new_request.destination.stop.id);
            Debug.Print("EarliestPickupTime: " + new_request.origin.earliestServingTime + ", LatestPickupTime: " + new_request.origin.latestServingTime);
            Debug.Print("EarliestDeliveryTime: " + new_request.destination.earliestServingTime + ", LatestDeliveryTime: " + new_request.destination.latestServingTime);
            
            //add the new request to the request list
            Add_request(new_request);
        }

        private void request_lookup_timer_handler(object obj)
        {
            stop_state = BusStop_State.REQUEST_READY_TO_BE_ASSIGNED;
            Lookup_request();
        }

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
            if (request_list[served_request_index].destination.latestServingTime - simulationMillis < urgencyThreshold)
            {
                int bus_index = Assign_request(request_list[served_request_index]);

                // if successfully assign the request, then we need to return the bus index in bus_list
                if (bus_index != -1)
                {
                    //save the the index of pending request in request_list
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

        //assign this requests to the best matched bus
        public int Assign_request(Request request)
        {
            int max_flex, flex_tmp;
            int matched_bus_index;

            Debug.Print("Assigning request: " + request.origin.requestId);

            max_flex = -1000000;
            matched_bus_index = -1;
            
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

                    //update the routeInfo after inserting the new quest
                    bus_list[matched_bus_index].routeInfo = bus_list[matched_bus_index].pending_routeInfo;

                    return matched_bus_index;
                }
            }
            return -1;
        }
       
        public int route_reschedule(Bus_info bus, Request_v[] routeInfo, Request new_request)
        {
            Request_v[] routeInfo_tmp;

            Request_v new_origin = new Request_v(), new_destination = new Request_v();
            int flex, max_flex = -1000000;

            //Here we create new_origin and new_destination content and then try to insert these two location to the bus route
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
                //Then we directly append new_origin and new_destination to bus route and calculate the flexibility 
                Request_v[] routeInfo_tmp_o = Append_route(null, new_origin);
                Request_v[] routeInfo_tmp_o_d = Append_route(routeInfo_tmp_o, new_destination);
                return flexibility_calculate(bus, routeInfo_tmp_o_d);
            }
            else
            {
                //creat a route info content to save possible sequences of route after inserting new_origin and new_destination
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
                    //If the insertion point for new_origin is at the last in route, then we directly append new_origin and new_destination to the last
                    Request_v[] routeInfo_tmp_o = Insert_route(routeInfo_tmp, new_origin, i);
                    Request_v[] routeInfo_tmp_o_d = Insert_route(routeInfo_tmp_o, new_destination, i+1);

                    //we must make sure the bus has seats to serve new passenger for the new route
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
                    //We cannot insert new_origin and new_destination before the served locations
                    if (routeInfo_tmp[i].served == false)
                    {
                        //Pick up a insert point for new_origin first
                        Request_v[] routeInfo_tmp_o = Insert_route(routeInfo_tmp, new_origin, i);

                        for (int j = i + 1; j < routeInfo_tmp_o.Length + 1; j++)
                        {
                            //Debug.Print("j: " + j);

                            //And then try all possible insert point for new_destination after new_origin
                            Request_v[] routeInfo_tmp_o_d = Insert_route(routeInfo_tmp_o, new_destination, j);

                            //we must make sure the bus has seats to serve new passenger for the new route
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

            //Here we create new_origin and new_destination content and then try to insert these two location to the bus route
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
                //Then we directly append new_origin and new_destination to bus route and calculate the flexibility 
                Request_v[] routeInfo_tmp_o = Append_route(null, new_origin);
                Request_v[] routeInfo_tmp_o_d = Append_route(routeInfo_tmp_o, new_destination);
                bus_list[bus_index].pending_routeInfo = routeInfo_tmp_o_d;
                return;
            }
            else
            {
                //creat a route info content to save possible sequences of route after inserting new_origin and new_destination
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
                    //If the insertion point for new_origin is at the last in route, then we directly append new_origin and new_destination to the last
                    Request_v[] routeInfo_tmp_o = Insert_route(routeInfo_tmp, new_origin, i);
                    Request_v[] routeInfo_tmp_o_d = Insert_route(routeInfo_tmp_o, new_destination, i + 1);

                    //we must make sure the bus has seats to serve new passenger for the new route
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
                    //We cannot insert new_origin and new_destination before the served locations
                    if (routeInfo_tmp[i].served == false)
                    {
                        //Pick up a insert point for new_origin first
                        Request_v[] routeInfo_tmp_o = Insert_route(routeInfo_tmp, new_origin, i);

                        for (int j = i + 1; j < routeInfo_tmp_o.Length + 1; j++)
                        {
                            //Debug.Print("j: " + j);

                            //And then try all possible insert point for new_destination after new_origin
                            Request_v[] routeInfo_tmp_o_d = Insert_route(routeInfo_tmp_o, new_destination, j);

                            //we must make sure the bus has seats to serve new passenger for the new route
                            if (bus_capacity_check(routeInfo_tmp_o_d, Bus.capacity) == true)
                            {
                                flex = flexibility_calculate(bus, routeInfo_tmp_o_d);
                                if (flex > max_flex)
                                {
                                    max_flex = flex;

                                    //Here we find the route with maximal flexibility after inserting new_origin and new_destination to the bus
                                    //Save the route to pending_routeInfo and then ready to send ROUTE_CHANGE_REQUEST
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
