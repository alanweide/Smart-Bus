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
        private const int NearbyThreshold = 1;

        private BusDriver()
        {
            rcvcallBack = new NetInst.ReceivePktCallback(ReadNetworkPkt);
            NetPort = new NetInst(rcvcallBack);
        }

        //private void NetworkBroadcast(byte[] msg, int size)
        //{
        //    NetPort.Broadcast(msg, size);
        //}

        //private static void BroadcastMessage(string msg)
        //{
        //    Debug.Print("Sending message " + msg);
        //    BusDriver driver = BusDriver.getInstance();
        //    byte[] msgBytes = Utilities.StringToByteArray(msg);
        //    driver.NetworkBroadcast(msgBytes, msg.Length);
        //}

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
                        int stopsUntilEncounter = bus.StopsUntilEncounter(stopId);

                        // Only reply if we're "nearby"; that is, this stop is in our immediate future
                        if (0 <= stopsUntilEncounter && stopsUntilEncounter < NearbyThreshold)
                        {
                            // A bus stop is asking for route information
                            // from "nearby" buses, and we're one of them

                            IMessagePayload replyPayload = bus.route;
                            SBMessage reply = new SBMessage(SBMessage.MessageType.ROUTE_INFO_RESPONSE, message.header.destination, message.header.origin, replyPayload);
                        }
                        break;
                    }
                case SBMessage.MessageType.ROUTE_CHANGE_REQUEST:
                    {
                        Route other = (Route)message.payload;
                        IMessagePayload replyPayload;
                        if (bus.route.IsRequestSubsetOf(other))
                        {
                            bus.route = other;
                            replyPayload = new PayloadRouteChangeAckResponse(true, other);
                        }
                        else
                        {
                            replyPayload = new PayloadRouteChangeAckResponse(false, bus.route);
                        }
                        SBMessage reply = new SBMessage(SBMessage.MessageType.ROUTE_CHANGE_ACK, message.header.destination, message.header.origin, replyPayload);
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
