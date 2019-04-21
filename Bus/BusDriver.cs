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
    public class BusDriver
    {
        private static int getRealSendTime(int simulationMillis)
        {
            return simulationMillis / Constants.TIME_MULTIPLIER;
        }

        private static BusDriver instance;

        // Requires an id, which is equivalent to appId, so do not create bus until after appId is initialized
        Bus myBus;

        NetInst NetPort;
        NetInst.ReceivePktCallback rcvcallBack;

        private static UInt16 appId;
        private static DateTime SimStart;
        private SBMessage.MessageEndpoint messageSource;

        private BusDriver()
        {
            rcvcallBack = new NetInst.ReceivePktCallback(ReadNetworkPkt);
            NetPort = new NetInst(rcvcallBack);
        }

        public static BusDriver getInstance()
        {
            if (BusDriver.instance == null)
            {
                BusDriver.instance = new BusDriver();
            }
            return BusDriver.instance;
        }

        public static void ReadNetworkPkt(byte[] msg, int size)
        {
            // Received message from network
            // There are a few possibilities for this message:
            //   If broadcast from a stop, decide if we're close enough to respond
            //     (i.e., the stop that sent it is on our route in the future),
            //     then respond with our route info and current location.
            //   If request from stop to change route
            //     Attempt to change route; reply either confirming or rejecting the change
            //   If it's a "start simulation" message from the RequestDriver,
            //     keep track of the start time to know current location
            //   If it's any other message, or we aren't close enough to the stop to
            //     reply, then ignore it.

            string msgString = Utilities.ByteArrayToString(msg);
            SBMessage message = new SBMessage(msgString);
            Debug.Print("Received message: " + msgString);
            SBMessage.MessageType msgType = message.header.type;
            Bus bus = BusDriver.getInstance().myBus;
            
            // Only handle the message if it is meant for us
            SBMessage.MessageEndpoint msgDest = message.header.destination;
            if (msgDest.endptType == SBMessage.MessageEndpoint.EndpointType.BROADCAST ||
                (msgDest.endptType == SBMessage.MessageEndpoint.EndpointType.BUS &&
                    msgDest.endptId == bus.id))
            {
                switch (msgType)
                {
                    case SBMessage.MessageType.START_SIMULATION:
                        {
                            PayloadSimStart startPayload = (PayloadSimStart)message.payload;
                            BusDriver.SimStart = startPayload.date;
                            Utilities.SimStart = BusDriver.SimStart;
                            break;
                        }
                    case SBMessage.MessageType.ROUTE_INFO_REQUEST:
                        {
                            int stopId = message.header.source.endptId;

                            // Only reply if we're "nearby"
                            //  *Note: IsNearbyStop simply returns true for this project;
                            //  this is a necessary simplification due to the deadline.
                            // A real system would reply only if the stop is in our immediate
                            //  future along the route; the stops themselves would employ BFS
                            //  to find all buses. See the implementation for details.
                            if (bus.IsNearbyStop(stopId))
                            {
                                // A bus stop is asking for route information
                                // from "nearby" buses, and we're one of them

                                IMessagePayload replyPayload = bus.route;
                                SBMessage reply = new SBMessage(
                                    SBMessage.MessageType.ROUTE_INFO_RESPONSE,
                                    BusDriver.getInstance().messageSource,
                                    message.header.source,
                                    replyPayload);
                                reply.Broadcast(BusDriver.getInstance().NetPort);
                            }
                            break;
                        }
                    case SBMessage.MessageType.ROUTE_CHANGE_REQUEST:
                        {
                            Route newRoute = message.payload as Route;
                            bool confirm = false;

                            // I believe the only way we this won't be true here is if
                            //  another stop has changed our route since this stop last
                            //  received info about our route.
                            if (bus.HasCapacityNow())
                            {
                                // If this is an acceptable route to change to, 
                                //  then update our route
                                bus.route = newRoute;
                                confirm = true;
                            }

                            IMessagePayload replyPayload = new PayloadRouteChangeAckResponse(confirm, bus.route);

                            Debug.Print("Acknowledging ROUTE_CHANGE_REQUEST with " + confirm.ToString());

                            // Notify the stop one way or the other about whether we accepted the new route
                            SBMessage reply = new SBMessage(
                                SBMessage.MessageType.ROUTE_CHANGE_ACK,
                                BusDriver.getInstance().messageSource,
                                message.header.source,
                                replyPayload);
                            reply.Broadcast(BusDriver.getInstance().NetPort);
                        }
                        break;
                } 
            }
        }

        public static void Main()
        {
            BusDriver driver = BusDriver.getInstance();
            driver.NetPort.Init();
            Thread.Sleep(100);
            appId = driver.NetPort.GetID();
            Debug.Print("Successfully got ID from the Hub: " + appId.ToString());

            instance.myBus = new Bus(appId);
            instance.messageSource = new SBMessage.MessageEndpoint(SBMessage.MessageEndpoint.EndpointType.BUS, appId);

            Thread.Sleep(Timeout.Infinite);
        }

    }
}
