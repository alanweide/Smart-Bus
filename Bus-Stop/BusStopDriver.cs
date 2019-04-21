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
            SBMessage message = new SBMessage(msgString);
            Debug.Print("Received message of type " + message.header.type.ToString() + ": " + msgString);
            SBMessage.MessageType msgType = message.header.type;
            BusStop stop = BusStopDriver.getInstance().myBusStop;

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

                        //Send ROUTE_INFO_REQUEST to buses while receiving a request but the stop does not have route info
                        SBMessage.MessageEndpoint origin = new SBMessage.MessageEndpoint(SBMessage.MessageEndpoint.EndpointType.BUS_STOP, instance.myBusStop.id);
                        SBMessage.MessageEndpoint destination = new SBMessage.MessageEndpoint();
                        SBMessage m = new SBMessage(SBMessage.MessageType.ROUTE_INFO_REQUEST, origin, destination, null);
                        m.Broadcast(BusStopDriver.getInstance().NetPort);

                        switch (instance.myBusStop.stop_state)
                        {
                            case BusStop_State.REQUEST_NULL:
                            case BusStop_State.REQUEST_READY_TO_BE_ASSIGNED:
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

                        instance.myBusStop.Update_busRoute(element);

                        switch (instance.myBusStop.stop_state)
                        {
                            case BusStop_State.REQUEST_WAIT_FOR_ROUTE_INFO:
                                instance.myBusStop.num_route_info_rsp_rcvd++;

                                if (instance.myBusStop.num_route_info_rsp_rcvd == instance.myBusStop.numBuses)
                                {
                                    instance.myBusStop.stop_state = BusStop_State.REQUEST_READY_TO_BE_ASSIGNED;
                                    //Start to assign request
                                    int bus_index = instance.myBusStop.Lookup_request();
                                    if (bus_index != -1)
                                    {
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
                                        instance.myBusStop.Lookup_request();
                                    }
                                    
                                }
                                else
                                {
                                    //The request has been reject, retry to assign requests again
                                    instance.myBusStop.stop_state = BusStop_State.REQUEST_READY_TO_BE_ASSIGNED;

                                    //Start to assign request
                                    int bus_index = instance.myBusStop.Lookup_request();
                                    if (bus_index != -1)
                                    {
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
                        
                        //The request has been accept, delete the request from request_list
                        break;
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
