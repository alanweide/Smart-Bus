using System;
using System.Text;
using Microsoft.SPOT;
using System.Threading;
using Microsoft.SPOT.Hardware;
using Microsoft.SPOT.Messaging;
using System.IO.Ports;
using Samraksh.SPOT.Emulator.Network;
using System.Collections;


namespace Smart_Bus
{
    class BusStopDriver
    {
        private static int getRealSendTime(int simulationMillis)
        {
            return simulationMillis / Constants.TIME_MULTIPLIER;
        }

        private static BusStopDriver instance;

        // Requires an id, which is equivalent to appId, so do not create bus until after appId is initialized
        BusStop myBusStop;

        NetInst NetPort;
        NetInst.ReceivePktCallback rcvcallBack;

        private static UInt16 appId;
        private static DateTime SimStart;
        private const int NearbyThreshold = 1;

        private BusStopDriver()
        {
            rcvcallBack = new NetInst.ReceivePktCallback(ReadNetworkPkt);
            NetPort = new NetInst(rcvcallBack);
        }

        public static BusStopDriver getInstance()
        {
            if (BusStopDriver.instance == null)
            {
                BusStopDriver.instance = new BusStopDriver();
            }
            return BusStopDriver.instance;
        }

        private static void SendRouteChangeRequest(int bus_index)
        {
            SBMessage.MessageEndpoint origin = new SBMessage.MessageEndpoint(SBMessage.MessageEndpoint.EndpointType.BUS_STOP, instance.myBusStop.id);
            SBMessage.MessageEndpoint destination = new SBMessage.MessageEndpoint(SBMessage.MessageEndpoint.EndpointType.BUS, instance.myBusStop.bus_list[bus_index].busId);
            IMessagePayload payload = new Route(instance.myBusStop.bus_list[bus_index].routeInfo);
            SBMessage message = new SBMessage(SBMessage.MessageType.ROUTE_CHANGE_REQUEST, origin, destination, payload);
            message.Broadcast(BusStopDriver.getInstance().NetPort);

        }

        public static void ReadNetworkPkt(byte[] msg, int size)
        {

            string msgString = Utilities.ByteArrayToString(msg);
            Debug.Print("Received message: " + msgString);
            SBMessage message = new SBMessage(msgString);
            SBMessage.MessageType msgType = message.header.type;
            BusStop stop = BusStopDriver.getInstance().myBusStop;


            // Only handle the message if it is meant for the stop
            SBMessage.MessageEndpoint msgDest = message.header.destination;
            if (msgDest.endptType == SBMessage.MessageEndpoint.EndpointType.BROADCAST ||
                (msgDest.endptType == SBMessage.MessageEndpoint.EndpointType.BUS_STOP &&
                    msgDest.endptId == instance.myBusStop.id))
            {

                switch (msgType)
                {
                    case SBMessage.MessageType.START_SIMULATION:
                        {
                            BusStopDriver.SimStart = ((PayloadSimStart)message.payload).date;
                            Utilities.SimStart = BusStopDriver.SimStart;
                            instance.myBusStop.SimStart = BusStopDriver.SimStart;

                            //here we know the number of buses in the network, 
                            //so we can know how many messages of ROUTE_INFO_RESPONSE the stop has to wait after sending ROUTE_INFO_REQUEST
                            PayloadSimStart payload = (PayloadSimStart)message.payload;
                            instance.myBusStop.numBuses = payload.numBuses;

                            break;
                        }
                    case SBMessage.MessageType.SEND_PASSENGER_REQUEST:
                        {
                            //Always send ROUTE_INFO_REQUEST to buses while receiving a request
                            SBMessage.MessageEndpoint origin = new SBMessage.MessageEndpoint(SBMessage.MessageEndpoint.EndpointType.BUS_STOP, instance.myBusStop.id);
                            SBMessage.MessageEndpoint destination = new SBMessage.MessageEndpoint();
                            SBMessage m = new SBMessage(SBMessage.MessageType.ROUTE_INFO_REQUEST, origin, destination, new PayloadSimpleString());
                            m.Broadcast(BusStopDriver.getInstance().NetPort);

                            switch (instance.myBusStop.stop_state)
                            {
                                case BusStop_State.REQUEST_NULL:
                                case BusStop_State.REQUEST_READY_TO_BE_ASSIGNED:

                                    //BusStop has to wait for receving all ROUTE_INFO_RESPONSE, then it's allowed to assigned requests
                                    instance.myBusStop.stop_state = BusStop_State.REQUEST_WAIT_FOR_ROUTE_INFO;

                                    //reset the counter for collecting ROUTE_INFO_RESPONSE
                                    instance.myBusStop.num_route_info_rsp_rcvd = 0;
                                    break;

                                case BusStop_State.REQUEST_WAIT_FOR_TIMER:

                                    //reset the counter for collecting ROUTE_INFO_RESPONSE
                                    instance.myBusStop.num_route_info_rsp_rcvd = 0;

                                    break;

                                case BusStop_State.REQUEST_WAIT_FOR_ROUTE_INFO:
                                case BusStop_State.REQUEST_ASSIGNED_AND_SENDING_TO_BUS:
                                    //do nothing
                                    break;
                                default:
                                    break;
                            }

                            Request new_request = (Request)message.payload;
                            //Save the request to request_list
                            instance.myBusStop.Receive_request(new_request);

                            break;
                        }
                    case SBMessage.MessageType.ROUTE_INFO_RESPONSE:
                        {
                            //Update the route info
                            Bus_info element = new Bus_info();
                            element.busId = message.header.source.endptId;

                            Route payload = (Route)message.payload;
                            element.NumServed = payload.NumServed;
                            element.routeInfo = payload.ToArray();
                            element.busStartTime = Bus.START_TIME;
                            element.busEndTime = Bus.END_TIME;
                            element.terminusLocation = Bus.TERMINUS;

                            //Save the bus info and route info to bus_list
                            instance.myBusStop.Update_busRoute(element);

                            switch (instance.myBusStop.stop_state)
                            {
                                case BusStop_State.REQUEST_WAIT_FOR_ROUTE_INFO:
                                    //Increase the num of recevied ROUTE_INFO_RESPONSE
                                    instance.myBusStop.num_route_info_rsp_rcvd++;

                                    if (instance.myBusStop.num_route_info_rsp_rcvd == instance.myBusStop.numBuses)
                                    {
                                        //received all ROUTE_INFO_RESPONSE, then BusStop have enough route info to assign request
                                        instance.myBusStop.stop_state = BusStop_State.REQUEST_READY_TO_BE_ASSIGNED;
                                        
                                        //Start to assign request
                                        int bus_index = instance.myBusStop.Lookup_request();
                                        if (bus_index != -1)
                                        {
                                            //BusStop find an request and its matched bus to assign
                                            SendRouteChangeRequest(bus_index);
                                            instance.myBusStop.stop_state = BusStop_State.REQUEST_ASSIGNED_AND_SENDING_TO_BUS;
                                        }
                                    }
                                    break;

                                case BusStop_State.REQUEST_NULL:
                                case BusStop_State.REQUEST_READY_TO_BE_ASSIGNED:
                                case BusStop_State.REQUEST_WAIT_FOR_TIMER:
                                case BusStop_State.REQUEST_ASSIGNED_AND_SENDING_TO_BUS:
                                    //do nothing
                                    break;
                                default:
                                    break;
                            }


                            break;
                        }
                    case SBMessage.MessageType.ROUTE_CHANGE_ACK:
                        {
                            switch (instance.myBusStop.stop_state)
                            {
                                case BusStop_State.REQUEST_ASSIGNED_AND_SENDING_TO_BUS:

                                    PayloadRouteChangeAckResponse ack = (PayloadRouteChangeAckResponse)message.payload;

                                    if (ack.didAcceptRouteChange == true)
                                    {
                                        //The request has been accept, delete the request from request_list
                                        instance.myBusStop.Remove_request(instance.myBusStop.pending_request_index);

                                        if (instance.myBusStop.request_list == null)
                                        {
                                            //If there is no other requests, 
                                            instance.myBusStop.stop_state = BusStop_State.REQUEST_NULL;
                                        }
                                        else
                                        {
                                            //If there is still other requests, start to assign requests again
                                            instance.myBusStop.stop_state = BusStop_State.REQUEST_READY_TO_BE_ASSIGNED;

                                            int bus_index = instance.myBusStop.Lookup_request();
                                            if (bus_index != -1)
                                            {
                                                //BusStop find an request and its matched bus to assign
                                                SendRouteChangeRequest(bus_index);
                                                instance.myBusStop.stop_state = BusStop_State.REQUEST_ASSIGNED_AND_SENDING_TO_BUS;
                                            }
                                        }

                                    }
                                    else
                                    {
                                        //Update the route info
                                        Bus_info element = new Bus_info();
                                        element.busId = message.header.source.endptId;

                                        Route payload = (Route)message.payload;
                                        element.NumServed = payload.NumServed;
                                        element.routeInfo = payload.ToArray();
                                        element.busStartTime = Bus.START_TIME;
                                        element.busEndTime = Bus.END_TIME;
                                        element.terminusLocation = Bus.TERMINUS;

                                        instance.myBusStop.Update_busRoute(element);
                                        
                                        //The request has been reject, retry to assign requests again
                                        instance.myBusStop.stop_state = BusStop_State.REQUEST_READY_TO_BE_ASSIGNED;

                                        //Start to assign request
                                        int bus_index = instance.myBusStop.Lookup_request();
                                        if (bus_index != -1)
                                        {
                                            //BusStop find an request and its matched bus to assign
                                            SendRouteChangeRequest(bus_index);
                                            instance.myBusStop.stop_state = BusStop_State.REQUEST_ASSIGNED_AND_SENDING_TO_BUS;
                                        }
                                    }

                                    break;

                                case BusStop_State.REQUEST_NULL:
                                case BusStop_State.REQUEST_WAIT_FOR_ROUTE_INFO:
                                case BusStop_State.REQUEST_WAIT_FOR_TIMER:
                                case BusStop_State.REQUEST_READY_TO_BE_ASSIGNED:
                                    //do nothing
                                    break;
                                default:
                                    break;
                            }

                            break;
                        }
                }
            }
        }


        public static void Main()
        {
            BusStopDriver driver = BusStopDriver.getInstance();
            driver.NetPort.Init();
            Thread.Sleep(100);
            appId = driver.NetPort.GetID();
            Debug.Print("Successfully got ID from the Hub: " + appId.ToString());

            instance.myBusStop = new Smart_Bus.BusStop(appId);

            Thread.Sleep(Timeout.Infinite);
        }
    }
}
