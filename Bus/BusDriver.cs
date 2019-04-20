using System;
using System.Text;
using Microsoft.SPOT;
using System.Threading;
using Microsoft.SPOT.Hardware;
using Microsoft.SPOT.Messaging;
using System.IO.Ports;
using Samraksh.SPOT.Emulator.Network;

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

        private static string BuildRouteReply(int stopId)
        {
            StringBuilder msg = new StringBuilder();
            msg.Append(appId.ToString() + " ");
            Bus bus = getInstance().myBus;
            msg.Append(bus.id.ToString() + " ");
            for (int i = 0; i < bus.route.StopCount; i++)
            {
                msg.Append(bus.route[i].stopId + "," + bus.route[i].duration + " ");
            }
            return msg.ToString();
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
            switch (msgType)
            {
                case SBMessage.MessageType.START_SIMULATION:
                    {
                        BusDriver.SimStart = ((PayloadDateTime)message.payload).date;
                        Utilities.SimStart = BusDriver.SimStart;
                        break;
                    }
                case SBMessage.MessageType.ROUTE_INFO_REQUEST:
                    {
                        int stopId = message.header.origin.srcId;

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
                                message.header.destination,
                                message.header.origin,
                                replyPayload);
                            reply.Broadcast(BusDriver.getInstance().NetPort);
                        }
                        break;
                    }
                case SBMessage.MessageType.ROUTE_CHANGE_REQUEST:
                    {
                        //Route other = (Route)message.payload;
                        Request_v[] requests = new Request_v[4];
                        int[] requestIds = new int[4];
                        IMessagePayload replyPayload;

                        bus.UpdateServedRequests();

                        Route other = new Route(requests, requestIds);

                        // I believe the only way we might end up with the else case here
                        //  is if another stop has changed our route since this stop last
                        //  received info about our route.
                        if (bus.route.IsRequestSubsetOf(other))
                        {
                            // If this is an acceptable route to change to, 
                            //  then set our route and notify the stop
                            bus.route = other;

                            replyPayload = new PayloadRouteChangeAckResponse(true, bus.route);
                        }
                        else
                        {
                            // Else, reject the new route (keep our own), 
                            //  and update the stop with our route
                            replyPayload = new PayloadRouteChangeAckResponse(false, bus.route);
                        }
                        SBMessage reply = new SBMessage(
                            SBMessage.MessageType.ROUTE_CHANGE_ACK,
                            message.header.destination,
                            message.header.origin,
                            replyPayload);
                        reply.Broadcast(BusDriver.getInstance().NetPort);
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

            instance.myBus = new Smart_Bus.Bus(appId, 0, 0, 100000);

            Thread.Sleep(Timeout.Infinite);
        }

    }
}
